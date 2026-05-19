using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BolDeSangManager.Tests;

public class AuthorizationServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<(AuthorizationService service, UserManager<ApplicationUser> um, ApplicationDbContext db)>
        CreateServiceAsync()
    {
        var db = _factory.CreateContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "GrandCommissaire", "Coach" })
        {
            if (!await rm.RoleExistsAsync(role))
                await rm.CreateAsync(new IdentityRole(role));
        }

        var service = new AuthorizationService(db, um);
        return (service, um, db);
    }

    private async Task<ApplicationUser> CreateUserWithRoleAsync(UserManager<ApplicationUser> um, string suffix, string role)
    {
        var user = new ApplicationUser
        {
            UserName = $"u{suffix}@test.fr",
            Email = $"u{suffix}@test.fr",
            EmailConfirmed = true,
            PseudoCoach = $"User{suffix}"
        };
        await um.CreateAsync(user, "Password123!");
        await um.AddToRoleAsync(user, role);
        return user;
    }

    [Fact]
    public async Task EstAdmin_ReturnsTrueForAdminUser()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "admin", "Admin");
        Assert.True(await svc.EstAdminAsync(admin.Id));
    }

    [Fact]
    public async Task EstAdmin_ReturnsFalseForCoach()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "coach", "Coach");
        Assert.False(await svc.EstAdminAsync(coach.Id));
    }

    [Fact]
    public async Task EstGrandCommissaire_ReturnsTrueForGCUser()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var gc = await CreateUserWithRoleAsync(um, "gc", "GrandCommissaire");
        Assert.True(await svc.EstGrandCommissaireAsync(gc.Id));
    }

    [Fact]
    public async Task EstCommissaireDeLigue_TrueQuandLeagueCommissionerExiste()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "lc1", "Coach");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var commissaire = await CreateUserWithRoleAsync(um, "creator", "Admin");
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);

        db.LeagueCommissioners.Add(new LeagueCommissioner
        {
            LeagueId = ligue.Id,
            UserId = coach.Id,
            AssignePar = commissaire.Id
        });
        await db.SaveChangesAsync();

        Assert.True(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue.Id));
    }

    [Fact]
    public async Task EstCommissaireDeLigue_FalseDansAutreLigue()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "lc2", "Coach");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var admin = await CreateUserWithRoleAsync(um, "admin2", "Admin");
        var ligue1 = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);
        var ligue2 = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        db.LeagueCommissioners.Add(new LeagueCommissioner { LeagueId = ligue1.Id, UserId = coach.Id });
        await db.SaveChangesAsync();

        Assert.True(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue1.Id));
        Assert.False(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue2.Id));
    }

    [Fact]
    public async Task PeutGererLigue_AdminAlwaysTrue()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "a3", "Admin");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        Assert.True(await svc.PeutGererLigueAsync(admin.Id, ligue.Id));
    }

    [Fact]
    public async Task PeutGererLigue_CoachSansPromotionFalse()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "c4", "Coach");
        var admin = await CreateUserWithRoleAsync(um, "a4", "Admin");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        Assert.False(await svc.PeutGererLigueAsync(coach.Id, ligue.Id));
    }

    [Fact]
    public async Task PeutEditerDonnees_AdminEtGCTrue_CoachFalse()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "ed-a", "Admin");
        var gc = await CreateUserWithRoleAsync(um, "ed-gc", "GrandCommissaire");
        var coach = await CreateUserWithRoleAsync(um, "ed-c", "Coach");

        Assert.True(await svc.PeutEditerDonneesAsync(admin.Id));
        Assert.True(await svc.PeutEditerDonneesAsync(gc.Id));
        Assert.False(await svc.PeutEditerDonneesAsync(coach.Id));
    }
}
