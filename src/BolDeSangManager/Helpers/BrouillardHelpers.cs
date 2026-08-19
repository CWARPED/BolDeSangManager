using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Mode brouillard (#2) : visibilité du calendrier pour un coach.
///
/// Quand une ligue active <see cref="League.ModeBrouillard"/>, un coach ne doit pas
/// pouvoir préparer ses rencontres futures pendant qu'il joue la ronde courante.
/// Il voit donc :
///  • tous les matchs <b>déjà joués</b> (les siens et ceux des autres équipes) ;
///  • son <b>prochain match</b> non joué (le plus petit numéro de ronde parmi ses matchs à venir) ;
///  • rien d'autre du calendrier à venir — y compris ses propres matchs ultérieurs.
///
/// Les fiches d'équipe restent consultables : l'objectif est de masquer le
/// calendrier, pas les effectifs.
/// </summary>
public static class BrouillardHelpers
{
    /// <summary>Un match est « joué » dès qu'un résultat est acquis ou en cours de validation.</summary>
    public static bool EstJoue(Match m) =>
        m.Statut is MatchStatus.Termine or MatchStatus.Concede
                 or MatchStatus.ValidationCompetences or MatchStatus.FeuilleEnSaisie
        || m.ScoreDomicile.HasValue;

    /// <summary>Le coach participe-t-il à ce match ?</summary>
    public static bool EstImplique(Match m, IReadOnlySet<int> equipesDuCoach) =>
        equipesDuCoach.Contains(m.EquipeDomicileId) || equipesDuCoach.Contains(m.EquipeExterieurId);

    /// <summary>
    /// Filtre une liste de matchs selon la règle du mode brouillard.
    /// </summary>
    /// <param name="matchs">Matchs à filtrer (typiquement ceux d'une division).</param>
    /// <param name="equipesDuCoach">Identifiants des équipes du coach dans cette ligue.</param>
    /// <param name="modeBrouillard">Option active sur la ligue.</param>
    /// <param name="estCommissaire">Un commissaire voit toujours l'intégralité du calendrier.</param>
    public static List<Match> FiltrerVisibles(
        IEnumerable<Match> matchs,
        IReadOnlySet<int> equipesDuCoach,
        bool modeBrouillard,
        bool estCommissaire)
    {
        var tous = matchs.ToList();

        if (!modeBrouillard || estCommissaire)
            return tous;

        // Prochain match du coach = ronde la plus basse parmi ses matchs non joués.
        var prochainId = tous
            .Where(m => !EstJoue(m) && EstImplique(m, equipesDuCoach))
            .OrderBy(m => m.Ronde)
            .ThenBy(m => m.Id)
            .Select(m => (int?)m.Id)
            .FirstOrDefault();

        return tous
            .Where(m => EstJoue(m) || m.Id == prochainId)
            .ToList();
    }

    /// <summary>
    /// Ce match précis est-il visible ? Même règle que <see cref="FiltrerVisibles"/>,
    /// pour protéger l'accès direct à une page de match (le masquage doit être
    /// côté serveur, pas seulement visuel).
    /// </summary>
    public static bool EstVisible(
        Match match,
        IEnumerable<Match> tousLesMatchsDeLaLigue,
        IReadOnlySet<int> equipesDuCoach,
        bool modeBrouillard,
        bool estCommissaire) =>
        FiltrerVisibles(tousLesMatchsDeLaLigue, equipesDuCoach, modeBrouillard, estCommissaire)
            .Any(m => m.Id == match.Id);
}
