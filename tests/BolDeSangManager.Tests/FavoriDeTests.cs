using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Règle « Favori de… » (LRB p.93) : l'équipe voue un culte à un Dieu du Chaos.
///
/// Modèle retenu (option A) : le CADRE est défini sur la fiche de race — quelles
/// divinités sont permises — et le CHOIX de chaque équipe est fait par le
/// COMMISSAIRE dans cette liste. Le coach ne saisit rien : le LRB rend le choix
/// définitif (« vous ne pouvez plus en changer »).
/// </summary>
public class FavoriDeTests
{
    private static TeamService Svc(Data.ApplicationDbContext db) =>
        new(db, NullLogger<TeamService>.Instance);

    private static DataEditService Edit(Data.ApplicationDbContext db) =>
        new(db, NullLogger<DataEditService>.Instance);

    /// <summary>Crée une race, éventuellement porteuse de « Favori de… ».</summary>
    private static async Task<(int versionId, int teamTypeId)> SeedRaceAsync(
        TestDbFactory factory, string nomRace, string? options)
    {
        int versionId, teamTypeId;

        using (var db = factory.CreateContext())
        {
            var game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
            db.Games.Add(game);
            await db.SaveChangesAsync();
            var v = new RulesVersion { GameId = game.Id, Nom = "Saison 3", EstActive = true, Ordre = 1 };
            db.RulesVersions.Add(v);
            await db.SaveChangesAsync();
            versionId = v.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = Edit(db);
            var tt = await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = nomRace, CoutRelance = 60_000 });
            teamTypeId = tt.Id;

