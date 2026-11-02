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
/// Ce service ne fait que LIRE : suggestions de cibles et description d'un
/// Trait du catalogue. La saisie passe par MatchService (feuille de match) et
/// JoueurEditionService (fenêtre « Modifier le joueur » de la page d'équipe).
/// </summary>
public class HaineService(ApplicationDbContext db)
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
}
