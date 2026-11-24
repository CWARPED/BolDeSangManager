using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Le seed des coups de pouce et des stars ne remplit qu'un catalogue VIDE.
/// Avant, il comparait par nom à chaque démarrage : tout coup de pouce renommé
/// en admin revenait en double (14 doublons sur une copie de la base de prod).
/// </summary>
public class SeedCatalogueCoupsDePouceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<int> VersionAvecRegleSpecialeAsync()
    {
        await using var db = _factory.CreateContext();
        var (_, version) = await DataSeeder.SeedGameAsync(db);
        db.SpecialRules.Add(new SpecialRule { RulesVersionId = version.Id, Nom = "Favori de…" });
        await db.SaveChangesAsync();
        return version.Id;
    }

    [Fact]
    public async Task VersionVide_RecoitLeCatalogueInitial_EtStarPlayersEstCoche()
    {
        var versionId = await VersionAvecRegleSpecialeAsync();

        await using (var db = _factory.CreateContext())
            await DbSeeder.SeedCoupsDePouceEtStarPlayersAsync(db, NullLogger.Instance);

        await using var verif = _factory.CreateContext();
        var cps = await verif.Inducements.Where(i => i.RulesVersionId == versionId).ToListAsync();
        Assert.Equal(InducementSeedData.CoupsDePouce.Length, cps.Count);
        Assert.Equal(["Star Players"], cps.Where(c => c.EstStarPlayer).Select(c => c.Nom));
        Assert.True(await verif.StarPlayers.CountAsync(s => s.RulesVersionId == versionId) > 0);
    }

    [Fact]
    public async Task CoupDePouceRenomme_NEstPasRecreeAuDemarrageSuivant()
    {
        var versionId = await VersionAvecRegleSpecialeAsync();
        await using (var db = _factory.CreateContext())
            await DbSeeder.SeedCoupsDePouceEtStarPlayersAsync(db, NullLogger.Instance);

        // L'association renomme un coup de pouce et une star en admin.
        await using (var db = _factory.CreateContext())
        {
            var cp = await db.Inducements.FirstAsync(i => i.RulesVersionId == versionId && i.Nom == "Prières à Nuffle");
            cp.Nom = "Prières à Nuffle 10KPO";
            var star = await db.StarPlayers.OrderBy(s => s.Id).FirstAsync(s => s.RulesVersionId == versionId);
            star.Nom += " (renommée)";
            await db.SaveChangesAsync();
        }
        int nbCp, nbStars;
        await using (var db = _factory.CreateContext())
        {
            nbCp = await db.Inducements.CountAsync(i => i.RulesVersionId == versionId);
            nbStars = await db.StarPlayers.CountAsync(s => s.RulesVersionId == versionId);
        }

        // Démarrage suivant.
        await using (var db = _factory.CreateContext())
            await DbSeeder.SeedCoupsDePouceEtStarPlayersAsync(db, NullLogger.Instance);

        await using var verif = _factory.CreateContext();
        Assert.Equal(nbCp, await verif.Inducements.CountAsync(i => i.RulesVersionId == versionId));
        Assert.Equal(nbStars, await verif.StarPlayers.CountAsync(s => s.RulesVersionId == versionId));
        Assert.False(await verif.Inducements.AnyAsync(i => i.RulesVersionId == versionId && i.Nom == "Prières à Nuffle"));
    }
}
