using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
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
public class SaisieLibreService(
    ApplicationDbContext db,
    IAuthorizationService auth,
    TeamService teamService,
    ILogger<SaisieLibreService> logger)
{
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
