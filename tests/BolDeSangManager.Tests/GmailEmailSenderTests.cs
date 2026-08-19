using BolDeSangManager.Data;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Tests for GmailEmailSender.
/// Real SMTP is not exercised — only the configuration guard and the exception-rethrow path.
/// </summary>
public class GmailEmailSenderTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private GmailEmailSender CreateSender(ApplicationDbContext db) =>
        new(new SettingsService(db), NullLogger<GmailEmailSender>.Instance);

    private static ApplicationUser FakeUser(string email) =>
        new() { Email = email, UserName = email };

    // ── 0. Détection de configuration (R8 : éviter le faux succès) ─────────────

    [Fact]
    public async Task EstConfigureAsync_SansAucunReglage_RenvoieFalse()
    {
        await using var db = _factory.CreateContext();
        Assert.False(await CreateSender(db).EstConfigureAsync());
    }

    [Fact]
    public async Task EstConfigureAsync_AvecExpediteurMaisSansMotDePasse_RenvoieFalse()
    {
        await using var db = _factory.CreateContext();
        var settings = new SettingsService(db);
        await settings.SetAsync(SettingsService.CleEmailExpediteur, "ligue@exemple.com");
        // pas de mot de passe d'application → configuration incomplète
        Assert.False(await CreateSender(db).EstConfigureAsync());
    }

    [Fact]
    public async Task EstConfigureAsync_AvecExpediteurEtMotDePasse_RenvoieTrue()
    {
        await using var db = _factory.CreateContext();
        var settings = new SettingsService(db);
        await settings.SetAsync(SettingsService.CleEmailExpediteur, "ligue@exemple.com");
        await settings.SetAsync(SettingsService.CleEmailMotDePasse, "mdp-application-16");
        Assert.True(await CreateSender(db).EstConfigureAsync());
    }

    // ── 1. Not configured → silently skipped (no exception) ────────────────────

    [Fact]
    public async Task SendPasswordResetLink_EmailNonConfigure_NeLevePasException()
    {
        await using var db = _factory.CreateContext();
        var sender = CreateSender(db);
        // No settings stored → should return silently
        await sender.SendPasswordResetLinkAsync(FakeUser("dest@exemple.com"), "dest@exemple.com",
            "https://boldesang.example.com/Account/ResetPassword?code=TEST");
    }

    [Fact]
    public async Task SendConfirmationLink_EmailNonConfigure_NeLevePasException()
    {
        await using var db = _factory.CreateContext();
        var sender = CreateSender(db);
        await sender.SendConfirmationLinkAsync(FakeUser("dest@exemple.com"), "dest@exemple.com",
            "https://boldesang.example.com/Account/Confirm?code=TEST");
    }

    [Fact]
    public async Task SendPasswordResetCode_EmailNonConfigure_NeLevePasException()
    {
        await using var db = _factory.CreateContext();
        var sender = CreateSender(db);
        await sender.SendPasswordResetCodeAsync(FakeUser("dest@exemple.com"), "dest@exemple.com", "ABC123");
    }

    // ── 2. Configured but unreachable SMTP → exception propagated ──────────────

    [Fact]
    public async Task SendPasswordResetLink_ConfigureAvecSMTPInjoignable_LeveException()
    {
        await using var db = _factory.CreateContext();
        var settings = new SettingsService(db);
        await settings.SetAsync(SettingsService.CleEmailExpediteur, "test@gmail.com");
        await settings.SetAsync(SettingsService.CleEmailMotDePasse, "motdepasse16car");

        await using var db2 = _factory.CreateContext();
        var sender = CreateSender(db2);
        // smtp.gmail.com:587 is reachable in prod but may refuse auth — either way an exception must propagate
        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendPasswordResetLinkAsync(FakeUser("dest@exemple.com"), "dest@exemple.com",
                "https://boldesang.example.com/Account/ResetPassword?code=TEST"));
    }

    // ── 3. Email keys stored and retrieved correctly via SettingsService ────────

    [Fact]
    public async Task SettingsService_ClesEmail_SontLuesEtEcrites()
    {
        await using var db1 = _factory.CreateContext();
        var svc = new SettingsService(db1);
        await svc.SetAsync(SettingsService.CleEmailExpediteur, "exp@gmail.com");
        await svc.SetAsync(SettingsService.CleEmailNomExpediteur, "Mon Asso");
        await svc.SetAsync(SettingsService.CleEmailMotDePasse, "xxxx xxxx xxxx xxxx");

        await using var db2 = _factory.CreateContext();
        var svc2 = new SettingsService(db2);
        Assert.Equal("exp@gmail.com", await svc2.GetAsync(SettingsService.CleEmailExpediteur));
        Assert.Equal("Mon Asso", await svc2.GetAsync(SettingsService.CleEmailNomExpediteur));
        Assert.Equal("xxxx xxxx xxxx xxxx", await svc2.GetAsync(SettingsService.CleEmailMotDePasse));
    }
}
