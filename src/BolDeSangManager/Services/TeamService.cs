using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class TeamService(ApplicationDbContext db, ILogger<TeamService> logger)
{
    public async Task<List<TeamType>> GetTypesEquipesAsync(int gameId) =>
        await db.TeamTypes
            .Include(t => t.Postes)
            .Where(t => t.GameId == gameId)
            .OrderBy(t => t.Nom)
            .ToListAsync();

    public async Task<List<TeamType>> GetTypesEquipesParVersionAsync(int versionId) =>
        await db.TeamTypes
            .Include(t => t.Postes)
            .Where(t => t.RulesVersionId == versionId)
            .OrderBy(t => t.Nom)
            .ToListAsync();

    public async Task<TeamType?> GetTeamTypeAvecPostesAsync(int teamTypeId) =>
        await db.TeamTypes
            .Include(t => t.Postes)
                .ThenInclude(p => p.CompetencesDepart)
                .ThenInclude(pps => pps.Skill)
            .Include(t => t.Postes)
                .ThenInclude(p => p.AccesCategories)
                .ThenInclude(a => a.SkillCategoryDef)
            .Include(t => t.LimitesMotsCles)
            .FirstOrDefaultAsync(t => t.Id == teamTypeId);

    public async Task<Team?> GetEquipeAsync(int teamId) =>
        await db.Teams
            .Include(t => t.Coach)
            .Include(t => t.TeamType).ThenInclude(tt => tt.Game)
            .Include(t => t.League)
            .Include(t => t.Joueurs)
                .ThenInclude(j => j.PlayerPosition)
                    .ThenInclude(pp => pp.CompetencesDepart)
                    .ThenInclude(pps => pps.Skill)
            .Include(t => t.Joueurs)
                .ThenInclude(j => j.PlayerPosition)
                    .ThenInclude(pp => pp.AccesCategories)
                    .ThenInclude(a => a.SkillCategoryDef)
            .Include(t => t.Joueurs)
                .ThenInclude(j => j.Competences.Where(c => !c.EstCompetenceDepart))
                .ThenInclude(c => c.Skill)
            .Include(t => t.Joueurs)
                .ThenInclude(j => j.Blessures)
            .FirstOrDefaultAsync(t => t.Id == teamId);

    public async Task<List<Team>> GetEquipesCoachAsync(string coachId) =>
        await db.Teams
            .Include(t => t.TeamType).ThenInclude(tt => tt.Game)
            .Include(t => t.League)
            .Include(t => t.Joueurs.Where(j => !j.EstMort && !j.EstRetraite))
            .Where(t => t.CoachId == coachId)
            .OrderByDescending(t => t.CreeLe)
            .ToListAsync();

    public async Task<List<Team>> GetEquipesLigueAsync(int ligueId) =>
        await db.Teams
            .Include(t => t.Coach)
            .Include(t => t.TeamType)
            .Include(t => t.Division)
            .Where(t => t.LeagueId == ligueId)
            .OrderByDescending(t => t.PointsLigue)
            .ToListAsync();

    public async Task<Team> CreerEquipeAsync(Team equipe, List<(int positionId, string nom, int numero)> joueurs)
    {
        var ligue = await db.Leagues.FirstOrDefaultAsync(l => l.Id == equipe.LeagueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        if (ligue.Statut != LeagueStatus.Inscription)
            throw new InvalidOperationException("Création d'équipe possible uniquement en phase Inscription.");

        var teamType = await GetTeamTypeAvecPostesAsync(equipe.TeamTypeId)
            ?? throw new InvalidOperationException("Type d'équipe introuvable");

        ValiderRoster(teamType, joueurs);

        equipe.CreeLe = DateTime.UtcNow;
        db.Teams.Add(equipe);
        await db.SaveChangesAsync();

        foreach (var (positionId, nom, numero) in joueurs)
        {
            var position = teamType.Postes.First(p => p.Id == positionId);
            var joueur = new TeamPlayer
            {
                TeamId = equipe.Id,
                PlayerPositionId = positionId,
                Nom = string.IsNullOrWhiteSpace(nom) ? $"#{numero}" : nom,
                Numero = numero,
                ValeurActuelle = position.Cout,
                RecruteLe = DateTime.UtcNow
            };
            db.TeamPlayers.Add(joueur);
            await db.SaveChangesAsync();

            AjouterCompetencesDepart(joueur.Id, position.CompetencesDepart);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Équipe créée : {NomEquipe} (id={Id}), {NbJoueurs} joueurs initiaux", equipe.Nom, equipe.Id, joueurs.Count);
        return equipe;
    }

    public async Task<Team> ModifierEquipeAsync(
        int teamId,
        string coachId,
        string nouveauNom,
        int tresorerie,
        int nombreRelances,
        int fansDevoues,
        int coachsAssistants,
        int cheerleaders,
        bool apothicaire,
        List<(int positionId, string nom, int numero)> joueurs)
    {
        var equipe = await db.Teams
            .Include(t => t.League)
            .Include(t => t.Joueurs).ThenInclude(j => j.Competences)
            .FirstOrDefaultAsync(t => t.Id == teamId)
            ?? throw new InvalidOperationException("Équipe introuvable");

        if (equipe.CoachId != coachId)
            throw new InvalidOperationException("Vous n'êtes pas le coach de cette équipe.");
        if (equipe.League is null || equipe.League.Statut != LeagueStatus.Inscription)
            throw new InvalidOperationException("Modification possible uniquement en phase Inscription.");

        var teamType = await GetTeamTypeAvecPostesAsync(equipe.TeamTypeId)
            ?? throw new InvalidOperationException("Type d'équipe introuvable");

        ValiderRoster(teamType, joueurs);

        // Supprimer l'ancien roster (compétences puis joueurs)
        var anciennesCompetences = equipe.Joueurs.SelectMany(j => j.Competences).ToList();
        if (anciennesCompetences.Count > 0)
            db.TeamPlayerSkills.RemoveRange(anciennesCompetences);
        if (equipe.Joueurs.Count > 0)
            db.TeamPlayers.RemoveRange(equipe.Joueurs);

        equipe.Nom = nouveauNom;
        equipe.Tresorerie = tresorerie;
        equipe.NombreRelances = nombreRelances;
        equipe.FansDevoues = fansDevoues;
        equipe.NombreCoachsAssistants = coachsAssistants;
        equipe.NombreCheerleaders = cheerleaders;
        equipe.Apothicaire = apothicaire;

        await db.SaveChangesAsync();

        // Recréer le roster
        foreach (var (positionId, nom, numero) in joueurs)
        {
            var position = teamType.Postes.First(p => p.Id == positionId);
            var joueur = new TeamPlayer
            {
                TeamId = equipe.Id,
                PlayerPositionId = positionId,
                Nom = string.IsNullOrWhiteSpace(nom) ? $"#{numero}" : nom,
                Numero = numero,
                ValeurActuelle = position.Cout,
                RecruteLe = DateTime.UtcNow
            };
            db.TeamPlayers.Add(joueur);
            await db.SaveChangesAsync();

            AjouterCompetencesDepart(joueur.Id, position.CompetencesDepart);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Équipe modifiée : {NomEquipe} (id={Id}), {NbJoueurs} joueurs", equipe.Nom, equipe.Id, joueurs.Count);
        return equipe;
    }

    public async Task SupprimerEquipeAsync(int teamId, string coachId)
    {
        var equipe = await db.Teams
            .Include(t => t.League)
            .Include(t => t.Joueurs).ThenInclude(j => j.Competences)
            .FirstOrDefaultAsync(t => t.Id == teamId)
            ?? throw new InvalidOperationException("Équipe introuvable");

        if (equipe.CoachId != coachId)
            throw new InvalidOperationException("Vous n'êtes pas le coach de cette équipe.");
        if (equipe.League is null || equipe.League.Statut != LeagueStatus.Inscription)
            throw new InvalidOperationException("Suppression possible uniquement en phase Inscription.");

        var competences = equipe.Joueurs.SelectMany(j => j.Competences).ToList();
        if (competences.Count > 0)
            db.TeamPlayerSkills.RemoveRange(competences);
        if (equipe.Joueurs.Count > 0)
            db.TeamPlayers.RemoveRange(equipe.Joueurs);
        db.Teams.Remove(equipe);
        await db.SaveChangesAsync();

        logger.LogInformation("Équipe supprimée : {NomEquipe} (id={Id})", equipe.Nom, equipe.Id);
    }

    private static void ValiderRoster(TeamType teamType, List<(int positionId, string nom, int numero)> joueurs)
    {
        // Limites par poste (quantité max)
        foreach (var (posId, _, _) in joueurs)
        {
            var pos = teamType.Postes.FirstOrDefault(p => p.Id == posId)
                ?? throw new InvalidOperationException($"Poste {posId} introuvable");
            var countPoste = joueurs.Count(j => j.positionId == posId);
            if (countPoste > pos.QuantiteMax)
                throw new InvalidOperationException($"Limite dépassée pour {pos.Nom} : maximum {pos.QuantiteMax} par équipe.");
        }

        // Limites par mot-clé (ex : max 3 Gros Bras pour Renégats du Chaos)
        if (teamType.LimitesMotsCles.Count > 0)
        {
            var keywordsParPosition = teamType.Postes.ToDictionary(
                p => p.Id,
                p => p.MotsCles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase));

            foreach (var limite in teamType.LimitesMotsCles)
            {
                var count = joueurs.Count(j =>
                    keywordsParPosition.TryGetValue(j.positionId, out var kws) && kws.Contains(limite.MotCle));
                if (count > limite.Max)
                    throw new InvalidOperationException(
                        $"Limite « {limite.MotCle} » dépassée : maximum {limite.Max} joueurs avec ce mot-clé.");
            }
        }
    }

    public async Task<TeamPlayer> RecruterJoueurAsync(int teamId, int positionId, string nom, int numero)
    {
        var equipe = await db.Teams.FindAsync(teamId)
            ?? throw new InvalidOperationException("Équipe introuvable");
        var position = await db.PlayerPositions
            .Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .FirstOrDefaultAsync(p => p.Id == positionId)
            ?? throw new InvalidOperationException("Poste introuvable");

        if (equipe.Tresorerie < position.Cout)
            throw new InvalidOperationException("Fonds insuffisants.");

        var nbDejaPoste = await db.TeamPlayers
            .CountAsync(j => j.TeamId == teamId && j.PlayerPositionId == positionId
                          && !j.EstMort && !j.EstRetraite);
        if (nbDejaPoste >= position.QuantiteMax)
            throw new InvalidOperationException($"Limite atteinte : maximum {position.QuantiteMax} {position.Nom} par équipe.");

        // Limites par mot-clé
        var limites = await db.Set<TeamTypeKeywordLimit>()
            .Where(l => l.TeamTypeId == position.TeamTypeId)
            .ToListAsync();

        if (limites.Count > 0)
        {
            var posKeywords = position.MotsCles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var limite in limites)
            {
                if (!posKeywords.Contains(limite.MotCle)) continue;

                var posIdsAvecMotCle = await db.PlayerPositions
                    .Where(p => p.TeamTypeId == position.TeamTypeId
                             && (p.MotsCles.Contains(limite.MotCle + ",") || p.MotsCles.EndsWith(limite.MotCle) || p.MotsCles == limite.MotCle))
                    .Select(p => p.Id)
                    .ToListAsync();

                var nbDejaMotCle = await db.TeamPlayers
                    .CountAsync(j => j.TeamId == teamId && posIdsAvecMotCle.Contains(j.PlayerPositionId)
                                  && !j.EstMort && !j.EstRetraite);

                if (nbDejaMotCle >= limite.Max)
                    throw new InvalidOperationException(
                        $"Limite « {limite.MotCle} » atteinte : maximum {limite.Max} joueurs avec ce mot-clé.");
            }
        }

        var joueur = new TeamPlayer
        {
            TeamId = teamId,
            PlayerPositionId = positionId,
            Nom = string.IsNullOrWhiteSpace(nom) ? $"#{numero}" : nom,
            Numero = numero,
            ValeurActuelle = position.Cout,
            RecruteLe = DateTime.UtcNow
        };
        db.TeamPlayers.Add(joueur);
        await db.SaveChangesAsync();

        AjouterCompetencesDepart(joueur.Id, position.CompetencesDepart);

        equipe.Tresorerie -= position.Cout;
        await db.SaveChangesAsync();
        logger.LogInformation("Joueur recruté : {NomJoueur} (poste={Poste}, coût={Cout}) pour l'équipe id={TeamId}", nom, position.Nom, position.Cout, teamId);
        return joueur;
    }

    // Valeur d'équipe actuelle (VEA)
    public int CalculerVEA(Team equipe)
    {
        var totalJoueurs = equipe.Joueurs
            .Where(j => !j.EstMort && !j.EstRetraite)
            .Sum(j => j.ValeurActuelle);

        var coutRelances = equipe.NombreRelances * (equipe.TeamType?.CoutRelance ?? 50_000);
        var coutFans = equipe.FansDevoues * 10_000;
        var coutCoachsAssistants = equipe.NombreCoachsAssistants * 10_000;
        var coutCheerleaders = equipe.NombreCheerleaders * 10_000;
        var coutApothicaire = equipe.Apothicaire ? 50_000 : 0;

        return totalJoueurs + coutRelances + coutFans + coutCoachsAssistants + coutCheerleaders + coutApothicaire;
    }

    /// <param name="rulesVersionId">
    /// Version de règles (R7) : sans ce filtre la liste mélange les versions, et
    /// les catégories d'une autre version ne correspondent à aucun accès de poste.
    /// </param>
    public async Task<List<Skill>> GetCompetencesAsync(string? categorie = null, int? rulesVersionId = null)
    {
        // R7 : la catégorie est nécessaire pour filtrer selon les accès du poste
        var query = db.Skills.Include(s => s.SkillCategoryDef).AsQueryable();
        if (rulesVersionId is int vid)
            query = query.Where(s => s.RulesVersionId == vid);
        if (!string.IsNullOrEmpty(categorie) && Enum.TryParse<SkillCategory>(categorie, out var cat))
            query = query.Where(s => s.Categorie == cat);
        return await query.OrderBy(s => s.Nom).ToListAsync();
    }

    /// <summary>
    /// Applique une amélioration à un joueur en débitant sa cagnotte d'XP (R4).
    ///
    /// Depuis R4 les paliers LRB (6/16/31…) ne commandent plus les améliorations :
    /// le coach saisit lui-même l'XP qu'il consomme, et peut prendre autant
    /// d'améliorations que sa cagnotte le permet.
    /// </summary>
    /// <param name="xpDepensee">XP retirée de la cagnotte. Doit être &gt; 0 et ≤ XP disponible.</param>
    public async Task AppliquerAmeliorationAsync(
        int joueurId,
        ImprovementType type,
        int? skillId = null,
        AffectedStat? statAmelioree = null,
        int? matchSheetId = null,
        int xpDepensee = 0)
    {
        var joueur = await db.TeamPlayers
            .Include(j => j.Improvements)
            .FirstOrDefaultAsync(j => j.Id == joueurId)
            ?? throw new InvalidOperationException("Joueur introuvable");

        if (xpDepensee <= 0)
            throw new InvalidOperationException("L'XP dépensée doit être supérieure à zéro.");

        if (xpDepensee > joueur.PointsStarPlayer)
            throw new InvalidOperationException(
                $"XP insuffisante : {joueur.Nom} dispose de {joueur.PointsStarPlayer} XP, {xpDepensee} demandés.");

        // Validation du type vs paramètres fournis
        bool requiertSkill = type is ImprovementType.AleaPrimaire or ImprovementType.SelectionPrimaire
                               or ImprovementType.AleaSecondaire or ImprovementType.SelectionSecondaire;
        bool requiertStat = type is ImprovementType.AmeliorationCarac or ImprovementType.AmeliorationForceArmure;

        if (requiertSkill && skillId is null)
            throw new InvalidOperationException("Un skillId est requis pour ce type d'amélioration.");
        if (requiertStat && statAmelioree is null)
            throw new InvalidOperationException("Une stat ciblée est requise pour ce type d'amélioration.");

        var prochainPalier = joueur.Improvements.Count + 1;
        var hausse = ImprovementThresholds.HausseValeur(type, statAmelioree);

        // Débit de la cagnotte
        joueur.PointsStarPlayer -= xpDepensee;

        var improvement = new PlayerImprovement
        {
            TeamPlayerId = joueurId,
            Palier = prochainPalier,
            Type = type,
            SkillId = skillId,
            StatAmelioree = statAmelioree,
            ValeurHausse = hausse,
            XpDepensee = xpDepensee,
            MatchSheetId = matchSheetId
        };
        db.PlayerImprovements.Add(improvement);

        // Si skill : ajouter à la liste des compétences acquises (non de départ)
        if (skillId.HasValue)
        {
            db.TeamPlayerSkills.Add(new TeamPlayerSkill
            {
                TeamPlayerId = joueurId,
                SkillId = skillId.Value,
                EstCompetenceDepart = false,
                EnAttenteValidation = false
            });
        }

        // Si stat : appliquer le modificateur
        if (statAmelioree.HasValue)
        {
            switch (statAmelioree.Value)
            {
                case AffectedStat.Mouvement: joueur.ModMouvement++; break;
                case AffectedStat.Force: joueur.ModForce++; break;
                case AffectedStat.Agilite: joueur.ModAgilite++; break;
                case AffectedStat.CapacitePasse: joueur.ModCapacitePasse++; break;
                case AffectedStat.Armure: joueur.ModArmure++; break;
            }
        }

        joueur.ValeurActuelle += hausse;
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Joueur id={JoueurId} : amélioration palier {Palier} (type={Type}, skill={SkillId}, stat={Stat}, hausse={Hausse}, xp dépensée={Xp})",
            joueurId, prochainPalier, type, skillId, statAmelioree, hausse, xpDepensee);
    }

    /// <summary>
    /// Corrige manuellement l'XP d'un joueur (R4) — réservé aux commissaires.
    /// La correction est journalisée dans <see cref="XpCorrection"/> pour rester
    /// auditable auprès des coaches.
    /// </summary>
    public async Task CorrigerXpAsync(int joueurId, int nouvelleValeur, string motif, string commissaireId)
    {
        var joueur = await db.TeamPlayers.FirstOrDefaultAsync(j => j.Id == joueurId)
            ?? throw new InvalidOperationException("Joueur introuvable");

        if (nouvelleValeur < 0)
            throw new InvalidOperationException("L'XP ne peut pas être négative.");

        if (string.IsNullOrWhiteSpace(motif))
            throw new InvalidOperationException("Un motif est requis pour corriger l'XP d'un joueur.");

        var ancienne = joueur.PointsStarPlayer;
        if (ancienne == nouvelleValeur) return;

        joueur.PointsStarPlayer = nouvelleValeur;
        db.XpCorrections.Add(new XpCorrection
        {
            TeamPlayerId   = joueurId,
            AncienneValeur = ancienne,
            NouvelleValeur = nouvelleValeur,
            Motif          = motif.Trim(),
            CorrigeParId   = commissaireId
        });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "XP corrigée par commissaire {Commissaire} : joueur id={JoueurId} {Ancienne} → {Nouvelle} ({Motif})",
            commissaireId, joueurId, ancienne, nouvelleValeur, motif);
    }

    /// <summary>Historique des corrections d'XP d'un joueur, plus récente d'abord.</summary>
    public async Task<List<XpCorrection>> GetCorrectionsXpAsync(int joueurId) =>
        await db.XpCorrections
            .Include(c => c.CorrigePar)
            .Where(c => c.TeamPlayerId == joueurId)
            .OrderByDescending(c => c.CorrigeLe)
            .ToListAsync();

    public async Task<List<PlayerPosition>> GetPostesDisponiblesAsync(int teamTypeId) =>
        await db.PlayerPositions
            .Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Where(p => p.TeamTypeId == teamTypeId)
            .OrderBy(p => p.Cout)
            .ToListAsync();

    private void AjouterCompetencesDepart(int joueurId, IEnumerable<PlayerPositionSkill> competences)
    {
        foreach (var comp in competences)
            db.TeamPlayerSkills.Add(new TeamPlayerSkill
            {
                TeamPlayerId = joueurId,
                SkillId = comp.SkillId,
                EstCompetenceDepart = true
            });
    }
}
