using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// #28 — Haines gagnées par un joueur blessé (LRB S3 p.42, « Prendre sa
/// revanche »). La Haine est un TEXTE sur le joueur (<see cref="TeamPlayer.Haines"/>),
/// indépendant du catalogue : elle existe donc en Dungeon Bowl comme en Blood
/// Bowl. Elle ne change pas la valeur du joueur (conforme au livre).
///
/// Ce service porte la saisie depuis la page de l'équipe (oubli, perte hors
/// saison) et les suggestions. La saisie sur la feuille de match passe par
/// MatchService. Droits : le coach de l'équipe, et le commissaire de la ligue.
/// </summary>
public class HaineService(
    ApplicationDbContext db,
    IAuthorizationService auth,
    ILogger<HaineService> logger)
{
    private const string Prefixe = "Haine (";

    /// <summary>
    /// Cibles proposées à la saisie : les mots-clés des postes de la version
    /// (Orque, Nain, Mort-Vivant… — présents en Dungeon Bowl aussi), sans les
    /// mots-clés de poste exclus par le livre, plus les cibles des Traits
    /// « Haine (…) » du catalogue s'il y en a. Simple aide : la saisie reste libre.
    /// </summary>
    public async Task<List<string>> GetSuggestionsAsync(int rulesVersionId)
    {
        var motsCles = await db.PlayerPositions.AsNoTracking()
            .Where(p => p.TeamType.RulesVersionId == rulesVersionId && p.MotsCles != "")
            .Select(p => p.MotsCles)
            .ToListAsync();
        var traits = await db.Skills.AsNoTracking()
            .Where(s => s.RulesVersionId == rulesVersionId && s.Nom.StartsWith(Prefixe))
            .Select(s => s.Nom)
            .ToListAsync();

        return motsCles.SelectMany(m => m.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Concat(traits.Select(n => n[Prefixe.Length..].TrimEnd(')').Trim()))
            .Where(m => m.Length > 0 && !HaineHelpers.MotsClesDePoste.Contains(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>
    /// Traits « Haine (…) » du catalogue, par nom : seulement pour afficher une
    /// description quand la version en a une. Vide en Dungeon Bowl 2022.
    /// </summary>
    public async Task<Dictionary<string, Skill>> GetTraitsCatalogueAsync(int rulesVersionId) =>
        (await db.Skills.AsNoTracking()
            .Include(s => s.SkillCategoryDef)
            .Where(s => s.RulesVersionId == rulesVersionId && s.Nom.StartsWith(Prefixe))
            .ToListAsync())
        .GroupBy(s => s.Nom, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    public async Task AjouterAsync(int joueurId, string? saisie, string userId)
    {
        var joueur = await JoueurAutoriseAsync(joueurId, userId);
        if (joueur.EstMort)
            throw new InvalidOperationException("Ce joueur est mort.");

        var cible = HaineHelpers.Normaliser(saisie)
            ?? throw new InvalidOperationException("Indiquez contre qui (ex. Orque).");
        if (HaineHelpers.Contient(joueur.Haines, cible) || await ALaHaineDansSonProfilAsync(db, joueur, cible))
            throw new InvalidOperationException($"{joueur.Nom} a déjà {HaineHelpers.Libelle(cible)}.");

        joueur.Haines = HaineHelpers.Ajouter(joueur.Haines, cible);
        await db.SaveChangesAsync();
        logger.LogInformation("Joueur {Nom} (id={Id}) gagne Haine ({Cible}) (page de l'équipe, par {UserId})",
            joueur.Nom, joueur.Id, cible, userId);
    }

    public async Task RetirerAsync(int joueurId, string cible, string userId)
    {
        var joueur = await JoueurAutoriseAsync(joueurId, userId);
        if (!HaineHelpers.Contient(joueur.Haines, cible))
        {
            if (await ALaHaineDansSonProfilAsync(db, joueur, cible))
                throw new InvalidOperationException(
                    "Cette Haine fait partie du profil du poste : elle ne se retire pas.");
            throw new InvalidOperationException("Ce joueur n'a pas cette Haine.");
        }

        joueur.Haines = HaineHelpers.Retirer(joueur.Haines, cible);
        await db.SaveChangesAsync();
        logger.LogInformation("Joueur {Nom} (id={Id}) perd Haine ({Cible}) (page de l'équipe, par {UserId})",
            joueur.Nom, joueur.Id, cible, userId);
    }

    /// <summary>
    /// Le joueur a-t-il déjà « Haine (cible) » comme COMPÉTENCE — Haine (Troll)
    /// du Tueur de Trolls, ou Trait acquis autrement ? Partagé avec MatchService
    /// pour ne jamais afficher deux fois la même Haine.
    /// </summary>
    public static async Task<bool> ALaHaineDansSonProfilAsync(ApplicationDbContext db, TeamPlayer joueur, string cible)
    {
        var nom = HaineHelpers.Libelle(cible).ToLower();
        return await db.TeamPlayerSkills.AnyAsync(s => s.TeamPlayerId == joueur.Id && s.Skill.Nom.ToLower() == nom)
            || await db.PlayerPositionSkills.AnyAsync(s => s.PlayerPositionId == joueur.PlayerPositionId && s.Skill.Nom.ToLower() == nom);
    }

    private async Task<TeamPlayer> JoueurAutoriseAsync(int joueurId, string userId)
    {
        var joueur = await db.TeamPlayers
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == joueurId)
            ?? throw new InvalidOperationException("Joueur introuvable");

        if (joueur.Team.CoachId != userId && !await auth.PeutGererLigueAsync(userId, joueur.Team.LeagueId))
            throw new InvalidOperationException(
                "Seuls le coach de l'équipe et le commissaire de la ligue peuvent modifier les Haines.");
        return joueur;
    }
}
