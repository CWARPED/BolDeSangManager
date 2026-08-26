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
    /// Filtre une liste de matchs appartenant à PLUSIEURS ligues, en appliquant à
    /// chacune son propre réglage de brouillard.
    ///
    /// Les écrans transverses (accueil, « Mes matchs ») agrègent les matchs de
    /// toutes les équipes du coach : filtrer ligue par ligue est indispensable,
    /// sinon le calendrier masqué sur la fiche de ligue ressort ailleurs.
    /// Le regroupement se fait sur la ligue de la division du match ; un match
    /// sans division rattachée reste visible (format Open, par exemple).
    /// </summary>
    /// <param name="matchs">Matchs de toutes ligues confondues.</param>
    /// <param name="equipesDuCoach">Toutes les équipes du coach, toutes ligues.</param>
    /// <param name="brouillardParLigue">
    /// Pour chaque identifiant de ligue, le mode brouillard y est-il actif ?
    /// Une ligue absente du dictionnaire est traitée comme sans brouillard.
    /// </param>
    /// <param name="commissaireDeLigues">
    /// Ligues dont le coach est commissaire : il y voit tout le calendrier.
    /// </param>
    public static List<Match> FiltrerVisiblesMultiLigues(
        IEnumerable<Match> matchs,
        IReadOnlySet<int> equipesDuCoach,
        IReadOnlyDictionary<int, bool> brouillardParLigue,
        IReadOnlySet<int> commissaireDeLigues)
    {
        var resultat = new List<Match>();

        foreach (var groupe in matchs.GroupBy(m => m.Division?.LeagueId))
        {
            if (groupe.Key is not int ligueId)
            {
                // Hors division (format Open) : aucun calendrier à masquer.
                resultat.AddRange(groupe);
                continue;
            }

            var brouillard = brouillardParLigue.TryGetValue(ligueId, out var actif) && actif;
            var estCommissaire = commissaireDeLigues.Contains(ligueId);

            resultat.AddRange(FiltrerVisibles(groupe, equipesDuCoach, brouillard, estCommissaire));
        }

        return resultat;
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

    /// <summary>
    /// Équipes que le coach affrontera lors de sa prochaine rencontre non jouée.
    /// (Une par match à venir visible : en pratique une seule, sauf si le coach
    /// engage plusieurs équipes dans la même ligue.)
    /// </summary>
    public static HashSet<int> ProchainsAdversaires(
        IEnumerable<Match> tousLesMatchsDeLaLigue,
        IReadOnlySet<int> equipesDuCoach)
    {
        var adversaires = new HashSet<int>();

        // Pour chacune de ses équipes, le prochain match non joué.
        foreach (var equipeId in equipesDuCoach)
        {
            var prochain = tousLesMatchsDeLaLigue
                .Where(m => !EstJoue(m)
                         && (m.EquipeDomicileId == equipeId || m.EquipeExterieurId == equipeId))
                .OrderBy(m => m.Ronde)
                .ThenBy(m => m.Id)
                .FirstOrDefault();

            if (prochain is null) continue;

            var adverse = prochain.EquipeDomicileId == equipeId
                ? prochain.EquipeExterieurId
                : prochain.EquipeDomicileId;

            // Ne jamais masquer une de ses propres équipes (cas d'un coach
            // qui en engage deux et les voit s'affronter).
            if (!equipesDuCoach.Contains(adverse))
                adversaires.Add(adverse);
        }

        return adversaires;
    }

    /// <summary>
    /// Le coach peut-il consulter la fiche de cette équipe ?
    ///
    /// Les fiches d'équipe sont publiques **par choix** : on ne masque donc
    /// QUE celle du prochain adversaire, et uniquement en mode brouillard.
    /// Sinon le coach préparerait sa rencontre en étudiant l'effectif d'en
    /// face — exactement ce que le brouillard cherche à empêcher, et que le
    /// seul masquage du calendrier ne suffit pas à garantir.
    ///
    /// Restent toujours visibles : ses propres équipes, celles des autres
    /// coaches qu'il n'affronte pas tout de suite, et tout pour un commissaire.
    /// </summary>
    public static bool PeutVoirFicheEquipe(
        int equipeCibleId,
        IEnumerable<Match> tousLesMatchsDeLaLigue,
        IReadOnlySet<int> equipesDuCoach,
        bool modeBrouillard,
        bool estCommissaire)
    {
        if (!modeBrouillard || estCommissaire) return true;
        if (equipesDuCoach.Contains(equipeCibleId)) return true;

        return !ProchainsAdversaires(tousLesMatchsDeLaLigue, equipesDuCoach).Contains(equipeCibleId);
    }
}
