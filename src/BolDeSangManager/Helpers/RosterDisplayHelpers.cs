using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Un cadre regroupant les postes partageant un même mot-clé soumis à une limite
/// (ex. « Gros Bras : 0/3 »). <see cref="Couleur"/> est attribuée de façon distincte par cadre.
/// </summary>
public sealed record KeywordFrame(string MotCle, int Max, string Couleur, List<PlayerPosition> Postes);

public static class RosterDisplayHelpers
{
    /// <summary>Palette cyclique : une couleur distincte par cadre de mot-clé limité.</summary>
    public static readonly string[] KeywordFramePalette =
        { "#e53935", "#1e88e5", "#43a047", "#fb8c00", "#8e24aa", "#00897b" };

    /// <summary>
    /// Sépare les postes en deux : ceux regroupés sous un mot-clé limité (un cadre coloré par limite)
    /// et les « classiques » (aucun mot-clé limité) affichés en cartes individuelles.
    /// Un poste portant plusieurs mots-clés limités n'est placé que dans le premier cadre correspondant.
    /// </summary>
    public static (List<KeywordFrame> Cadres, List<PlayerPosition> Classiques) GroupePostesParMotCleLimite(
        IEnumerable<PlayerPosition>? postes, IEnumerable<TeamTypeKeywordLimit>? limites)
    {
        var postesList = (postes ?? []).OrderBy(p => p.Cout).ToList();
        var limitesList = (limites ?? []).ToList();

        var cadres = new List<KeywordFrame>();
        var placed = new HashSet<int>();

        for (int i = 0; i < limitesList.Count; i++)
        {
            var lim = limitesList[i];
            var membres = postesList
                .Where(p => !placed.Contains(p.Id) && PositionAMotCle(p, lim.MotCle))
                .ToList();
            foreach (var p in membres) placed.Add(p.Id);

            cadres.Add(new KeywordFrame(
                lim.MotCle,
                lim.Max,
                KeywordFramePalette[i % KeywordFramePalette.Length],
                membres));
        }

        var classiques = postesList.Where(p => !placed.Contains(p.Id)).ToList();
        return (cadres, classiques);
    }

    /// <summary>Vrai si le poste porte le mot-clé donné (MotsCles est une chaîne CSV).</summary>
    public static bool PositionAMotCle(PlayerPosition pos, string motCle) =>
        (pos.MotsCles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(k => k.Equals(motCle, StringComparison.OrdinalIgnoreCase));
}
