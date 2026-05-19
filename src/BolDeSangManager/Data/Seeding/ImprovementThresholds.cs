using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Paliers de Points Star Player (PSP) et hausse de valeur associée selon le LRB Saison 3.
/// </summary>
public static class ImprovementThresholds
{
    /// <summary>
    /// PSP cumulés requis pour atteindre chaque palier (palier 1 = 6 PSP, palier 2 = 16, etc.).
    /// </summary>
    public static readonly int[] PspParPalier = [6, 16, 31, 51, 76, 176];

    /// <summary>
    /// Hausse de la valeur d'un joueur (en pièces d'or) selon le type d'amélioration choisi.
    /// Source : bloodbowl.md §11 (LRB S3).
    /// </summary>
    public static int HausseValeur(ImprovementType type, AffectedStat? stat = null) => type switch
    {
        ImprovementType.AleaPrimaire             => 10_000,
        ImprovementType.SelectionPrimaire        => 20_000,
        ImprovementType.AleaSecondaire           => 20_000,
        ImprovementType.SelectionSecondaire     => 40_000,
        ImprovementType.AmeliorationCarac        => 30_000,
        ImprovementType.AmeliorationForceArmure  => stat == AffectedStat.Force ? 80_000 : 40_000,
        _ => 0
    };

    /// <summary>
    /// Calcule le palier le plus haut atteint pour un nombre donné de PSP cumulés (0 si &lt; 6 PSP).
    /// </summary>
    public static int PalierAtteint(int pspCumules) =>
        PspParPalier.Count(seuil => pspCumules >= seuil);
}
