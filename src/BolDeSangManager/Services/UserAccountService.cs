using BolDeSangManager.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Suppression de compte (issue #9).
///
/// Quatre clés étrangères pointent vers <see cref="ApplicationUser"/> en
/// <c>Restrict</c> : <c>Team.CoachId</c>, <c>League.CommissaireId</c>,
/// <c>MatchSheet.SaisiParId</c> et <c>LeagueCommissioner.UserId</c>. Supprimer
/// la ligne d'un coach qui a joué échouerait donc — et passer ces FK en
/// <c>Cascade</c> détruirait l'historique sportif d'autres coaches, ce que le
/// projet interdit.
///
/// D'où deux comportements : suppression dure si le compte n'a laissé aucune
/// trace, anonymisation sinon. Dans les deux cas les données personnelles
/// disparaissent, ce qui satisfait la demande d'effacement RGPD.
///
/// ⚠ Toute NOUVELLE clé étrangère vers ApplicationUser doit être soit
/// nullable + SetNull, soit ajoutée au comptage de
/// <see cref="EvaluerSuppressionAsync"/> — sinon la suppression dure d'un
/// compte considéré comme vierge échouerait au moment du SaveChanges.
/// </summary>
public class UserAccountService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<UserAccountService> logger)
{
    /// <summary>Nom affiché à la place du pseudo d'un compte anonymisé.</summary>
    public const string PseudoAnonyme = "Coach supprimé";

    /// <param name="PeutEtreSupprimeDur">
    /// Vrai si aucune donnée de jeu ne référence ce compte : la ligne peut
    /// réellement disparaître. Faux ⇒ anonymisation.
    /// </param>
    public record VerdictSuppression(
        bool PeutEtreSupprimeDur,
        int NbEquipes,
        int NbLigues,
        int NbFeuilles,
        int NbCommissariats);

    /// <summary>
    /// Que se passera-t-il si on supprime ce compte ? Sert à prévenir
    /// l'utilisateur AVANT qu'il confirme, plutôt que de le découvrir après.
    /// </summary>
    public async Task<VerdictSuppression> EvaluerSuppressionAsync(string userId)
    {
        var nbEquipes = await db.Teams.CountAsync(t => t.CoachId == userId);
        var nbLigues = await db.Leagues.CountAsync(l => l.CommissaireId == userId);
        var nbFeuilles = await db.MatchSheets.CountAsync(f => f.SaisiParId == userId);
        var nbCommissariats = await db.LeagueCommissioners.CountAsync(c => c.UserId == userId);

        var vierge = nbEquipes == 0 && nbLigues == 0 && nbFeuilles == 0 && nbCommissariats == 0;

        return new VerdictSuppression(vierge, nbEquipes, nbLigues, nbFeuilles, nbCommissariats);
    }

    /// <summary>
    /// Supprime un compte : suppression réelle s'il n'a aucune trace,
    /// anonymisation sinon. Dans les deux cas les données personnelles
    /// disparaissent, ce qui satisfait une demande d'effacement RGPD.
    /// </summary>
    /// <param name="parQui">Id de l'admin auteur, ou "self" si c'est le coach lui-même.</param>
    public async Task<IdentityResult> SupprimerCompteAsync(string userId, string parQui)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError
            {
                Code = "IntrouvableOuDejaSupprime",
                Description = "Ce compte n'existe pas."
            });

        // Idempotence : un compte déjà anonymisé n'a plus rien à effacer.
        if (user.EstSupprime) return IdentityResult.Success;

        // Garde-fou : retirer le dernier Admin rendrait l'administration
        // inaccessible à tout le monde, y compris pour réparer l'erreur.
        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count(a => !a.EstSupprime) <= 1)
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DernierAdmin",
                    Description = "Impossible de supprimer le dernier compte Admin. "
                                + "Promouvez d'abord un autre administrateur."
                });
        }

        var verdict = await EvaluerSuppressionAsync(userId);

        if (verdict.PeutEtreSupprimeDur)
        {
            var suppression = await userManager.DeleteAsync(user);
            if (suppression.Succeeded)
                logger.LogInformation(
                    "Compte {UserId} supprimé définitivement (aucune donnée de jeu), par {ParQui}",
                    userId, parQui);
            return suppression;
        }

        return await AnonymiserAsync(user, parQui, verdict);
    }

    /// <summary>
    /// Écrase les données personnelles en gardant la ligne AspNetUsers, sans
    /// laquelle les équipes, ligues et feuilles de match perdraient leur
    /// référence (FK en Restrict).
    /// </summary>
    private async Task<IdentityResult> AnonymiserAsync(
        ApplicationUser user, string parQui, VerdictSuppression verdict)
    {
        var marqueur = user.Id.Replace("-", "")[..8];

        // L'ancienne adresse doit être libérée pour qu'une réinscription reste
        // possible : on la remplace par une adresse du domaine réservé
        // .invalid (RFC 2606), qui ne peut correspondre à personne.
        user.Email = $"compte-supprime-{marqueur}@local.invalid";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.UserName = $"compte-supprime-{marqueur}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.PseudoCoach = PseudoAnonyme;

        user.PasswordHash = null;
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.EmailConfirmed = false;
        user.TwoFactorEnabled = false;
        user.SecurityStamp = Guid.NewGuid().ToString();

        // Connexion définitivement impossible, même si un mot de passe était
        // réintroduit par erreur.
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        user.EstSupprime = true;
        user.SupprimeLe = DateTime.UtcNow;
        user.SupprimePar = parQui;

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
            await userManager.RemoveFromRolesAsync(user, roles);

        // Connexions externes et jetons : autant de données personnelles
        // résiduelles, et autant de portes d'entrée à refermer.
        foreach (var login in await userManager.GetLoginsAsync(user))
            await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

        db.UserTokens.RemoveRange(db.UserTokens.Where(t => t.UserId == user.Id));
        db.UserClaims.RemoveRange(db.UserClaims.Where(c => c.UserId == user.Id));
        await db.SaveChangesAsync();

        var maj = await userManager.UpdateAsync(user);
        if (maj.Succeeded)
            logger.LogInformation(
                "Compte {UserId} anonymisé par {ParQui} — conservé car {Eq} équipe(s), "
                + "{Li} ligue(s), {Fe} feuille(s), {Co} commissariat(s) y font référence",
                user.Id, parQui, verdict.NbEquipes, verdict.NbLigues,
                verdict.NbFeuilles, verdict.NbCommissariats);

        return maj;
    }
}