            if (options is not null)
            {
                var regle = await svc.CreerRegleSpecialeAsync(
                    versionId, "Favori de…", "Culte d'un Dieu du Chaos.", SpecialRuleCodes.FavoriDe);
                await svc.AssocierRegleSpecialeAsync(teamTypeId, regle.Id, options);
            }
        }

        return (versionId, teamTypeId);
    }

    // ── Options offertes par la race ─────────────────────────────────────────

    [Fact]
    public async Task GetOptions_RaceSansLaRegle_RetourneListeVide()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Humains", options: null);

        using var db = factory.CreateContext();
        Assert.Empty(await Svc(db).GetOptionsDiviniteAsync(ttId));
    }

    [Fact]
    public async Task GetOptions_DiviniteImposee_RetourneUneSeuleValeur()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Nurgle", options: "Nurgle");

        using var db = factory.CreateContext();
        var options = await Svc(db).GetOptionsDiviniteAsync(ttId);
        Assert.Equal(["Nurgle"], options);
    }

    [Fact]
    public async Task GetOptions_ChoixLibre_RetourneToutesLesDivinites()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(
            factory, "Renégats du Chaos", options: "Hashut,Khorne,Nurgle,Slaanesh,Tzeentch,Chaos Universel");

        using var db = factory.CreateContext();
        var options = await Svc(db).GetOptionsDiviniteAsync(ttId);
        Assert.Equal(6, options.Count);
        Assert.Contains("Chaos Universel", options);
    }

    // ── Choix du commissaire ─────────────────────────────────────────────────

    private static async Task<int> CreerEquipeAsync(
        TestDbFactory factory, int teamTypeId, string nomEquipe = "Les Testeurs")
    {
        using var db = factory.CreateContext();

        // On passe par les helpers du projet : un ApplicationUser incomplet ou
        // une League sans GameId font échouer une FK au SaveChanges, avec un
        // « FOREIGN KEY constraint failed » qui ressemble à un bug du code
        // testé alors que c'est la fixture qui est fausse.
        var commissaire = DataSeeder.CreateUser("comm");
        var coach = DataSeeder.CreateUser("coach");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();

        var version = await db.RulesVersions.FirstAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, version.GameId, version.Id, commissaire.Id);

        var equipe = new Team
        {
            Nom = nomEquipe, CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamTypeId
        };
        return (await Svc(db).CreerEquipeAsync(equipe, [])).Id;
    }

    /// <summary>
    /// Divinité unique = imposée par le LRB : elle est assignée à la création,
    /// sans rien demander à personne.
    /// </summary>
    [Fact]
    public async Task CreerEquipe_DiviniteImposee_EstAssigneeAutomatiquement()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Nurgle", options: "Nurgle");
        var equipeId = await CreerEquipeAsync(factory, ttId, "Les Pustuleux");

        using var db = factory.CreateContext();
        Assert.Equal("Nurgle", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    /// <summary>
    /// Plusieurs options = c'est au commissaire de trancher. L'équipe naît sans
    /// divinité plutôt qu'avec un choix arbitraire pris à sa place.
    /// </summary>
    [Fact]
    public async Task CreerEquipe_ChoixLibre_NaitSansDivinite()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Renégats du Chaos", options: "Khorne,Nurgle,Tzeentch");
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using var db = factory.CreateContext();
        Assert.Equal("", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    [Fact]
    public async Task CreerEquipe_RaceSansLaRegle_NaitSansDivinite()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Humains", options: null);
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using var db = factory.CreateContext();
        Assert.Equal("", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    [Fact]
    public async Task DefinirDivinite_ChoixValide_EstEnregistre()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Renégats du Chaos", options: "Khorne,Nurgle,Tzeentch");
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using (var db = factory.CreateContext())
            await Svc(db).DefinirDiviniteAsync(equipeId, "Nurgle");

        using (var db = factory.CreateContext())
            Assert.Equal("Nurgle", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    /// <summary>
    /// Garde-fou serveur : une valeur postée depuis un écran est falsifiable.
    /// Un dieu hors de la liste de la race doit être refusé.
    /// </summary>
    [Fact]
    public async Task DefinirDivinite_HorsDesOptionsDeLaRace_EstRefuse()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Renégats du Chaos", options: "Khorne,Nurgle");
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using var db = factory.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Svc(db).DefinirDiviniteAsync(equipeId, "Slaanesh"));

        Assert.Contains("Khorne", ex.Message);   // le message rappelle les valeurs permises
    }

    [Fact]
    public async Task DefinirDivinite_SurUneRaceSansLaRegle_EstRefuse()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Humains", options: null);
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using var db = factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Svc(db).DefinirDiviniteAsync(equipeId, "Khorne"));
    }

    /// <summary>La casse de la saisie ne doit pas créer deux orthographes en base.</summary>
    [Fact]
    public async Task DefinirDivinite_EnregistreLaFormeCanonique()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Renégats du Chaos", options: "Khorne,Nurgle");
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using (var db = factory.CreateContext())
            await Svc(db).DefinirDiviniteAsync(equipeId, "  khorne  ");

        using (var db = factory.CreateContext())
            Assert.Equal("Khorne", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    /// <summary>Le commissaire peut corriger une erreur en effaçant le choix.</summary>
    [Fact]
    public async Task DefinirDivinite_ChaineVide_EffaceLeChoix()
    {
        using var factory = new TestDbFactory();
        var (_, ttId) = await SeedRaceAsync(factory, "Renégats du Chaos", options: "Khorne,Nurgle");
        var equipeId = await CreerEquipeAsync(factory, ttId);

        using (var db = factory.CreateContext())
            await Svc(db).DefinirDiviniteAsync(equipeId, "Khorne");
        using (var db = factory.CreateContext())
            await Svc(db).DefinirDiviniteAsync(equipeId, "");

        using (var db = factory.CreateContext())
            Assert.Equal("", (await db.Teams.FindAsync(equipeId))!.DiviniteChoisie);
    }

    // ── Feuille imprimée ─────────────────────────────────────────────────────

    /// <summary>
    /// Sur la feuille, on imprime la divinité RETENUE (« Favori de Khorne »),
    /// pas le nom générique de la règle : c'est l'information utile à la table.
    /// </summary>
    [Fact]
    public void FeuilleEquipePdf_ImprimeLaDiviniteRetenue()
    {
        var tt = new TeamType { Nom = "Renégats du Chaos", CoutRelance = 70_000 };
        tt.ReglesSpecialesListe.Add(new TeamTypeSpecialRule
        {
            OptionsChoix = "Khorne,Nurgle",
            SpecialRule = new SpecialRule
            {
                Nom = "Favori de…", Code = SpecialRuleCodes.FavoriDe,
                Description = "L'équipe rend hommage à un Dieu du Chaos."
            }
        });

        var equipe = new Team { Nom = "Les Renégats", TeamType = tt, DiviniteChoisie = "Khorne" };
        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, false));

        Assert.Contains("Favori de Khorne", texte);
    }

    /// <summary>
    /// Sans divinité choisie, la feuille garde le nom générique plutôt que
    /// d'imprimer un « Favori de » incomplet.
    /// </summary>
    [Fact]
    public void FeuilleEquipePdf_SansDivinite_GardeLeNomGenerique()
    {
        var tt = new TeamType { Nom = "Renégats du Chaos", CoutRelance = 70_000 };
        tt.ReglesSpecialesListe.Add(new TeamTypeSpecialRule
        {
            OptionsChoix = "Khorne,Nurgle",
            SpecialRule = new SpecialRule
            {
                Nom = "Favori de…", Code = SpecialRuleCodes.FavoriDe,
                Description = "L'équipe rend hommage à un Dieu du Chaos."
            }
        });

        var equipe = new Team { Nom = "Les Renégats", TeamType = tt, DiviniteChoisie = "" };
        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, false));

        Assert.Contains("Favori de…", texte);
        Assert.DoesNotContain("Favori de Khorne", texte);
    }

    private static string LireTextePdf(byte[] pdf)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }
}
