using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Services;

/// <summary>
/// Actions réalisées par UNE équipe sur un match, agrégées depuis les lignes
/// joueurs de la feuille. Sert d'entrée au calcul des points bonus.
/// </summary>
public readonly record struct ActionsEquipe(
    int Touchdowns,
    int Eliminations,
    int Interceptions,
    int Passes,
    int Deviations,
    int Agressions);

/// <summary>
/// Palier de points selon le nombre de tours joués : « jusqu'au tour
/// <paramref name="JusquAuTour"/> inclus, une victoire vaut <paramref name="Victoire"/>… ».
/// </summary>
public readonly record struct PalierPoints(
    int JusquAuTour,
    int Victoire,
    int Nul,
    int Defaite);

/// <summary>
/// Barème des points de classement (« points de ligue ») d'un match.
///
/// Même patron que <see cref="XpBareme"/> : les valeurs plates appartiennent à
/// la version de règles, la ligue en prend une copie à sa création. Les PALIERS,
/// eux, n'existent qu'au niveau ligue — c'est un choix de format, pas une règle
/// d'édition.
///
/// Les valeurs par défaut (3 / 1 / 0, aucun bonus, aucun palier) reproduisent
/// exactement le calcul en dur qui existait avant : appliquer ce barème à une
/// ligue existante ne change aucun classement.
/// </summary>
public class BaremePoints
{
    /// <summary>Points de base d'une victoire (au-delà du dernier palier).</summary>
    public int Victoire { get; init; } = 3;

    /// <summary>Points de base d'un match nul.</summary>
    public int Nul { get; init; } = 1;

    /// <summary>Points de base d'une défaite.</summary>
    public int Defaite { get; init; } = 0;

    public int ParTouchdown { get; init; } = 0;
    public int ParElimination { get; init; } = 0;
    public int ParInterception { get; init; } = 0;
    public int ParPasse { get; init; } = 0;
    public int ParDeviation { get; init; } = 0;
    public int ParAgression { get; init; } = 0;

    /// <summary>
    /// Paliers de la ligue, éventuellement vides. Le premier palier (par ordre
    /// croissant de <see cref="PalierPoints.JusquAuTour"/>) dont le seuil est
    /// atteint ou dépassé par le nombre de tours du match s'applique.
    /// </summary>
    public IReadOnlyList<PalierPoints> Paliers { get; init; } = [];

    /// <summary>Barème par défaut : le comportement historique en dur.</summary>
    public static BaremePoints ParDefaut() => new();

    /// <summary>
    /// Barème de référence porté par la version de règles — celui dont héritent
    /// les nouvelles ligues. Jamais de paliers à ce niveau.
    /// </summary>
    public static BaremePoints DeVersion(RulesVersion? version) =>
        version is null
            ? ParDefaut()
            : new BaremePoints
            {
                Victoire        = version.PointsVictoire,
                Nul             = version.PointsNul,
                Defaite         = version.PointsDefaite,
                ParTouchdown    = version.PointsParTouchdown,
                ParElimination  = version.PointsParElimination,
                ParInterception = version.PointsParInterception,
                ParPasse        = version.PointsParPasse,
                ParDeviation    = version.PointsParDeviation,
                ParAgression    = version.PointsParAgression
            };

    /// <summary>
    /// Barème effectif d'une ligue, paliers compris. Repli sur le barème par
    /// défaut si la ligue est inconnue (vue détachée, graphe partiel).
    /// </summary>
    public static BaremePoints DeLigue(League? ligue) =>
        ligue is null
            ? ParDefaut()
            : new BaremePoints
            {
                Victoire        = ligue.PointsVictoire,
                Nul             = ligue.PointsNul,
                Defaite         = ligue.PointsDefaite,
                ParTouchdown    = ligue.PointsParTouchdown,
                ParElimination  = ligue.PointsParElimination,
                ParInterception = ligue.PointsParInterception,
                ParPasse        = ligue.PointsParPasse,
                ParDeviation    = ligue.PointsParDeviation,
                ParAgression    = ligue.PointsParAgression,
                Paliers         = ligue.PaliersPoints
                    .OrderBy(p => p.JusquAuTour)
                    .Select(p => new PalierPoints(p.JusquAuTour, p.PointsVictoire, p.PointsNul, p.PointsDefaite))
                    .ToList()
            };

    /// <summary>Écrit les valeurs plates sur une version de règles.</summary>
    public void AppliquerA(RulesVersion version)
    {
        version.PointsVictoire        = Victoire;
        version.PointsNul             = Nul;
        version.PointsDefaite         = Defaite;
        version.PointsParTouchdown    = ParTouchdown;
        version.PointsParElimination  = ParElimination;
        version.PointsParInterception = ParInterception;
        version.PointsParPasse        = ParPasse;
        version.PointsParDeviation    = ParDeviation;
        version.PointsParAgression    = ParAgression;
    }

    /// <summary>
    /// Écrit les valeurs plates sur une ligue. Ne touche PAS aux paliers : ils
    /// sont une collection, remplacée séparément par le service.
    /// </summary>
    public void AppliquerA(League ligue)
    {
        ligue.PointsVictoire        = Victoire;
        ligue.PointsNul             = Nul;
        ligue.PointsDefaite         = Defaite;
        ligue.PointsParTouchdown    = ParTouchdown;
        ligue.PointsParElimination  = ParElimination;
        ligue.PointsParInterception = ParInterception;
        ligue.PointsParPasse        = ParPasse;
        ligue.PointsParDeviation    = ParDeviation;
        ligue.PointsParAgression    = ParAgression;
    }

    /// <summary>
    /// Points de classement gagnés par une équipe sur un match.
    ///
    /// <paramref name="tours"/> null (feuille saisie avant les paliers, ou ligue
    /// sans palier) ⇒ ce sont les points de BASE qui s'appliquent.
    /// </summary>
    public int PointsEquipe(int tdPour, int tdContre, int? tours, ActionsEquipe actions)
    {
        var (victoire, nul, defaite) = TripletApplicable(tours);

        int points =
            tdPour > tdContre ? victoire :
            tdPour < tdContre ? defaite  : nul;

        points += actions.Touchdowns    * ParTouchdown;
        points += actions.Eliminations  * ParElimination;
        points += actions.Interceptions * ParInterception;
        points += actions.Passes        * ParPasse;
        points += actions.Deviations    * ParDeviation;
        points += actions.Agressions    * ParAgression;

        return points;
    }

    private (int victoire, int nul, int defaite) TripletApplicable(int? tours)
    {
        if (tours is not int t || Paliers.Count == 0)
            return (Victoire, Nul, Defaite);

        foreach (var p in Paliers.OrderBy(p => p.JusquAuTour))
            if (t <= p.JusquAuTour)
                return (p.Victoire, p.Nul, p.Defaite);

        return (Victoire, Nul, Defaite);
    }

    /// <summary>
    /// Agrège les actions d'un côté d'une feuille de match.
    /// </summary>
    public static ActionsEquipe ActionsDe(IEnumerable<MatchPlayerRecord> records, bool coteDomicile)
    {
        var lignes = records.Where(r => r.EstCoteDomicile == coteDomicile).ToList();
        return new ActionsEquipe(
            Touchdowns:    lignes.Sum(r => r.Touchdowns),
            Eliminations:  lignes.Sum(r => r.EliminationsInfligees),
            Interceptions: lignes.Sum(r => r.Interceptions),
            Passes:        lignes.Sum(r => r.Passes),
            Deviations:    lignes.Sum(r => r.Deviations),
            Agressions:    lignes.Sum(r => r.Agressions));
    }
}
