namespace BolDeSangManager.Data.Models;

/// <summary>
/// Palier de points de classement selon le nombre de tours joués dans le match.
///
/// Lecture : « <b>à partir du tour</b> <see cref="APartirDuTour"/> INCLUS, une
/// victoire vaut <see cref="PointsVictoire"/>, un nul <see cref="PointsNul"/>,
/// une défaite <see cref="PointsDefaite"/> ». En dessous du plus petit palier,
/// ce sont les points de base de la ligue qui s'appliquent.
///
/// Exemple (ligue de l'association) : base 3000 / 1500 / 0, un seul palier
/// « à partir du tour 13 » → 2000 / 1500 / 1000.
///
/// Table dédiée plutôt que des colonnes sur League : le nombre de paliers est
/// libre (0 à N), et une ligue sans palier ne porte alors aucune ligne.
/// </summary>
public class PalierPointsLigue
{
    public int Id { get; set; }

    public int LeagueId { get; set; }
    public League League { get; set; } = null!;

    /// <summary>Seuil INCLUSIF, en nombre de tours joués : le palier s'applique
    /// dès que le match a duré ce nombre de tours ou plus.</summary>
    public int APartirDuTour { get; set; }

    public int PointsVictoire { get; set; }
    public int PointsNul { get; set; }
    public int PointsDefaite { get; set; }
}
