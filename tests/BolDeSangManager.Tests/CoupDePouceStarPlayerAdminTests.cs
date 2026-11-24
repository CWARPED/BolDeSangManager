using BolDeSangManager.Data;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// La case « Star player » d'un coup de pouce est une donnée de version comme
/// les autres : elle se règle en admin, survit à la modification des autres
/// champs, au clonage d'une version et à l'export/import portable.
/// </summary>
public class CoupDePouceStarPlayerAdminTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static DataEditService Data(ApplicationDbContext db) => new(db, NullLogger<DataEditService>.Instance);

    private async Task<(int gameId, int versionId, int star, int ordinaire)> SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var star = await Data(db).CreerCoupDePouceAsync(rv.Id, "Star Player 1", "", 0, 1, "Prix variable", estStarPlayer: true);
        var ordinaire = await Data(db).CreerCoupDePouceAsync(rv.Id, "Pots-de-vin", "", 100_000, 3);
        return (game.Id, rv.Id, star.Id, ordinaire.Id);
    }

    [Fact]
    public async Task Creation_PoseLaCase_SeulementSiDemandee()
    {
        var (_, _, star, ordinaire) = await SetupAsync();
        await using var db = _factory.CreateContext();
        Assert.True((await db.Inducements.FindAsync(star))!.EstStarPlayer);
        Assert.False((await db.Inducements.FindAsync(ordinaire))!.EstStarPlayer);
    }

    /// <summary>Modifier un AUTRE champ sans passer la case ne la décoche pas.</summary>
    [Fact]
    public async Task Modifier_SansLaCase_LaGarde_AvecLaCase_LaChange()
    {
        var (_, _, star, _) = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Data(db).ModifierCoupDePouceAsync(star, "Star Player 1", "Effet", 0, 1, "Prix variable", 17);
        await using (var db = _factory.CreateContext())
            Assert.True((await db.Inducements.FindAsync(star))!.EstStarPlayer);

        await using (var db = _factory.CreateContext())
            await Data(db).ModifierCoupDePouceAsync(star, "Star Player 1", "Effet", 0, 1, "Prix variable", 17, false);
        await using (var db = _factory.CreateContext())
            Assert.False((await db.Inducements.FindAsync(star))!.EstStarPlayer);
    }

    [Fact]
    public async Task ClonerVersion_ConserveLaCase()
    {
        var (gameId, versionId, _, _) = await SetupAsync();
        int cloneId;
        await using (var db = _factory.CreateContext())
            cloneId = (await Data(db).CreerVersionAsync(gameId, "Clone", versionId)).Id;

        await using var verif = _factory.CreateContext();
        var cps = await verif.Inducements.Where(i => i.RulesVersionId == cloneId).OrderBy(i => i.Nom).ToListAsync();
        Assert.Equal([("Pots-de-vin", false), ("Star Player 1", true)], cps.Select(c => (c.Nom, c.EstStarPlayer)));
    }

    /// <summary>Le catalogue (coups de pouce, stars, ligues) a son propre export portable.</summary>
    [Fact]
    public async Task ExportImportCatalogue_ConserveLaCase()
    {
        var (gameId, versionId, _, _) = await SetupAsync();
        int cibleId;
        byte[] json;
        await using (var db = _factory.CreateContext())
        {
            json = await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance).ExportCatalogueAsync(versionId);
            cibleId = (await Data(db).CreerVersionAsync(gameId, "Cible", null)).Id;
        }
        await using (var db = _factory.CreateContext())
        {
            var (ok, erreurs) = await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance)
                .ImportCatalogueAsync(cibleId, new MemoryStream(json));
            Assert.True(ok, string.Join(" / ", erreurs));
        }

        await using var verif = _factory.CreateContext();
        var cps = await verif.Inducements.Where(i => i.RulesVersionId == cibleId).OrderBy(i => i.Nom).ToListAsync();
        Assert.Equal([("Pots-de-vin", false), ("Star Player 1", true)], cps.Select(c => (c.Nom, c.EstStarPlayer)));
    }
}
