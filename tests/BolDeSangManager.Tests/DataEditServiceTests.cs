using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BolDeSangManager.Tests;

public class DataEditServiceTests
{
    private static (int gameId, int versionId) SeedVersion(Data.ApplicationDbContext db)
    {
        var game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
        db.Games.Add(game);
        db.SaveChanges();
        var v = new RulesVersion { GameId = game.Id, Nom = "Saison 3", EstActive = true, Ordre = 1 };
        db.RulesVersions.Add(v);
        db.SaveChanges();
        return (game.Id, v.Id);
    }

    [Fact]
    public async Task AjouterReserve_PersisteLePoste()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext())
            (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var data = new PoolPosition { Nom = "Ogre mercenaire", Cout = 140_000, Force = 5, Mouvement = 5 };
            await svc.AjouterReserveAsync(versionId, data, Array.Empty<int>());
        }

        using (var db = factory.CreateContext())
        {
            var liste = await db.PoolPositions.Where(p => p.RulesVersionId == versionId).ToListAsync();
            Assert.Single(liste);
            Assert.Equal("Ogre mercenaire", liste[0].Nom);
            Assert.Equal(5, liste[0].Force);
        }
    }

    [Fact]
    public async Task ImporterReserve_CopiePosteEtSkills_EtResteIndependant()
    {
        using var factory = new TestDbFactory();
        int versionId, teamTypeId, poolId, skillId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var skill = new Skill { Nom = "Châtaigne", Categorie = SkillCategory.Generale, RulesVersionId = versionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;

            var tt = new TeamType { GameId = gameId, RulesVersionId = versionId, Nom = "Humains" };
            db.TeamTypes.Add(tt); db.SaveChanges(); teamTypeId = tt.Id;

            var pool = new PoolPosition { RulesVersionId = versionId, Nom = "Ogre", Cout = 140_000, Force = 5 };
            db.PoolPositions.Add(pool); db.SaveChanges(); poolId = pool.Id;
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = poolId, SkillId = skillId });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ImporterReserveVersTeamTypeAsync(teamTypeId, new[] { poolId });
        }

        using (var db = factory.CreateContext())
        {
            var poste = await db.PlayerPositions
                .Include(p => p.CompetencesDepart)
                .FirstOrDefaultAsync(p => p.TeamTypeId == teamTypeId && p.Nom == "Ogre");
            Assert.NotNull(poste);
            Assert.Equal(5, poste!.Force);
            Assert.Single(poste.CompetencesDepart);
            Assert.Equal(skillId, poste.CompetencesDepart.First().SkillId);
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.SupprimerReserveAsync(poolId);
        }
        using (var db = factory.CreateContext())
        {
            var poste = await db.PlayerPositions
                .Include(p => p.CompetencesDepart)
                .FirstOrDefaultAsync(p => p.TeamTypeId == teamTypeId && p.Nom == "Ogre");
            Assert.NotNull(poste);
            Assert.Single(poste!.CompetencesDepart);
        }
    }

    [Fact]
    public async Task ClonerVersion_CopieAussiLaReserve()
    {
        using var factory = new TestDbFactory();
        int gameId, srcVersionId, skillId, poolId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; srcVersionId = vId;
            var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, RulesVersionId = srcVersionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;
            var pool = new PoolPosition { RulesVersionId = srcVersionId, Nom = "Troll", Force = 5 };
            db.PoolPositions.Add(pool); db.SaveChanges(); poolId = pool.Id;
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = poolId, SkillId = skillId });
            db.SaveChanges();
        }

        int newVersionId;
        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var nouvelle = await svc.CreerVersionAsync(gameId, "Saison 4", 2, false, srcVersionId);
            newVersionId = nouvelle.Id;
        }

        using (var db = factory.CreateContext())
        {
            var pools = await db.PoolPositions
                .Include(p => p.CompetencesDepart)
                .Where(p => p.RulesVersionId == newVersionId).ToListAsync();
            Assert.Single(pools);
            Assert.Equal("Troll", pools[0].Nom);
            var skillCloneId = pools[0].CompetencesDepart.Single().SkillId;
            var skillClone = await db.Skills.FindAsync(skillCloneId);
            Assert.Equal(newVersionId, skillClone!.RulesVersionId);
        }
    }
}
