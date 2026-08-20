namespace BolDeSangManager.Services;

/// <summary>
/// Contrôle de traçabilité des fichiers JSON importés (F3).
///
/// Un export porte désormais un numéro de révision et une date. Avant
/// d'appliquer un fichier sur une version existante, on compare ce numéro à
/// celui de la base pour repérer le cas dangereux : réimporter un fichier
/// plus ancien que ce qu'on a déjà, et donc écraser des corrections récentes
/// par des anciennes sans s'en apercevoir.
///
/// Le contrôle AVERTIT, il ne bloque pas : il peut être légitime de revenir en
/// arrière volontairement. C'est à l'utilisateur de trancher en connaissance
/// de cause.
/// </summary>
public static class TracabiliteImport
{
    public enum Verdict
    {
        /// <summary>Le fichier est plus récent que la base : cas normal.</summary>
        PlusRecent,

        /// <summary>Même révision : le fichier a déjà été intégré, ou rien n'a bougé.</summary>
        Identique,

        /// <summary>⚠️ Le fichier est plus ancien : risque d'écraser des corrections.</summary>
        PlusAncien,

        /// <summary>Fichier exporté avant F3 : aucune traçabilité disponible.</summary>
        Inconnue,
    }

    public record Controle(Verdict Verdict, string Message)
    {
        /// <summary>Vrai quand l'utilisateur devrait confirmer avant d'appliquer.</summary>
        public bool DemandeConfirmation => Verdict is Verdict.PlusAncien or Verdict.Identique;
    }

    /// <param name="revisionFichier">Révision portée par le JSON, null si absente.</param>
    /// <param name="revisionBase">Révision de la version ciblée en base.</param>
    /// <param name="exporteLe">Date d'export portée par le JSON, si présente.</param>
    public static Controle Verifier(int? revisionFichier, int revisionBase, DateTime? exporteLe = null)
    {
        if (revisionFichier is null)
            return new Controle(Verdict.Inconnue,
                "Ce fichier ne porte pas de numéro de révision (export antérieur à cette " +
                "fonctionnalité). Impossible de vérifier s'il est plus récent que vos données.");

        var quand = exporteLe.HasValue
            ? $" (exporté le {exporteLe.Value.ToLocalTime():dd/MM/yyyy à HH'h'mm})"
            : "";

        if (revisionFichier > revisionBase)
            return new Controle(Verdict.PlusRecent,
                $"Fichier en révision {revisionFichier}{quand}, vos données sont en " +
                $"révision {revisionBase}. Le fichier est plus récent.");

        if (revisionFichier == revisionBase)
            return new Controle(Verdict.Identique,
                $"Fichier et données sont tous deux en révision {revisionFichier}{quand}. " +
                "Ce fichier a probablement déjà été intégré.");

        return new Controle(Verdict.PlusAncien,
            $"⚠️ Ce fichier est en révision {revisionFichier}{quand}, alors que vos données " +
            $"sont déjà en révision {revisionBase}. L'appliquer risque d'écraser des " +
            "corrections plus récentes par des anciennes.");
    }
}
