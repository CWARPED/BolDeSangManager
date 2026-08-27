namespace BolDeSangManager.Services;

/// <summary>
/// Prévient en temps réel les écrans ouverts qu'une ligue a changé.
///
/// Le problème résolu : une page ligue lit la base UNE SEULE FOIS, à son
/// ouverture. Blazor Server garde le circuit vivant, donc la page paraît
/// active alors que ses données sont figées. Concrètement, un coach qui avait
/// la page ouverte pendant que le commissaire lançait la saison continuait de
/// voir « Inscriptions » — et le bloc « Proposer une rencontre », réservé aux
/// ligues en cours, n'existait tout simplement pas chez lui. Sans rafraîchir à
/// la main, il ne pouvait rien faire et ne comprenait pas pourquoi.
///
/// Pas besoin d'un Hub SignalR dédié : en Blazor Server chaque page possède
/// DÉJÀ son circuit SignalR vers le navigateur. Ce singleton sert simplement de
/// point de rendez-vous entre le circuit qui modifie la ligue et ceux qui
/// l'affichent ; le rafraîchissement redescend ensuite par le circuit existant.
///
/// Volontairement minimal : on diffuse l'identifiant de la ligue, pas son
/// contenu. Chaque page recharge ce dont ELLE a besoin, avec les droits de SON
/// utilisateur — un commissaire et un coach ne voient pas la même chose, et
/// diffuser un état préparé ailleurs risquerait de fuiter (mode brouillard).
/// </summary>
public class LeagueNotificationService(ILogger<LeagueNotificationService> logger)
{
    /// <summary>
    /// Déclenché après une modification de ligue, avec son identifiant.
    ///
    /// ⚠️ Les abonnés sont des composants Blazor : ils DOIVENT se désabonner
    /// dans <c>Dispose</c>, sinon le singleton garde en vie des composants
    /// détruits (fuite mémoire) et tente de rendre des pages fermées.
    /// </summary>
    public event Func<int, Task>? LigueModifiee;

    /// <summary>
    /// Signale que la ligue a changé. Chaque abonné est appelé dans son propre
    /// try/catch : un écran en erreur (circuit déjà fermé, par exemple) ne doit
    /// pas empêcher les autres d'être prévenus, ni faire échouer l'action
    /// métier qui vient de réussir en base.
    /// </summary>
    public async Task NotifierAsync(int ligueId)
    {
        var handlers = LigueModifiee;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<int, Task>>())
        {
            try
            {
                await handler(ligueId);
            }
            catch (Exception ex)
            {
                // Abonné injoignable (circuit déjà fermé) : la diffusion continue,
                // et surtout l'action métier qui vient de réussir n'échoue pas.
                logger.LogWarning(ex, "Écran injoignable lors de la notification de la ligue {Id}", ligueId);
            }
        }
    }
}
