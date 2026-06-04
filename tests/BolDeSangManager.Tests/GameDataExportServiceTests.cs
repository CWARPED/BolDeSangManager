using System.Text.Json;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class GameDataExportServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static GameDataExportService CreateService(ApplicationDbContext db) =>
        new(db, NullLogger<GameDataExportService>.Instance);

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<(Game game, RulesVersion version, Skill skill, TeamType teamType, PlayerPosition position)>
        SetupGameDataAsync()
    {
        await using var db = _factory.CreateContext();

        var (game, version) = await DataSeeder.SeedGameAsync(db);

        var skill = new Skill
        {
            RulesVersionId = version.Id,
            Nom = "Blocage",
            Categorie = SkillCategory.Generale,
            Description = "Permet de relancer les dés d'attaque."
        };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();

        var teamType = new TeamType
        {
            GameId = game.Id,
            RulesVersionId = version.Id,
            Nom = "Humains",
            CoutRelance = 50_000,
            Categorie = TeamCategory.Staller,
            ReglesSpeciales = "",
            ReglesSpecialesLigue = ""
        };
        db.TeamTypes.Add(teamType);
        await db.SaveChangesAsync();

        var position = new PlayerPosition
        {
            TeamTypeId = teamType.Id,
            Nom = "Blitzer",
            QuantiteMax = 4,
            Cout = 90_000,
            Mouvement = 7,
            Force = 3,
            Agilite = "3+",
            CapacitePasse = "4+",
            Armure = "9+",
            CompetencesPrincipales = "GAF",
            CompetencesSecondaires = "P",
            MotsCles = "Humain,Blitzer"
        };
        db.PlayerPositions.Add(position);
        await db.SaveChangesAsync();

        db.PlayerPositionSkills.Add(new PlayerPositionSkill
        {
            PlayerPositionId = position.Id,
            SkillId = skill.Id
        });
        await db.SaveChangesAsync();

        db.TeamTypeKeywordLimits.Add(new TeamTypeKeywordLimit
        {
            TeamTypeId = teamType.Id,
            MotCle = "Blitzer",
            Max = 4
        });
        await db.SaveChangesAsync();

        return (game, version, skill, teamType, position);
    }

    // ─── Export ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ProduceJsonAvecSkillsEtTeamTypes()
    {
        var (_, version, skill, teamType, position) = await SetupGameDataAsync();

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(version.Id);
        Assert.NotEmpty(bytes);

        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;

        Assert.Equal("Blood Bowl", root.GetProperty("jeu").GetString());
        Assert.Equal("Saison 3", root.GetProperty("version").GetString());

        var skills = root.GetProperty("skills");
        Assert.Equal(1, skills.GetArrayLength());
        Assert.Equal("Blocage", skills[0].GetProperty("nom").GetString());

        var types = root.GetProperty("typesEquipes");
        Assert.Equal(1, types.GetArrayLength());
        Assert.Equal("Humains", types[0].GetProperty("nom").GetString());

        var postes = types[0].GetProperty("postes");
        Assert.Equal(1, postes.GetArrayLength());
        Assert.Equal("Blitzer", postes[0].GetProperty("nom").GetString());

        var competencesDepart = postes[0].GetProperty("competencesDepart");
        Assert.Equal(1, competencesDepart.GetArrayLength());
        Assert.Equal("Blocage", competencesDepart[0].GetString());

        var limites = types[0].GetProperty("limites");
        Assert.Equal(1, limites.GetArrayLength());
        Assert.Equal("Blitzer", limites[0].GetProperty("motCle").GetString());
        Assert.Equal(4, limites[0].GetProperty("max").GetInt32());
    }

    // ─── Import ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Import_CreeToutesLesEntites()
    {
        var (game, version, _, _, _) = await SetupGameDataAsync();

        // Export
        await using var db1 = _factory.CreateContext();
        var svc = CreateService(db1);
        var bytes = await svc.ExportAsync(version.Id);

        // Import sous un nouveau nom
        await using var db2 = _factory.CreateContext();
        var svc2 = CreateService(db2);
        using var stream = new MemoryStream(bytes);
        var (success, errors) = await svc2.ImportAsync(stream, game.Id, "Saison 3 — Importée");

        Assert.True(success, string.Join("; ", errors));

        await using var db3 = _factory.CreateContext();
        var nouvelleVersion = await db3.RulesVersions
            .FirstOrDefaultAsync(v => v.Nom == "Saison 3 — Importée" && v.GameId == game.Id);
        Assert.NotNull(nouvelleVersion);

        var skills = await db3.Skills.Where(s => s.RulesVersionId == nouvelleVersion.Id).ToListAsync();
        Assert.Single(skills);
        Assert.Equal("Blocage", skills[0].Nom);

        var types = await db3.TeamTypes.Include(tt => tt.Postes).Include(tt => tt.LimitesMotsCles)
            .Where(tt => tt.RulesVersionId == nouvelleVersion.Id).ToListAsync();
        Assert.Single(types);
        Assert.Equal("Humains", types[0].Nom);
        Assert.Single(types[0].Postes);
        Assert.Equal("Blitzer", types[0].Postes.First().Nom);
        Assert.Single(types[0].LimitesMotsCles);
        Assert.Equal("Blitzer", types[0].LimitesMotsCles.First().MotCle);

        var poste = types[0].Postes.First();
        var compDepart = await db3.PlayerPositionSkills
            .Where(pps => pps.PlayerPositionId == poste.Id).ToListAsync();
        Assert.Single(compDepart);
    }

    [Fact]
    public async Task Import_RefuseVersionDejaPresenteAvecMemNom()
    {
        var (game, version, _, _, _) = await SetupGameDataAsync();

        await using var db1 = _factory.CreateContext();
        var svc = CreateService(db1);
        var bytes = await svc.ExportAsync(version.Id);

        // Tenter d'importer avec le même nom que la version existante
        await using var db2 = _factory.CreateContext();
        var svc2 = CreateService(db2);
        using var stream = new MemoryStream(bytes);
        var (success, errors) = await svc2.ImportAsync(stream, game.Id, "Saison 3");

        Assert.False(success);
        Assert.NotEmpty(errors);
    }
}
