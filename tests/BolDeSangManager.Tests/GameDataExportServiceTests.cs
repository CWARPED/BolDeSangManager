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

        var catId = await DataSeeder.GetOrCreateCategorieAsync(db, version.Id);

        var skill = new Skill
        {
            RulesVersionId = version.Id,
            Nom = "Blocage",
            Categorie = SkillCategory.Generale,
            SkillCategoryDefId = catId,
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
            Categorie = 2,
            ReglesSpeciales = "",
            LiguesTexteObsolete = ""
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

    // ─── Réserve : round-trip via export complet ──────────────────────────────

    [Fact]
    public async Task Export_Import_Complet_InclutLaReserve()
    {
        var (game, version, skill, _, _) = await SetupGameDataAsync();

        // Ajouter un poste de réserve avec une compétence de départ (skill "Blocage")
        await using (var db = _factory.CreateContext())
        {
            var pool = new PoolPosition
            {
                RulesVersionId = version.Id, Nom = "Ogre mercenaire",
                Cout = 140_000, Force = 5, Mouvement = 5, Agilite = "4+", Armure = "10+"
            };
            db.PoolPositions.Add(pool);
            await db.SaveChangesAsync();
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = pool.Id, SkillId = skill.Id });
            await db.SaveChangesAsync();
        }

        byte[] bytes;
        await using (var db = _factory.CreateContext())
            bytes = await CreateService(db).ExportAsync(version.Id);

        // Le JSON contient bien la réserve
        using (var doc = JsonDocument.Parse(bytes))
        {
            var reserve = doc.RootElement.GetProperty("reserve");
            Assert.Equal(1, reserve.GetArrayLength());
            Assert.Equal("Ogre mercenaire", reserve[0].GetProperty("nom").GetString());
            Assert.Equal("Blocage", reserve[0].GetProperty("competencesDepart")[0].GetString());
        }

        // Import complet sous un nouveau nom → la réserve est recréée avec le skill résolu par nom
        await using (var db = _factory.CreateContext())
        {
            using var stream = new MemoryStream(bytes);
            var (success, errors) = await CreateService(db).ImportAsync(stream, game.Id, "Saison 3 — Copie");
            Assert.True(success, string.Join("; ", errors));
        }

        await using (var db = _factory.CreateContext())
        {
            var nv = await db.RulesVersions.FirstAsync(v => v.Nom == "Saison 3 — Copie" && v.GameId == game.Id);
            var pools = await db.PoolPositions.Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
                .Where(p => p.RulesVersionId == nv.Id).ToListAsync();
            Assert.Single(pools);
            Assert.Equal("Ogre mercenaire", pools[0].Nom);
            Assert.Single(pools[0].CompetencesDepart);
            Assert.Equal("Blocage", pools[0].CompetencesDepart.First().Skill.Nom);
        }
    }

    // ─── Réserve seule : round-trip vers une autre version ────────────────────

    [Fact]
    public async Task ExportReserve_ImportReserve_VersAutreVersion()
    {
        var (game, version, skill, _, _) = await SetupGameDataAsync();

        await using (var db = _factory.CreateContext())
        {
            var pool = new PoolPosition { RulesVersionId = version.Id, Nom = "Troll", Force = 5, Mouvement = 4 };
            db.PoolPositions.Add(pool);
            await db.SaveChangesAsync();
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = pool.Id, SkillId = skill.Id });
            await db.SaveChangesAsync();
        }

        // Version cible avec un skill de même nom "Blocage" (résolution par nom)
        int cibleId;
        await using (var db = _factory.CreateContext())
        {
            var cible = new RulesVersion { GameId = game.Id, Nom = "Saison 4", Ordre = 2, EstActive = false };
            db.RulesVersions.Add(cible);
            await db.SaveChangesAsync();
            var catCible = await DataSeeder.GetOrCreateCategorieAsync(db, cible.Id);
            db.Skills.Add(new Skill { RulesVersionId = cible.Id, Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catCible });
            await db.SaveChangesAsync();
            cibleId = cible.Id;
        }

        byte[] bytes;
        await using (var db = _factory.CreateContext())
            bytes = await CreateService(db).ExportReserveAsync(version.Id);

        await using (var db = _factory.CreateContext())
        {
            using var stream = new MemoryStream(bytes);
            var (success, imported, errors) = await CreateService(db).ImportReserveAsync(stream, cibleId);
            Assert.True(success, string.Join("; ", errors));
            Assert.Equal(1, imported);
        }

        await using (var db = _factory.CreateContext())
        {
            var pools = await db.PoolPositions.Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
                .Where(p => p.RulesVersionId == cibleId).ToListAsync();
            Assert.Single(pools);
            Assert.Equal("Troll", pools[0].Nom);
            Assert.Single(pools[0].CompetencesDepart);
            Assert.Equal("Blocage", pools[0].CompetencesDepart.First().Skill.Nom);
        }
    }
}
