using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// #22 — texte de présentation d'une version de règles, sur le modèle du
/// règlement de ligue. Comme tout champ de version, il doit suivre la copie
/// d'une version ET l'export/import (sinon on retombe dans le #21).
/// </summary>
public class PresentationVersionTests
{
    private const string Texte = "# Coupe du monde 2027\n\n- 1 150 000 po\n- **Pas** de star players";

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

    private static DataEditService Svc(Data.ApplicationDbContext db) =>
        new(db, NullLogger<DataEditService>.Instance);

    [Fact]
    public async Task DefinirPresentation_EnregistreLeTexteSansToucherAuReste()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext())
            (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
            await Svc(db).DefinirPresentationVersionAsync(versionId, Texte);

        using (var db = factory.CreateContext())
        {
            var v = await db.RulesVersions.FindAsync(versionId);
            Assert.Equal(Texte, v!.Presentation);
            Assert.Equal("Saison 3", v.Nom);
            Assert.True(v.EstActive);
        }

        // Effacer le texte est permis (null → vide).
        using (var db = factory.CreateContext())
            await Svc(db).DefinirPresentationVersionAsync(versionId, null);
        using (var db = factory.CreateContext())
            Assert.Equal("", (await db.RulesVersions.FindAsync(versionId))!.Presentation);
    }

    [Fact]
    public async Task DefinirPresentation_VersionInconnue_EstRefusee()
    {
        using var factory = new TestDbFactory();
        using var db = factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Svc(db).DefinirPresentationVersionAsync(999, Texte));
    }

    [Fact]
    public async Task CopierUneVersion_ReprendSaPresentation()
    {
        using var factory = new TestDbFactory();
        int gameId, srcId;
        using (var db = factory.CreateContext())
        {
            (gameId, srcId) = SeedVersion(db);
            (await db.RulesVersions.FindAsync(srcId))!.Presentation = Texte;
            db.SaveChanges();
        }

        int copieId;
        using (var db = factory.CreateContext())
            copieId = (await Svc(db).CreerVersionAsync(gameId, "BB WC 2027", srcId)).Id;

        using (var db = factory.CreateContext())
            Assert.Equal(Texte, (await db.RulesVersions.FindAsync(copieId))!.Presentation);
    }

    [Fact]
    public async Task ExportImportDeVersion_TransporteLaPresentation()
    {
        using var factory = new TestDbFactory();
        int gameId, srcId;
        using (var db = factory.CreateContext())
        {
            (gameId, srcId) = SeedVersion(db);
            (await db.RulesVersions.FindAsync(srcId))!.Presentation = Texte;
            db.SaveChanges();
        }

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance)
                .ExportAsync(srcId);

        using (var db = factory.CreateContext())
        {
            var (ok, erreurs) = await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance)
                .ImportAsync(new MemoryStream(fichier), gameId, "Saison 3 bis");
            Assert.True(ok, string.Join(" / ", erreurs));
        }

        using (var db = factory.CreateContext())
            Assert.Equal(Texte, (await db.RulesVersions.SingleAsync(v => v.Nom == "Saison 3 bis")).Presentation);
    }
}
