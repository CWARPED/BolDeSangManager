using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Suppression de compte (issue #9).
///
/// Contrainte structurante : Team.CoachId, League.CommissaireId,
/// MatchSheet.SaisiParId et LeagueCommissioner.UserId pointent vers
/// ApplicationUser en Restrict. Supprimer la ligne d'un coach ayant joué
/// ferait donc échouer la suppression — ou pire, détruirait l'historique
/// sportif d'autres coaches si on passait ces FK en Cascade.
///
/// D'où la règle : suppression dure seulement si le compte n'a aucune trace,
/// anonymisation sinon.
/// </summary>
public class UserAccountServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private (UserAccountService svc, UserManager<ApplicationUser> um, ApplicationDbContext db) Creer()
    {
        var db = _factory.CreateContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>(o => o.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var svc = new UserAccountService(db, um, NullLogger<UserAccountService>.Instance);
        return (svc, um, db);
    }

    private static async Task<ApplicationUser> CreerUserAsync(
        UserManager<ApplicationUser> um, string email, string pseudo = "Coach")
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PseudoCoach = pseudo
        };
        var r = await um.CreateAsync(user, "Password123!");
        Assert.True(r.Succeeded, string.Join(", ", r.Errors.Select(e => e.Description)));
        return user;
    }

    // ─── Verdict ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluer_CompteVierge_AutoriseLaSuppressionDure()
    {
        var (svc, um, _) = Creer();
        var user = await CreerUserAsync(um, "vierge@test.fr");

        var verdict = await svc.EvaluerSuppressionAsync(user.Id);

        Assert.True(verdict.PeutEtreSupprimeDur);
        Assert.Equal(0, verdict.NbEquipes);
        Assert.Equal(0, verdict.NbLigues);
        Assert.Equal(0, verdict.NbFeuilles);
        Assert.Equal(0, verdict.NbCommissariats);
    }

    [Fact]
    public async Task Evaluer_CoachAvecEquipe_ImposeLAnonymisation()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "avec-equipe@test.fr");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);

        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId, "Les Testeurs");

        var verdict = await svc.EvaluerSuppressionAsync(user.Id);

        Assert.False(verdict.PeutEtreSupprimeDur);
        Assert.Equal(1, verdict.NbEquipes);
    }

    [Fact]
    public async Task Evaluer_CommissaireDUneLigue_ImposeLAnonymisation()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "commissaire@test.fr");
        var (gameId, versionId, _) = await SeedRefsAsync(db);
        await DataSeeder.SeedLeagueAsync(db, gameId, versionId, user.Id);

        var verdict = await svc.EvaluerSuppressionAsync(user.Id);

        Assert.False(verdict.PeutEtreSupprimeDur);
        Assert.Equal(1, verdict.NbLigues);
    }

    [Fact]
    public async Task Evaluer_AyantSaisiUneFeuille_ImposeLAnonymisation()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "saisisseur@test.fr");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);

        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, tiers.Id, teamTypeId, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, tiers.Id, teamTypeId, "Ext");
        var match = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id);

        db.MatchSheets.Add(new MatchSheet { MatchId = match.Id, SaisiParId = user.Id });
        await db.SaveChangesAsync();

        var verdict = await svc.EvaluerSuppressionAsync(user.Id);

        Assert.False(verdict.PeutEtreSupprimeDur);
        Assert.Equal(1, verdict.NbFeuilles);
    }

    [Fact]
    public async Task Evaluer_PromuCommissaireDeLigue_ImposeLAnonymisation()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "promu@test.fr");
        var (gameId, versionId, _) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);

        db.LeagueCommissioners.Add(new LeagueCommissioner { LeagueId = ligue.Id, UserId = user.Id });
        await db.SaveChangesAsync();

        var verdict = await svc.EvaluerSuppressionAsync(user.Id);

        Assert.False(verdict.PeutEtreSupprimeDur);
        Assert.Equal(1, verdict.NbCommissariats);
    }

    // ─── Helpers de seed ──────────────────────────────────────────────────
    //
    // On s'appuie sur le DataSeeder du projet : il connaît les FK obligatoires
    // (une ligue exige un commissaire réel, une équipe un TeamType valide).
    // Refaire ce seed à la main revenait à redécouvrir ces contraintes une par
    // une au fil des violations de clé étrangère.

    private async Task<(int gameId, int versionId, int teamTypeId)> SeedRefsAsync(
        ApplicationDbContext db)
    {
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, version.Id);
        return (game.Id, version.Id, teamType.Id);
    }

    /// <summary>Compte tiers, pour les FK qui exigent un utilisateur réel.</summary>
    private static async Task<ApplicationUser> CreerTiersAsync(ApplicationDbContext db)
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var tiers = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"tiers-{suffixe}@test.fr",
            NormalizedUserName = $"TIERS-{suffixe}@TEST.FR",
            Email = $"tiers-{suffixe}@test.fr",
            NormalizedEmail = $"TIERS-{suffixe}@TEST.FR",
            SecurityStamp = Guid.NewGuid().ToString(),
            PseudoCoach = "Commissaire tiers"
        };
        db.Users.Add(tiers);
        await db.SaveChangesAsync();
        return tiers;
    }
}
