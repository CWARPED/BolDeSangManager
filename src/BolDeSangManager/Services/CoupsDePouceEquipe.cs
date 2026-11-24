using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Ce que l'écran envoie pour UN coup de pouce d'une équipe : la quantité,
/// le prix unitaire saisi par le coach (seulement si le catalogue ne le fixe
/// pas) et la star choisie (seulement sur un coup de pouce « Star player »).
/// </summary>
public sealed record CoupDePouceSaisi(int Quantite, int? PrixSaisi = null, int? StarPlayerId = null);

/// <summary>Une ligne voulue, validée contre le catalogue.</summary>
public sealed record CoupDePouceValide(Inducement CoupDePouce, int Quantite, int? PrixSaisi, StarPlayer? Star);

/// <summary>
/// Règles communes aux coups de pouce détenus par une équipe d'une ligue en
/// « Saisie libre » (<see cref="TeamInducement"/>). Partagé par la création
/// d'équipe, sa refonte en phase d'inscription et la fiche d'équipe.
///
/// Tout le catalogue de la version est proposé : la restriction d'un coup de
/// pouce est du texte libre, affichée mais jamais vérifiée. Une quantité
/// au-delà du maximum du catalogue n'est qu'un avertissement.
///
/// Prix : celui du catalogue, sauf quand le catalogue ne le fixe pas
/// (<c>Cout == 0</c>, « prix variable ») — c'est alors le coach qui le saisit.
/// Star : un coup de pouce <see cref="Inducement.EstStarPlayer"/> porte UNE
/// star du catalogue accessible à la race, imprimée avec ses caractéristiques.
/// </summary>
public static class CoupsDePouceEquipe
{
    /// <summary>Garde-fou contre une saisie absurde, pas une règle de jeu.</summary>
    public const int QuantiteMaxSaisie = 99;

    /// <summary>Garde-fou sur un prix saisi (po).</summary>
    public const int PrixMaxSaisi = 5_000_000;

    /// <summary>Le coach saisit-il le prix de ce coup de pouce ?</summary>
    public static bool PrixVariable(Inducement coupDePouce) => coupDePouce.Cout == 0;

    /// <summary>
    /// Prix unitaire effectif d'une ligne : le prix saisi s'il existe, sinon
    /// celui du catalogue. SEULE fonction de prix — VEA, PDF et écrans la lisent.
    /// </summary>
    public static int PrixUnitaire(TeamInducement ligne) =>
        ligne.PrixSaisi ?? ligne.Inducement?.Cout ?? 0;

    /// <summary>Total d'une ligne : prix unitaire × quantité.</summary>
    public static int Total(TeamInducement ligne) => PrixUnitaire(ligne) * ligne.Quantite;

    /// <summary>
    /// Libellé court d'une ligne pour les listes (fiche, PDF) :
    /// « Star Player 1 : Varag Mâche-Goule (280k) », « Pots-de-vin ×2 (100k) ».
    /// </summary>
    public static string Libelle(TeamInducement ligne)
    {
        var nom = ligne.Inducement?.Nom ?? "?";
        if (ligne.StarPlayer is not null) nom += $" : {ligne.StarPlayer.Nom}";
        if (ligne.Quantite > 1) nom += $" ×{ligne.Quantite}";
        var prix = PrixUnitaire(ligne);
        return prix > 0 ? $"{nom} ({prix / 1000}k)" : nom;
    }

    /// <summary>
    /// Stars proposées sur les coups de pouce « Star player » d'une équipe : les
    /// stars accessibles à sa race, plus celles qu'elle détient déjà (restées
    /// valables même si l'Admin a changé leur accès), triées par nom.
    /// </summary>
    public static List<StarPlayer> StarsProposees(IEnumerable<StarPlayer> accessibles, IEnumerable<TeamInducement> detenues) =>
        accessibles.Concat(detenues.Select(d => d.StarPlayer).OfType<StarPlayer>())
            .DistinctBy(s => s.Id).OrderBy(s => s.Nom).ToList();

