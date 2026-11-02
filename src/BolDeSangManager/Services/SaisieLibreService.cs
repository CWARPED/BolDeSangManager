using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>Règles communes aux écritures « Saisie libre » (#29).</summary>
public static class SaisieLibreRegles
{
    public const int TresorerieMax = 10_000_000;

    /// <summary>Une caisse saisie à la main : ni négative, ni absurde.</summary>
    public static int ValiderTresorerie(int tresorerie)
    {
        if (tresorerie < 0)
            throw new InvalidOperationException("La trésorerie ne peut pas être négative.");
        if (tresorerie > TresorerieMax)
            throw new InvalidOperationException($"Trésorerie invalide : {tresorerie:N0} po.");
        return tresorerie;
    }
}

/// <summary>
/// #29 — Modifications à la main d'une équipe dans une ligue en « Saisie
/// libre » : l'application ne fait plus respecter les règles de coût, les
/// joueurs s'en chargent. Chaque écriture laisse une trace
/// (<see cref="ModificationLibre"/>) visible du commissaire.
///
/// Droits : le coach de l'équipe, et le commissaire de la ligue (admins et
/// grands commissaires compris). Aucun verrou de phase : le but est justement
/// de pouvoir corriger à tout moment. Refus net hors saisie libre — une ligue
/// classique garde ses règles.
/// </summary>
/// <summary>
/// Blessure DURABLE d'un joueur dans l'édition libre. <c>Id</c> nul = à créer ;
/// une blessure existante absente de la liste est retirée (effet inversé).
/// « Amoché » n'y figure pas : son seul effet est de rater le prochain match,
/// porté par <see cref="ProgressionLibre.RateProchainMatch"/>.
/// </summary>
public record BlessureLibre(int? Id, InjuryType Type, AffectedStat? Stat = null);

/// <summary>État COMPLET voulu de la progression d'un joueur (#29, lot 2).</summary>
/// <param name="CompetencesAcquises">Compétences acquises (pas celles du poste),
/// par id de Skill ; la liste remplace l'existant.</param>
/// <param name="Caracteristiques">Une entrée par +1 (deux +1 AG = deux entrées).</param>
public record ProgressionLibre(
    int Psp,
    bool RateProchainMatch,
    IReadOnlyList<int> CompetencesAcquises,
    IReadOnlyList<AffectedStat> Caracteristiques,
    IReadOnlyList<BlessureLibre> Blessures);

public class SaisieLibreService(
    ApplicationDbContext db,
    IAuthorizationService auth,
    TeamService teamService,
    JoueurEditionService joueurEdition,
    ILogger<SaisieLibreService> logger)
{
    public const int PspMax = 999;

    /// <summary>Blessures que l'édition libre montre et sait retirer.</summary>
    public static readonly InjuryType[] BlessuresDurables =
        [InjuryType.BlessurePersistante, InjuryType.Sequelle, InjuryType.Mort, InjuryType.RetraiteTemporaire];

    /// <summary>Blessures que l'édition libre sait AJOUTER.</summary>
    public static readonly InjuryType[] BlessuresAjoutables =
        [InjuryType.BlessurePersistante, InjuryType.Sequelle, InjuryType.Mort];

    /// <summary>
    /// ✓ de l'édition de ligne en saisie libre : nom, Haines, capitaine ET
    /// progression, dans UNE transaction — tout ou rien.
    /// </summary>
    public async Task ModifierJoueurAsync(int joueurId, string userId, string? nom,
        IEnumerable<string?> haines, bool? capitaine, ProgressionLibre progression)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await joueurEdition.ModifierJoueurAsync(joueurId, userId, nom, haines, capitaine);
            await ModifierProgressionAsync(joueurId, userId, progression);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            // Le contexte vit avec le circuit : sans ça, les entités modifiées
            // avant le refus seraient écrites par le prochain enregistrement.
            db.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>
    /// PSP, « rate le prochain match », compétences acquises, +1 en
    /// caractéristique et blessures durables : tout ce que l'après-match fait
    /// d'habitude, à la main. Les ajouts créent les mêmes lignes qu'un vrai
    /// après-match (améliorations à 0 PSP, blessures sans match) : la valeur du
    /// joueur et les caractéristiques suivent comme d'habitude.
    /// </summary>
    public async Task ModifierProgressionAsync(int joueurId, string userId, ProgressionLibre p)
    {
        var joueur = await db.TeamPlayers
            .Include(j => j.PlayerPosition).ThenInclude(pp => pp.AccesCategories)
            .Include(j => j.Improvements).ThenInclude(i => i.Skill)
            .Include(j => j.Competences)
            .Include(j => j.Blessures)
            .FirstOrDefaultAsync(j => j.Id == joueurId)
            ?? throw new InvalidOperationException("Joueur introuvable.");
        var equipe = await EquipeAutoriseeAsync(joueur.TeamId, userId);

        // ── Validation complète, avant toute écriture ───────────────────────
        if (p.Psp < 0 || p.Psp > PspMax)
            throw new InvalidOperationException($"PSP invalides : {p.Psp}.");

        var competencesVoulues = await db.Skills
            .Where(s => p.CompetencesAcquises.Contains(s.Id) && s.RulesVersionId == equipe.League.RulesVersionId)
            .ToDictionaryAsync(s => s.Id);
        if (p.CompetencesAcquises.Any(id => !competencesVoulues.ContainsKey(id)))
            throw new InvalidOperationException("Compétence inconnue dans cette version des règles.");
        if (p.CompetencesAcquises.Distinct().Count() != p.CompetencesAcquises.Count)
            throw new InvalidOperationException("Une compétence ne peut être acquise qu'une fois.");
        var depart = joueur.Competences.Where(c => c.EstCompetenceDepart).Select(c => c.SkillId).ToHashSet();
        if (p.CompetencesAcquises.FirstOrDefault(depart.Contains) is var doublon and > 0)
            throw new InvalidOperationException(
                $"« {competencesVoulues[doublon].Nom} » fait déjà partie des compétences du poste.");

        var durables = joueur.Blessures.Where(b => BlessuresDurables.Contains(b.Type)).ToList();
        foreach (var b in p.Blessures)
        {
            if (b.Id is int id)
            {
                if (durables.All(x => x.Id != id))
                    throw new InvalidOperationException("Blessure inconnue pour ce joueur.");
            }
            else
            {
                if (!BlessuresAjoutables.Contains(b.Type))
                    throw new InvalidOperationException("Ce type de blessure ne s'ajoute pas ici.");
                if (b.Type == InjuryType.Sequelle && b.Stat is null)
                    throw new InvalidOperationException("Séquelle : choisissez la caractéristique réduite.");
            }
        }
        if (p.Blessures.Count(b => b.Type == InjuryType.Mort) > 1)
            throw new InvalidOperationException("Un joueur ne meurt qu'une fois.");

        // ── Écriture ────────────────────────────────────────────────────────
        // Un joueur sans nom s'appelle « #5 » : ne pas écrire « #5 #5 ».
        var sujet = joueur.Nom == $"#{joueur.Numero}" ? joueur.Nom : $"#{joueur.Numero} {joueur.Nom}";
        var traces = new List<(string, string, string)>();

        if (joueur.PointsStarPlayer != p.Psp)
        {
            traces.Add(("PSP", joueur.PointsStarPlayer.ToString(), p.Psp.ToString()));
            joueur.PointsStarPlayer = p.Psp;
        }
        if (joueur.ManqueSuivantMatch != p.RateProchainMatch)
        {
            traces.Add(("Rate le prochain match", OuiNon(joueur.ManqueSuivantMatch), OuiNon(p.RateProchainMatch)));
            joueur.ManqueSuivantMatch = p.RateProchainMatch;
        }

        // Compétences acquises : retirer ce qui n'est plus voulu, ajouter le neuf.
        var ameliorationsCompetence = joueur.Improvements.Where(i => i.SkillId is not null).ToList();
        foreach (var imp in ameliorationsCompetence.Where(i => !p.CompetencesAcquises.Contains(i.SkillId!.Value)))
        {
            var ligne = joueur.Competences.FirstOrDefault(c => !c.EstCompetenceDepart && c.SkillId == imp.SkillId);
            if (ligne is not null) db.TeamPlayerSkills.Remove(ligne);
            db.PlayerImprovements.Remove(imp);
            traces.Add(("Compétence", imp.Skill?.Nom ?? "?", "—"));
        }
        var dejaAcquises = ameliorationsCompetence.Select(i => i.SkillId!.Value).ToHashSet();
        var palier = joueur.Improvements.Count;
        foreach (var id in p.CompetencesAcquises.Where(id => !dejaAcquises.Contains(id)))
        {
            var skill = competencesVoulues[id];
            var principale = AccesCompetencesHelpers.EstAccesPrincipal(skill, joueur.PlayerPosition.AccesCategories);
            db.PlayerImprovements.Add(new PlayerImprovement
            {
                TeamPlayerId = joueur.Id, Palier = ++palier, PspDepensee = 0, SkillId = id,
                Type = principale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire,
                ValeurHausse = ImprovementThresholds.HausseValeur(
                    principale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire, null)
            });
            db.TeamPlayerSkills.Add(new TeamPlayerSkill
            {
                TeamPlayerId = joueur.Id, SkillId = id, EstCompetenceDepart = false
            });
            traces.Add(("Compétence", "—", skill.Nom));
        }

        // +1 en caractéristique : comparaison de MULTI-ensembles (deux +1 AG).
        var ameliorationsCarac = joueur.Improvements.Where(i => i.SkillId is null && i.StatAmelioree is not null).ToList();
        var voulues = p.Caracteristiques.ToList();
        foreach (var imp in ameliorationsCarac)
        {
            if (voulues.Remove(imp.StatAmelioree!.Value)) continue;
            db.PlayerImprovements.Remove(imp);
            Modifier(joueur, imp.StatAmelioree.Value, -1);
            traces.Add(("Caractéristique", $"+1 {BlessureHelpers.AbregeStat(imp.StatAmelioree.Value)}", "—"));
        }
        foreach (var stat in voulues)
        {
            var type = stat is AffectedStat.Force or AffectedStat.Armure
                ? ImprovementType.AmeliorationForceArmure : ImprovementType.AmeliorationCarac;
            db.PlayerImprovements.Add(new PlayerImprovement
            {
                TeamPlayerId = joueur.Id, Palier = ++palier, PspDepensee = 0, Type = type,
                StatAmelioree = stat, ValeurHausse = ImprovementThresholds.HausseValeur(type, stat)
            });
            Modifier(joueur, stat, +1);
            traces.Add(("Caractéristique", "—", $"+1 {BlessureHelpers.AbregeStat(stat)}"));
        }

        // Blessures durables : retrait = effet inversé, ajout = effet appliqué.
        var gardees = p.Blessures.Where(b => b.Id is not null).Select(b => b.Id!.Value).ToHashSet();
        foreach (var b in durables.Where(b => !gardees.Contains(b.Id)))
        {
            switch (b.Type)
            {
                case InjuryType.Sequelle when b.StatAffectee is { } st: Modifier(joueur, st, +1); break;
                case InjuryType.Mort: joueur.EstMort = false; break;
                case InjuryType.RetraiteTemporaire: joueur.EstRetraite = false; break;
            }
            db.PlayerInjuries.Remove(b);
            traces.Add(("Blessure", BlessureHelpers.Resume(b.Type, b.StatAffectee), "—"));
        }
        foreach (var b in p.Blessures.Where(b => b.Id is null))
        {
            var stat = b.Type == InjuryType.Sequelle ? b.Stat : null;
            db.PlayerInjuries.Add(new PlayerInjury
            {
                TeamPlayerId = joueur.Id, MatchId = null, Type = b.Type, StatAffectee = stat,
                Description = "Saisie libre"
            });
            if (stat is { } st) Modifier(joueur, st, -1);
            if (b.Type == InjuryType.Mort) { joueur.EstMort = true; joueur.EstCapitaine = false; }
            traces.Add(("Blessure", "—", BlessureHelpers.Resume(b.Type, stat)));
        }

        Tracer(joueur.TeamId, userId, sujet, traces);
        await db.SaveChangesAsync();
        logger.LogInformation("Saisie libre : joueur id={JoueurId} modifié par {UserId} ({Nb} changement(s))",
            joueur.Id, userId, traces.Count);
    }

    static string OuiNon(bool b) => b ? "Oui" : "Non";

    static void Modifier(TeamPlayer j, AffectedStat stat, int delta)
    {
        switch (stat)
        {
            case AffectedStat.Mouvement:     j.ModMouvement += delta; break;
            case AffectedStat.Force:         j.ModForce += delta; break;
            case AffectedStat.Agilite:       j.ModAgilite += delta; break;
            case AffectedStat.CapacitePasse: j.ModCapacitePasse += delta; break;
            case AffectedStat.Armure:        j.ModArmure += delta; break;
        }
    }

    /// <summary>
    /// Trésorerie et staff de l'équipe, en une fois.
    /// </summary>
    /// <param name="staff">Quantités COMPLÈTES par LeagueStaffType ; un type
    /// absent est remis à zéro (même contrat que la création).</param>
    public async Task ModifierRessourcesEquipeAsync(
        int teamId, string userId, int tresorerie, IReadOnlyDictionary<int, int> staff)
    {
        var equipe = await EquipeAutoriseeAsync(teamId, userId);
        SaisieLibreRegles.ValiderTresorerie(tresorerie);

        var avantStaff = equipe.Staff.ToDictionary(s => s.LeagueStaffTypeId, s => s.Quantite);
        var typesLigue = await db.LeagueStaffTypes.Where(l => l.LeagueId == equipe.LeagueId).ToListAsync();
        var traces = new List<(string champ, string avant, string apres)>();

        if (equipe.Tresorerie != tresorerie)
            traces.Add(("Trésorerie", $"{equipe.Tresorerie:N0} po", $"{tresorerie:N0} po"));
        foreach (var type in typesLigue.OrderBy(t => t.Ordre))
        {
            var avant = avantStaff.GetValueOrDefault(type.Id);
            var apres = staff.GetValueOrDefault(type.Id);
            if (avant != apres) traces.Add((type.Nom, avant.ToString(), apres.ToString()));
        }

        // Validation complète AVANT toute écriture : un refus au milieu de
        // AppliquerStaffAsync laisserait des lignes modifiées dans le contexte
        // (scopé au circuit), qu'un enregistrement suivant écrirait.
        foreach (var (typeId, quantite) in staff)
        {
            var type = typesLigue.FirstOrDefault(t => t.Id == typeId)
                ?? throw new InvalidOperationException("Ce staff n'existe pas dans cette ligue.");
            if (quantite < 0)
                throw new InvalidOperationException("Une quantité de staff ne peut pas être négative.");
            if (!type.EstActif && quantite > 0)
                throw new InvalidOperationException($"« {type.Nom} » n'est pas disponible dans cette ligue.");
        }
        await teamService.AppliquerStaffAsync(equipe, staff, aLaCreation: false, libre: true);
        equipe.Tresorerie = tresorerie;
        Tracer(equipe.Id, userId, "Équipe", traces);
        await db.SaveChangesAsync();

        logger.LogInformation("Saisie libre : équipe id={TeamId} modifiée par {UserId} ({Nb} changement(s))",
            equipe.Id, userId, traces.Count);
    }

    /// <summary>Historique de l'équipe, du plus récent au plus ancien.</summary>
    public async Task<List<ModificationLibre>> GetHistoriqueAsync(int teamId) =>
        await db.ModificationsLibres.AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .ToListAsync();

    /// <summary>
    /// Historique lisible, pour l'écran du commissaire : auteur par son pseudo
    /// de coach (jamais l'e-mail), « compte supprimé » s'il n'existe plus.
    /// </summary>
    public async Task<List<(ModificationLibre modif, string auteur)>> GetHistoriqueAfficheAsync(int teamId, string userId)
    {
        var equipe = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId)
            ?? throw new InvalidOperationException("Équipe introuvable.");
        if (!await auth.PeutGererLigueAsync(userId, equipe.LeagueId))
            throw new InvalidOperationException("Historique réservé au commissaire de la ligue.");

        var lignes = await GetHistoriqueAsync(teamId);
        var ids = lignes.Select(l => l.UserId).Distinct().ToList();
        var pseudos = await db.Users.Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.PseudoCoach) ? "Coach" : u.PseudoCoach);
        return lignes.Select(l => (l, pseudos.GetValueOrDefault(l.UserId, "compte supprimé"))).ToList();
    }

    void Tracer(int teamId, string userId, string sujet, IEnumerable<(string champ, string avant, string apres)> traces)
    {
        var maintenant = DateTime.UtcNow;
        foreach (var (champ, avant, apres) in traces)
            db.ModificationsLibres.Add(new ModificationLibre
            {
                TeamId = teamId, UserId = userId, Date = maintenant,
                Sujet = sujet, Champ = champ, Avant = avant, Apres = apres
            });
    }

    async Task<Team> EquipeAutoriseeAsync(int teamId, string userId)
    {
        var equipe = await db.Teams
            .Include(t => t.League)
            .Include(t => t.Staff)
            .FirstOrDefaultAsync(t => t.Id == teamId)
            ?? throw new InvalidOperationException("Équipe introuvable.");
        if (!equipe.League.SaisieLibre)
            throw new InvalidOperationException("Cette ligue n'est pas en saisie libre.");
        if (equipe.CoachId != userId && !await auth.PeutGererLigueAsync(userId, equipe.LeagueId))
            throw new InvalidOperationException("Vous ne pouvez pas modifier cette équipe.");
        return equipe;
    }
}
