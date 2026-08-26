using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BolDeSangManager.Tests;

/// <summary>
/// Export RGPD (droit d'accès, article 15).
///
/// L'export fourni par le squelette ASP.NET Identity ne sortait que les
/// propriétés portant l'attribut [PersonalData] : sur ce projet, l'e-mail et
/// deux drapeaux techniques. Ni le pseudo de coach, ni les équipes, ni les
/// matchs n'y figuraient — autrement dit, tout ce que l'association détient
/// réellement sur la personne était absent du fichier censé le lui restituer.
///
/// Ces tests verrouillent le contenu attendu, et la limite inverse : l'export
/// d'un coach ne doit pas déverser les données des autres.
/// </summary>
public class PersonalDataExportServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private (PersonalDataExportService svc, UserManager<ApplicationUser> um, ApplicationDbContext db) Creer()
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
        return (new PersonalDataExportService(db, um), um, db);
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

    // ─── Contenu du dossier ───────────────────────────────────────────────

    [Fact]
    public async Task Export_CompteInexistant_RetourneNull()
    {
        var (svc, _, _) = Creer();

        Assert.Null(await svc.ConstruireDossierAsync("id-qui-nexiste-pas"));
        Assert.Null(await svc.ExporterJsonAsync("id-qui-nexiste-pas"));
    }

    [Fact]
    public async Task Export_ContientLePseudo_CeQueLAncienExportOmettait()
    {
        var (svc, um, _) = Creer();
        var user = await CreerUserAsync(um, "pseudo@test.fr", "Le Boucher");

        var dossier = await svc.ConstruireDossierAsync(user.Id);

        Assert.NotNull(dossier);
        Assert.Equal("Le Boucher", dossier!.Compte.Pseudo);
        Assert.Equal("pseudo@test.fr", dossier.Compte.Email);
    }

    [Fact]
    public async Task Export_ContientLesEquipesEtLeursJoueurs()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "coach@test.fr");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, tiers.Id);

        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamType.Id, "Les Charognards");
        await DataSeeder.SeedPlayerAsync(db, equipe.Id, position.Id, "Grognak", 7);

        var dossier = await svc.ConstruireDossierAsync(user.Id);

        var e = Assert.Single(dossier!.Equipes);
        Assert.Equal("Les Charognards", e.Nom);
        var j = Assert.Single(e.Joueurs);
        Assert.Equal("Grognak", j.Nom);
        Assert.Equal(7, j.Numero);
    }

    [Fact]
    public async Task Export_ContientLesMatchsDuPointDeVueDuDemandeur()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "joueur@test.fr");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var tiers = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, tiers.Id);

        // Le demandeur joue à l'EXTÉRIEUR : le point de vue doit être inversé.
        var chezEux = await DataSeeder.SeedTeamAsync(db, ligue.Id, tiers.Id, teamType.Id, "Les Marteaux");
        var chezMoi = await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamType.Id, "Les Charognards");
        var match = await DataSeeder.SeedMatchAsync(db, chezEux.Id, chezMoi.Id);
        match.ScoreDomicile = 1;
        match.ScoreExterieur = 3;
        await db.SaveChangesAsync();

        var dossier = await svc.ConstruireDossierAsync(user.Id);

        var m = Assert.Single(dossier!.Matchs);
        Assert.Equal("Les Charognards", m.MonEquipe);
        Assert.Equal("Les Marteaux", m.Adversaire);
        Assert.False(m.ADomicile);
        Assert.Equal(3, m.MonScore);
        Assert.Equal(1, m.ScoreAdverse);
    }

    [Fact]
    public async Task Export_ContientLesLiguesGereesEtLesFeuillesSaisies()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "commissaire@test.fr");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, user.Id);

        var tiers = await CreerTiersAsync(db);
        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, tiers.Id, teamType.Id, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, tiers.Id, teamType.Id, "Ext");
        var match = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id);

        db.MatchSheets.Add(new MatchSheet
        {
            MatchId = match.Id,
            SaisiParId = user.Id,
            TouchdownsDomicile = 2,
            TouchdownsExterieur = 1
        });
        await db.SaveChangesAsync();

        var dossier = await svc.ConstruireDossierAsync(user.Id);

        Assert.Single(dossier!.LiguesGerees);
        var f = Assert.Single(dossier.FeuillesDeMatchSaisies);
        Assert.Equal(2, f.TouchdownsDomicile);
        Assert.Contains("Dom", f.Rencontre);
    }

    /// <summary>
    /// Le pendant du droit d'accès : on restitue le dossier du demandeur, pas
    /// celui du voisin. Un coach ne doit pas récupérer les équipes des autres.
    /// </summary>
    [Fact]
    public async Task Export_NeContientPasLesEquipesDesAutresCoaches()
    {
        var (svc, um, db) = Creer();
        var user = await CreerUserAsync(um, "moi@test.fr");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var autre = await CreerTiersAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, autre.Id);

        await DataSeeder.SeedTeamAsync(db, ligue.Id, user.Id, teamType.Id, "La mienne");
        await DataSeeder.SeedTeamAsync(db, ligue.Id, autre.Id, teamType.Id, "Celle du voisin");

        var dossier = await svc.ConstruireDossierAsync(user.Id);

        var e = Assert.Single(dossier!.Equipes);
        Assert.Equal("La mienne", e.Nom);
        Assert.Empty(dossier.LiguesGerees);
    }

    // ─── Sérialisation ────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ProduitDuJsonLisibleAvecAccentsNonEchappes()
    {
        var (svc, um, _) = Creer();
        var user = await CreerUserAsync(um, "accents@test.fr", "Écrase-Tête");

        var octets = await svc.ExporterJsonAsync(user.Id);

        Assert.NotNull(octets);
        var json = System.Text.Encoding.UTF8.GetString(octets!);
        // Accents rendus tels quels et non en \u00c9 : le destinataire est un
        // coach qui ouvre le fichier dans un éditeur de texte.
        Assert.Contains("Écrase-Tête", json);
        Assert.Contains("\n", json); // indenté
    }

    [Fact]
    public void NomFichier_AssainitLePseudoEtDateLeFichier()
    {
        var nom = PersonalDataExportService.NomFichier("Écrase/Tête:2024");

        Assert.StartsWith("mes-donnees-", nom);
        Assert.EndsWith(".json", nom);
        Assert.DoesNotContain("/", nom);
        Assert.DoesNotContain(":", nom);
        Assert.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"), nom);
    }

    [Fact]
    public void NomFichier_PseudoVide_RetombeSurUnNomParDefaut()
    {
        var nom = PersonalDataExportService.NomFichier("");

        Assert.StartsWith("mes-donnees-coach-", nom);
    }
}
