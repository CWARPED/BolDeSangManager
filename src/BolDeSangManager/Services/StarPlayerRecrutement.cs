using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Règles communes au recrutement de star players dans une ligue en « Saisie
/// libre » (<see cref="TeamStarPlayer"/>) : quelles stars une race peut
/// recruter, et validation d'une liste voulue. Partagé par la création
/// d'équipe, sa refonte en phase d'inscription et l'ajout depuis la fiche.
/// </summary>
public static class StarPlayerRecrutement
{
    /// <summary>
    /// Nombre de stars au-delà duquel l'écran AVERTIT (LRB : 2 par équipe). En
    /// saisie libre ce n'est jamais un refus.
    /// </summary>
    public const int MaxConseille = 2;

    /// <summary>
    /// Stars du catalogue accessibles à une race : même version de règles, et
    /// soit aucune ligue thématique rattachée (= accessible à tous, choix
    /// produit), soit une ligue thématique commune avec la race.
    /// </summary>
    public static async Task<List<StarPlayer>> AccessiblesAsync(ApplicationDbContext db, int teamTypeId)
    {
        var race = await db.TeamTypes
            .Include(t => t.LiguesListe)
            .FirstOrDefaultAsync(t => t.Id == teamTypeId);

        if (race is null) return [];

        var liguesDeLaRace = race.LiguesListe.Select(x => x.ThemedLeagueId).ToHashSet();

        var candidats = await db.StarPlayers
            .Include(s => s.Ligues)
            .Where(s => s.RulesVersionId == race.RulesVersionId)
            .OrderBy(s => s.Nom)
            .ToListAsync();

        return candidats
            .Where(s => s.Ligues.Count == 0
                        || s.Ligues.Any(x => liguesDeLaRace.Contains(x.ThemedLeagueId)))
            .ToList();
    }

    /// <summary>
    /// Vérifie une liste COMPLÈTE de stars voulues pour une équipe de cette
    /// race et renvoie les fiches correspondantes. Refuse une star inconnue,
    /// non accessible à la race, ou présente deux fois.
    /// </summary>
    /// <param name="dejaRecrutees">Stars que l'équipe a DÉJÀ : gardées même si
    /// l'Admin a changé depuis leurs ligues thématiques — un réglage de
    /// catalogue ne doit pas bloquer l'enregistrement d'une équipe existante.</param>
    public static async Task<List<StarPlayer>> ValiderAsync(
        ApplicationDbContext db, int teamTypeId, IReadOnlyCollection<int> starPlayerIds,
        IReadOnlyCollection<int>? dejaRecrutees = null)
    {
        if (starPlayerIds.Count == 0) return [];

        var accessibles = (await AccessiblesAsync(db, teamTypeId)).ToDictionary(s => s.Id);
        if (dejaRecrutees is { Count: > 0 })
            foreach (var s in await db.StarPlayers.Where(s => dejaRecrutees.Contains(s.Id)).ToListAsync())
                accessibles.TryAdd(s.Id, s);
        var retenues = new List<StarPlayer>();
        foreach (var id in starPlayerIds)
        {
            if (!accessibles.TryGetValue(id, out var star))
            {
                var nom = await db.StarPlayers.Where(s => s.Id == id).Select(s => s.Nom).FirstOrDefaultAsync();
                throw new InvalidOperationException(nom is null
                    ? "Star player inconnu."
                    : $"« {nom} » n'est pas accessible à cette équipe.");
            }
            if (retenues.Contains(star))
                throw new InvalidOperationException($"« {star.Nom} » est déjà dans l'équipe.");
            retenues.Add(star);
        }
        return retenues;
    }
}
