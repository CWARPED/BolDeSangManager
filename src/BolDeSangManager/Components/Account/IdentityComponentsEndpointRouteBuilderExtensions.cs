using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using BolDeSangManager.Components.Account.Pages;
using BolDeSangManager.Data;
using BolDeSangManager.Services;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query = [
                new("ReturnUrl", returnUrl),
                new("Action", ExternalLogin.LoginCallbackAction)];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            // returnUrl peut commencer par "/" — éviter "~//" rejeté par LocalRedirect
            return TypedResults.LocalRedirect($"~/{returnUrl.TrimStart('/')}");
        });

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var downloadLogger = loggerFactory.CreateLogger("ExportDonneesPersonnelles");

        // Export RGPD (droit d'accès). Servi par un endpoint plutôt que par un
        // composant : la réponse est un fichier, pas une page. Accessible depuis
        // « Mon profil » ; l'ancienne page /Account/Manage/PersonalData a été
        // retirée, elle faisait doublon avec /profil.
        accountGroup.MapPost("/ExporterMesDonnees", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] PersonalDataExportService exportService) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var fichier = await exportService.ExporterJsonAsync(user.Id);
            if (fichier is null)
            {
                return Results.NotFound();
            }

            downloadLogger.LogInformation(
                "Le compte {UserId} a exporté ses données personnelles.", user.Id);

            var nom = PersonalDataExportService.NomFichier(user.PseudoCoach);
            return TypedResults.File(fichier, contentType: "application/json", fileDownloadName: nom);
        }).RequireAuthorization();

        return accountGroup;
    }
}
