using BolDeSangManager.Data;
using BolDeSangManager.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Changement d'adresse email (R8) : circuit avec jeton de confirmation.
/// Vérifie le comportement réel d'Identity, notamment la synchronisation
/// Email / UserName — l'email servant d'identifiant de connexion.
/// </summary>
public class ChangementEmailTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private UserManager<ApplicationUser> CreateUserManager()
    {
        var db = _factory.CreateContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddDataProtection();   // requis par les générateurs de jetons Identity
        services.AddIdentityCore<ApplicationUser>(o => o.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
    }

    private static async Task<ApplicationUser> CreerUtilisateurAsync(
        UserManager<ApplicationUser> um, string email)
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var r = await um.CreateAsync(user, "Password123!");
        Assert.True(r.Succeeded, string.Join(", ", r.Errors.Select(e => e.Description)));
        return user;
    }

    [Fact]
    public async Task ChangementEmail_AvecJetonValide_MetAJourEmailEtUserName()
    {
        var um = CreateUserManager();
        var user = await CreerUtilisateurAsync(um, "ancien@exemple.com");

        const string nouvel = "nouveau@exemple.com";
        var token = await um.GenerateChangeEmailTokenAsync(user, nouvel);

        var result = await um.ChangeEmailAsync(user, nouvel, token);
        Assert.True(result.Succeeded);

        // L'email sert d'identifiant : les deux doivent bouger ensemble,
        // sinon la connexion casse.
        var setName = await um.SetUserNameAsync(user, nouvel);
        Assert.True(setName.Succeeded);

        var recharge = await um.FindByIdAsync(user.Id);
        Assert.Equal(nouvel, recharge!.Email);
        Assert.Equal(nouvel, recharge.UserName);

        // L'ancienne adresse ne permet plus de retrouver le compte
        Assert.Null(await um.FindByEmailAsync("ancien@exemple.com"));
        Assert.NotNull(await um.FindByEmailAsync(nouvel));
    }

    [Fact]
    public async Task ChangementEmail_AvecJetonInvalide_EchoueEtLaisseLAncienneAdresse()
    {
        var um = CreateUserManager();
        var user = await CreerUtilisateurAsync(um, "ancien@exemple.com");

        var result = await um.ChangeEmailAsync(user, "nouveau@exemple.com", "jeton-bidon");
        Assert.False(result.Succeeded);

        var recharge = await um.FindByIdAsync(user.Id);
        Assert.Equal("ancien@exemple.com", recharge!.Email);
        Assert.Equal("ancien@exemple.com", recharge.UserName);
    }

    [Fact]
    public async Task ChangementEmail_JetonEmisPourUneAutreAdresse_EstRefuse()
    {
        var um = CreateUserManager();
        var user = await CreerUtilisateurAsync(um, "ancien@exemple.com");

        // jeton généré pour A, réutilisé pour B → doit échouer
        var token = await um.GenerateChangeEmailTokenAsync(user, "cible-a@exemple.com");
        var result = await um.ChangeEmailAsync(user, "cible-b@exemple.com", token);

        Assert.False(result.Succeeded);
        var recharge = await um.FindByIdAsync(user.Id);
        Assert.Equal("ancien@exemple.com", recharge!.Email);
    }

    [Fact]
    public async Task ChangementEmail_JetonNonReutilisable()
    {
        var um = CreateUserManager();
        var user = await CreerUtilisateurAsync(um, "ancien@exemple.com");

        var token = await um.GenerateChangeEmailTokenAsync(user, "nouveau@exemple.com");
        Assert.True((await um.ChangeEmailAsync(user, "nouveau@exemple.com", token)).Succeeded);
        await um.SetUserNameAsync(user, "nouveau@exemple.com");

        // Rejouer le même lien ne doit pas repasser (le SecurityStamp a changé)
        var rejeu = await um.ChangeEmailAsync(user, "nouveau@exemple.com", token);
        Assert.False(rejeu.Succeeded);
    }

    [Fact]
    public async Task CreationCompte_AvecEmailDejaUtilise_EstRefusee()
    {
        var um = CreateUserManager();
        await CreerUtilisateurAsync(um, "occupe@exemple.com");

        var doublon = new ApplicationUser { UserName = "occupe@exemple.com", Email = "occupe@exemple.com" };
        var result = await um.CreateAsync(doublon, "Password123!");

        Assert.False(result.Succeeded);
    }
}
