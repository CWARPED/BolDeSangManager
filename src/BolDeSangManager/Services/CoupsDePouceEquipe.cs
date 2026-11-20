using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Règles communes aux coups de pouce détenus par une équipe d'une ligue en
/// « Saisie libre » (<see cref="TeamInducement"/>). Partagé par la création
/// d'équipe, sa refonte en phase d'inscription et la fiche d'équipe.
///
/// Tout le catalogue de la version est proposé : la restriction d'un coup de
/// pouce est du texte libre, affichée mais jamais vérifiée. Une quantité
/// au-delà du maximum du catalogue n'est qu'un avertissement.
/// </summary>
public static class CoupsDePouceEquipe
{
    /// <summary>Garde-fou contre une saisie absurde, pas une règle de jeu.</summary>
    public const int QuantiteMaxSaisie = 99;

    /// <summary>
    /// Valide un état COMPLET voulu (id de coup de pouce → quantité ; 0 = aucun)
    /// et renvoie les coups de pouce concernés, quantité &gt; 0 seulement.
    /// </summary>
    public static async Task<List<(Inducement coupDePouce, int quantite)>> ValiderAsync(
        ApplicationDbContext db, int rulesVersionId, IReadOnlyDictionary<int, int> quantites)
    {
        if (quantites.Values.Any(q => q < 0 || q > QuantiteMaxSaisie))
            throw new InvalidOperationException($"Quantité de coup de pouce invalide (0 à {QuantiteMaxSaisie}).");

        var ids = quantites.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
        if (ids.Count == 0) return [];

        var catalogue = await db.Inducements
            .Where(i => ids.Contains(i.Id) && i.RulesVersionId == rulesVersionId)
            .ToDictionaryAsync(i => i.Id);
        if (ids.Any(id => !catalogue.ContainsKey(id)))
            throw new InvalidOperationException("Coup de pouce inconnu dans cette version des règles.");

        return ids.Select(id => (catalogue[id], quantites[id]))
            .OrderBy(x => x.Item1.Ordre).ThenBy(x => x.Item1.Nom).ToList();
    }

    /// <summary>Noms des coups de pouce dont la quantité dépasse le maximum du catalogue.</summary>
    public static List<string> AuDelaDuMaximum(IEnumerable<(Inducement coupDePouce, int quantite)> lignes) =>
        lignes.Where(x => x.coupDePouce.QuantiteMax > 0 && x.quantite > x.coupDePouce.QuantiteMax)
            .Select(x => x.coupDePouce.Nom).ToList();

    /// <summary>
    /// Écrit l'état voulu sur les lignes d'une équipe (déjà chargées) et renvoie
    /// les changements (nom, avant, après) pour l'historique.
    /// </summary>
    public static List<(string nom, int avant, int apres)> Appliquer(
        ApplicationDbContext db, int teamId, ICollection<TeamInducement> actuelles,
        IReadOnlyList<(Inducement coupDePouce, int quantite)> voulues)
    {
        var changements = new List<(string, int, int)>();
        var parId = voulues.ToDictionary(v => v.coupDePouce.Id);

        foreach (var ligne in actuelles.ToList())
        {
            if (parId.TryGetValue(ligne.InducementId, out var v))
            {
                if (ligne.Quantite != v.quantite)
                {
                    changements.Add((v.coupDePouce.Nom, ligne.Quantite, v.quantite));
                    ligne.Quantite = v.quantite;
                }
            }
            else
            {
                changements.Add((ligne.Inducement?.Nom ?? "?", ligne.Quantite, 0));
                db.TeamInducements.Remove(ligne);
            }
        }

        var deja = actuelles.Select(l => l.InducementId).ToHashSet();
        foreach (var (cp, q) in voulues.Where(v => !deja.Contains(v.coupDePouce.Id)))
        {
            db.TeamInducements.Add(new TeamInducement { TeamId = teamId, InducementId = cp.Id, Quantite = q });
            changements.Add((cp.Nom, 0, q));
        }
        return changements;
    }
}
