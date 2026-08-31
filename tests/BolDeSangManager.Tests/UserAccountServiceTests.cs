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

    /// <summary>Renseigné par <see cref="Creer"/> — utile aux tests de rôles.</summary>
    private RoleManager<IdentityRole>? _roleManager;

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
        _roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
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

    // ─── Suppression / anonymisation ──────────────────────────────────────

    [Fact]
    public async Task Supprimer_CompteVierge_RetireVraimentLaLigne()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "jetable@test.fr");
        var id = user.Id;

        var r = await svc.SupprimerCompteAsync(id, parQui: "self");

        Assert.True(r.Succeeded, string.Join(", ", r.Errors.Select(e => e.Description)));
        Assert.Null(await db.Users.FindAsync(id));
    }

    [Fact]
    public async Task Supprimer_CoachAvecEquipe_AnonymiseMaisConserveLEquipe()
    {
        // Le cœur du sujet : l'historique sportif ne doit pas disparaître avec
        // le compte. L'équipe reste, rattachée au MÊME CoachId.
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "partant@test.fr", "Ragnar");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId, "Les Partants");

        var r = await svc.SupprimerCompteAsync(user.Id, parQui: "self");
        Assert.True(r.Succeeded);

        var apres = await db.Users.FindAsync(user.Id);
        Assert.NotNull(apres);
        Assert.True(apres!.EstSupprime);
        Assert.Equal(UserAccountService.PseudoAnonyme, apres.PseudoCoach);
        Assert.DoesNotContain("partant@test.fr", apres.Email ?? "");
        Assert.Null(apres.PasswordHash);
        Assert.NotNull(apres.SupprimeLe);
        Assert.Equal("self", apres.SupprimePar);

        // La preuve : l'équipe existe toujours, avec le même coach.
        var equipeApres = await db.Teams.FindAsync(equipe.Id);
        Assert.NotNull(equipeApres);
        Assert.Equal(user.Id, equipeApres!.CoachId);
        Assert.Equal("Les Partants", equipeApres.Nom);
    }

    [Fact]
    public async Task Supprimer_EffaceLeJetonDAbonnementCalendrier()
    {
        // Le jeton ouvre l'accès au calendrier du coach SANS authentification :
        // laissé en place, il continuerait de répondre après l'anonymisation.
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "abonne@test.fr", "Abonne");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId, "Les Abonnes");

        user.JetonCalendrier = AbonnementCalendrierService.NouveauJeton();
        await db.SaveChangesAsync();

        var r = await svc.SupprimerCompteAsync(user.Id, parQui: "self");
        Assert.True(r.Succeeded);

        var apres = await db.Users.FindAsync(user.Id);
        Assert.True(apres!.EstSupprime);
        Assert.Null(apres.JetonCalendrier);
    }

    [Fact]
    public async Task Supprimer_CompteAnonymise_NePeutPlusSeConnecter()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "bloque@test.fr");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId);

        await svc.SupprimerCompteAsync(user.Id, parQui: "self");

        var apres = await db.Users.FindAsync(user.Id);
        Assert.False(await um.CheckPasswordAsync(apres!, "Password123!"));
        Assert.True(apres.LockoutEnabled);
        Assert.NotNull(apres.LockoutEnd);
        Assert.True(apres.LockoutEnd > DateTimeOffset.UtcNow.AddYears(50));
        Assert.Empty(await um.GetRolesAsync(apres));
    }

    [Fact]
    public async Task Supprimer_LibereLEmailPourUneNouvelleInscription()
    {
        // L'adresse doit redevenir utilisable : l'index unique d'Identity ne
        // doit pas rester bloqué par le compte anonymisé.
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "revient@test.fr");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId);

        await svc.SupprimerCompteAsync(user.Id, parQui: "self");

        var nouveau = await CreerUserAsync(um, "revient@test.fr", "Nouveau départ");
        Assert.NotEqual(user.Id, nouveau.Id);
    }

    [Fact]
    public async Task Supprimer_DeuxFois_NeJettePas()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "double@test.fr");
        var (gameId, versionId, teamTypeId) = await SeedRefsAsync(db);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, gameId, versionId, tiers.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamTypeId);

        await svc.SupprimerCompteAsync(user.Id, parQui: "self");
        var r2 = await svc.SupprimerCompteAsync(user.Id, parQui: "self");

        Assert.True(r2.Succeeded);
    }

    [Fact]
    public async Task Supprimer_LeDernierAdmin_EstRefuse()
    {
        // Garde-fou : se retirer le dernier compte Admin rendrait
        // l'administration inaccessible à tout le monde.
        var (svc, um, db) = Creer();
        var admin = await CreerUserAsync(um, "dernier-admin@test.fr");

        var roleManager = _roleManager!;
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await um.AddToRoleAsync(admin, "Admin");

        var r = await svc.SupprimerCompteAsync(admin.Id, parQui: "self");

        Assert.False(r.Succeeded);
        Assert.Contains(r.Errors, e => e.Description.Contains("Admin", StringComparison.OrdinalIgnoreCase));

        var apres = await db.Users.FindAsync(admin.Id);
        Assert.NotNull(apres);
        Assert.False(apres!.EstSupprime);
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
