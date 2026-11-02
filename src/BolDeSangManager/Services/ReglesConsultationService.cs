using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Lecture seule du contenu d'une version de règles pour la page « Règles »
/// (#20) : races / collèges et leurs postes, compétences, star players, texte
/// de présentation (#22). Aucune écriture ici — la saisie reste dans
/// « Édition des données » (DataEditService).
///
/// Requêtes sans suivi EF : la page est consultée par tous les membres et ne
/// doit jamais relire un cache de circuit périmé.
/// </summary>
public class ReglesConsultationService(ApplicationDbContext db)
{
    /// <summary>Toutes les versions, groupées par jeu puis dans leur ordre.</summary>
    public async Task<List<RulesVersion>> GetVersionsAsync() =>
        await db.RulesVersions.AsNoTracking()
            .Include(v => v.Game)
            .OrderBy(v => v.GameId).ThenBy(v => v.Ordre).ThenBy(v => v.Nom)
            .ToListAsync();

    /// <summary>
    /// Version proposée à l'ouverture : la version active du premier jeu
    /// (Blood Bowl), sinon la première venue.
    /// </summary>
    public static RulesVersion? VersionParDefaut(IReadOnlyList<RulesVersion> versions) =>
        versions.FirstOrDefault(v => v.EstActive) ?? versions.FirstOrDefault();

    public async Task<ContenuRegles?> GetContenuAsync(int versionId)
    {
        var version = await db.RulesVersions.AsNoTracking()
            .Include(v => v.Game)
            .FirstOrDefaultAsync(v => v.Id == versionId);
        if (version is null) return null;

        var races = await db.TeamTypes.AsNoTracking().AsSplitQuery()
            .Where(t => t.RulesVersionId == versionId)
            .Include(t => t.Postes).ThenInclude(p => p.CompetencesDepart).ThenInclude(c => c.Skill)
                .ThenInclude(s => s.SkillCategoryDef)   // la fiche affiche la catégorie
            .Include(t => t.Postes).ThenInclude(p => p.AccesCategories).ThenInclude(a => a.SkillCategoryDef)
            .Include(t => t.ReglesSpecialesListe).ThenInclude(r => r.SpecialRule)
            .Include(t => t.LiguesListe).ThenInclude(l => l.ThemedLeague)
            .OrderBy(t => t.Nom)
            .ToListAsync();

        var competences = await db.Skills.AsNoTracking()
            .Where(s => s.RulesVersionId == versionId)
            .Include(s => s.SkillCategoryDef)
            .OrderBy(s => s.Nom)
            .ToListAsync();

        var stars = await db.StarPlayers.AsNoTracking()
            .Where(s => s.RulesVersionId == versionId)
            .Include(s => s.Ligues).ThenInclude(l => l.ThemedLeague)
            .OrderBy(s => s.Nom)
            .ToListAsync();

        return new ContenuRegles(version, races, competences, stars);
    }

    /// <summary>
    /// Star players qu'une race peut engager — MÊME règle que le recrutement
    /// (<see cref="StarPlayer.EstAccessible"/> ; un test le vérifie contre
    /// <c>DataEditService.GetStarPlayersAccessiblesAsync</c>) : une ligue en
    /// commun, ou aucune ligue (ouvert à tous).
    /// </summary>
    public static IEnumerable<StarPlayer> StarsAccessibles(IEnumerable<StarPlayer> stars, TeamType race)
    {
        var ligues = race.LiguesListe.Select(l => l.ThemedLeagueId).ToHashSet();
        return stars.Where(s => s.EstAccessible(ligues));
    }
}

public record ContenuRegles(
    RulesVersion Version,
    List<TeamType> Races,
    List<Skill> Competences,
    List<StarPlayer> StarPlayers);
