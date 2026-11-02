using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>Page « Règles » (#20) : lecture seule du contenu d'une version.</summary>
public class ReglesConsultationTests
{
    /// <summary>Deux versions du même jeu, chacune avec sa race, sa compétence, ses stars.</summary>
    private static async Task<(int s3, int wc, int orquesS3)> SeedAsync(TestDbFactory factory)
    {
        using var db = factory.CreateContext();
        var game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var s3 = new RulesVersion { GameId = game.Id, Nom = "Saison 3", EstActive = true, Ordre = 1, Presentation = "# S3" };
        var wc = new RulesVersion { GameId = game.Id, Nom = "BB WC 2027", EstActive = false, Ordre = 2 };
        db.RulesVersions.AddRange(s3, wc);
        await db.SaveChangesAsync();

        foreach (var v in new[] { s3, wc })
        {
            var cat = new SkillCategoryDef { RulesVersionId = v.Id, Nom = "Générale", Code = "G" };
            db.SkillCategories.Add(cat);
            db.Skills.Add(new Skill { Nom = $"Blocage {v.Nom}", Categorie = SkillCategory.Generale, SkillCategoryDef = cat, RulesVersionId = v.Id, Description = "Texte" });
            var skill = db.Skills.Local.Last();
            var race = new TeamType { GameId = game.Id, RulesVersionId = v.Id, Nom = $"Orques {v.Nom}" };
            var poste = new PlayerPosition { TeamType = race, Nom = "Trois-quart", QuantiteMax = 16, Cout = 50000 };
            poste.CompetencesDepart.Add(new PlayerPositionSkill { PlayerPosition = poste, Skill = skill });
            race.Postes.Add(poste);
            db.TeamTypes.Add(race);
            db.StarPlayers.Add(new StarPlayer { RulesVersionId = v.Id, Nom = $"Morg {v.Nom}" });
        }
        await db.SaveChangesAsync();

        // Saison 3 : Varag réservé à « Bagarre », où sont les Orques ; Glart
        // réservé à « Sylvestre », où ils ne sont pas.
        var orques = db.TeamTypes.Single(t => t.RulesVersionId == s3.Id);
        var bagarre = new ThemedLeague { RulesVersionId = s3.Id, Nom = "Bagarre des Terres Arides" };
        var sylvestre = new ThemedLeague { RulesVersionId = s3.Id, Nom = "Ligue Sylvestre" };
        var varag = new StarPlayer { RulesVersionId = s3.Id, Nom = "Varag" };
        var glart = new StarPlayer { RulesVersionId = s3.Id, Nom = "Glart" };
        db.AddRange(bagarre, sylvestre, varag, glart);
        await db.SaveChangesAsync();
        db.AddRange(
            new TeamTypeThemedLeague { TeamTypeId = orques.Id, ThemedLeagueId = bagarre.Id },
            new StarPlayerThemedLeague { StarPlayerId = varag.Id, ThemedLeagueId = bagarre.Id },
            new StarPlayerThemedLeague { StarPlayerId = glart.Id, ThemedLeagueId = sylvestre.Id });
        await db.SaveChangesAsync();
        return (s3.Id, wc.Id, orques.Id);
    }

    [Fact]
    public async Task LeContenuNeMelangePasLesVersions()
    {
        using var factory = new TestDbFactory();
        var (s3, _, _) = await SeedAsync(factory);

        using var db = factory.CreateContext();
        var c = await new ReglesConsultationService(db).GetContenuAsync(s3);

        Assert.NotNull(c);
        Assert.Equal("# S3", c!.Version.Presentation);
        Assert.Equal(["Orques Saison 3"], c.Races.Select(r => r.Nom));
        Assert.Equal(["Blocage Saison 3"], c.Competences.Select(s => s.Nom));
        Assert.Equal(["Glart", "Morg Saison 3", "Varag"], c.StarPlayers.Select(s => s.Nom));
        // Ce que la page affiche est chargé : ligues de la race, ligues des stars,
        // catégorie d'une compétence ouverte depuis un poste.
        Assert.Equal("Générale", c.Races[0].Postes.Single().CompetencesDepart.Single().Skill.SkillCategoryDef.Nom);
        Assert.Equal("Bagarre des Terres Arides", c.Races[0].LiguesListe.Single().ThemedLeague.Nom);
        Assert.Equal("Bagarre des Terres Arides",
            c.StarPlayers.Single(s => s.Nom == "Varag").Ligues.Single().ThemedLeague.Nom);
    }

    [Fact]
    public async Task VersionInconnue_RenvoieNull()
    {
        using var factory = new TestDbFactory();
        using var db = factory.CreateContext();
        Assert.Null(await new ReglesConsultationService(db).GetContenuAsync(999));
    }

    [Fact]
    public async Task ParDefaut_LaVersionActive()
    {
        using var factory = new TestDbFactory();
        var (s3, _, _) = await SeedAsync(factory);

        using var db = factory.CreateContext();
        var versions = await new ReglesConsultationService(db).GetVersionsAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(s3, ReglesConsultationService.VersionParDefaut(versions)!.Id);
    }

    [Fact]
    public async Task LeFiltreParRace_EstLaRegleDuRecrutement()
    {
        using var factory = new TestDbFactory();
        var (s3, _, orques) = await SeedAsync(factory);

        using var db = factory.CreateContext();
        var c = await new ReglesConsultationService(db).GetContenuAsync(s3);
        var race = c!.Races.Single(r => r.Id == orques);
        var page = ReglesConsultationService.StarsAccessibles(c.StarPlayers, race).Select(s => s.Nom).ToList();

        var recrutement = (await new DataEditService(db, NullLogger<DataEditService>.Instance)
            .GetStarPlayersAccessiblesAsync(orques)).Select(s => s.Nom).ToList();

        Assert.Equal(["Morg Saison 3", "Varag"], page);   // pas Glart (Sylvestre)
        Assert.Equal(recrutement.OrderBy(n => n), page.OrderBy(n => n));
    }
}