    /// <summary>
    /// Valide un état COMPLET voulu (id de coup de pouce → saisie ; quantité 0
    /// = aucun) et renvoie les lignes retenues, quantité &gt; 0 seulement.
    /// </summary>
    /// <param name="teamTypeId">Race de l'équipe : filtre des stars proposables.</param>
    /// <param name="starsDejaDetenues">Stars que l'équipe a DÉJÀ : gardées même
    /// si l'Admin a changé depuis leur accès — un réglage de catalogue ne doit
    /// pas bloquer l'enregistrement d'une équipe existante.</param>
    public static async Task<List<CoupDePouceValide>> ValiderAsync(
        ApplicationDbContext db, int rulesVersionId, int teamTypeId,
        IReadOnlyDictionary<int, CoupDePouceSaisi> voulus,
        IReadOnlyCollection<int>? starsDejaDetenues = null)
    {
        if (voulus.Values.Any(v => v.Quantite < 0 || v.Quantite > QuantiteMaxSaisie))
            throw new InvalidOperationException($"Quantité de coup de pouce invalide (0 à {QuantiteMaxSaisie}).");

        var ids = voulus.Where(kv => kv.Value.Quantite > 0).Select(kv => kv.Key).ToList();
        if (ids.Count == 0) return [];

        var catalogue = await db.Inducements
            .Where(i => ids.Contains(i.Id) && i.RulesVersionId == rulesVersionId)
            .ToDictionaryAsync(i => i.Id);
        if (ids.Any(id => !catalogue.ContainsKey(id)))
            throw new InvalidOperationException("Coup de pouce inconnu dans cette version des règles.");

        var starIds = ids.Select(id => voulus[id].StarPlayerId).OfType<int>().ToList();
        if (starIds.Count != starIds.Distinct().Count())
            throw new InvalidOperationException("La même star est choisie deux fois.");
        var stars = (await StarPlayerRecrutement.ValiderAsync(db, teamTypeId, starIds, starsDejaDetenues))
            .ToDictionary(s => s.Id);

        var lignes = new List<CoupDePouceValide>();
        foreach (var id in ids)
        {
            var cp = catalogue[id];
            var saisie = voulus[id];

            if (saisie.PrixSaisi is int prix)
            {
                if (!PrixVariable(cp))
                    throw new InvalidOperationException($"Le prix de « {cp.Nom} » est fixé par le catalogue.");
                if (prix < 0 || prix > PrixMaxSaisi)
                    throw new InvalidOperationException($"Prix de « {cp.Nom} » invalide (0 à {PrixMaxSaisi:N0} po).");
            }

            StarPlayer? star = null;
            if (saisie.StarPlayerId is int starId)
            {
                if (!cp.EstStarPlayer)
                    throw new InvalidOperationException($"« {cp.Nom} » n'est pas un coup de pouce Star player.");
                star = stars[starId];
            }

            // Une ligne star = une star ; sans prix saisi, celui de la star.
            var quantite = star is null ? saisie.Quantite : 1;
            var prixRetenu = PrixVariable(cp) ? saisie.PrixSaisi ?? star?.Cout : null;
            lignes.Add(new CoupDePouceValide(cp, quantite, prixRetenu, star));
        }
        return lignes.OrderBy(x => x.CoupDePouce.Ordre).ThenBy(x => x.CoupDePouce.Nom).ToList();
    }

    /// <summary>Noms des coups de pouce dont la quantité dépasse le maximum du catalogue.</summary>
    public static List<string> AuDelaDuMaximum(IEnumerable<CoupDePouceValide> lignes) =>
        lignes.Where(x => x.CoupDePouce.QuantiteMax > 0 && x.Quantite > x.CoupDePouce.QuantiteMax)
            .Select(x => x.CoupDePouce.Nom).ToList();

    /// <summary>
    /// Écrit l'état voulu sur les lignes d'une équipe (déjà chargées, avec
    /// <c>Inducement</c> et <c>StarPlayer</c>) et renvoie les changements
    /// (nom, avant, après) pour l'historique.
    /// </summary>
    public static List<(string nom, string avant, string apres)> Appliquer(
        ApplicationDbContext db, int teamId, ICollection<TeamInducement> actuelles,
        IReadOnlyList<CoupDePouceValide> voulues)
    {
        var changements = new List<(string, string, string)>();
        var parId = voulues.ToDictionary(v => v.CoupDePouce.Id);

        foreach (var ligne in actuelles.ToList())
        {
            if (parId.TryGetValue(ligne.InducementId, out var v))
            {
                var avant = Decrire(ligne.Quantite, ligne.PrixSaisi, ligne.StarPlayer?.Nom);
                var apres = Decrire(v.Quantite, v.PrixSaisi, v.Star?.Nom);
                if (avant != apres)
                {
                    changements.Add((v.CoupDePouce.Nom, avant, apres));
                    ligne.Quantite = v.Quantite;
                    ligne.PrixSaisi = v.PrixSaisi;
                    ligne.StarPlayerId = v.Star?.Id;
                    ligne.StarPlayer = v.Star;
                }
            }
            else
            {
                changements.Add((ligne.Inducement?.Nom ?? "?",
                    Decrire(ligne.Quantite, ligne.PrixSaisi, ligne.StarPlayer?.Nom), "0"));
                db.TeamInducements.Remove(ligne);
            }
        }

        var deja = actuelles.Select(l => l.InducementId).ToHashSet();
        foreach (var v in voulues.Where(v => !deja.Contains(v.CoupDePouce.Id)))
        {
            db.TeamInducements.Add(new TeamInducement
            {
                TeamId = teamId, InducementId = v.CoupDePouce.Id, Quantite = v.Quantite,
                PrixSaisi = v.PrixSaisi, StarPlayerId = v.Star?.Id
            });
            changements.Add((v.CoupDePouce.Nom, "0", Decrire(v.Quantite, v.PrixSaisi, v.Star?.Nom)));
        }
        return changements;
    }

    /// <summary>« 2 », « 1 · 50 000 po », « 1 · Varag Mâche-Goule · 280 000 po ».</summary>
    static string Decrire(int quantite, int? prix, string? star) =>
        string.Join(" · ", new[]
        {
            quantite.ToString(),
            star,
            prix is int p ? $"{p:N0} po" : null
        }.Where(x => !string.IsNullOrEmpty(x)));
}
