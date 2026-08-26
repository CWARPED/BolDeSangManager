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
}
