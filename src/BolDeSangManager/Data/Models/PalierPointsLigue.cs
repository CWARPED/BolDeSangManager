namespace BolDeSangManager.Data.Models;

/// <summary>
/// Palier de points de classement selon le nombre de tours joués dans le match.
///
/// Lecture : « jusqu'au tour <see cref="JusquAuTour"/> INCLUS, une victoire vaut
/// <see cref="PointsVictoire"/>, un nul <see cref="PointsNul"/>, une défaite
/// <see cref="PointsDefaite"/> ». Au-delà du plus grand palier, ce sont les
/// points de base de la ligue qui s'appliquent.
///
/// Exemple (ligue de l'association) : un seul palier à 12 →
/// « victoire avant le 13e tour = 3000, sinon 2000 ».
///
/// Table dédiée plutôt que des colonnes sur League : le nombre de paliers est
/// libre (0 à N), et une ligue sans palier ne porte alors aucune ligne.
/// </summary>
public class PalierPointsLigue
{
    public int Id { get; set; }

    public int LeagueId { get; set; }
    public League League { get; set; } = null!;

    /// <summary>Seuil INCLUSIF, en nombre de tours joués.</summary>
    public int JusquAuTour { get; set; }

    public int PointsVictoire { get; set; }
    public int PointsNul { get; set; }
    public int PointsDefaite { get; set; }
}
