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
}
