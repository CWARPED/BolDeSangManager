using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// CRUD des catégories de compétence (R2a) : les catégories sont des données
/// portées par une RulesVersion, plus un enum figé.
/// </summary>
public class SkillCategoryTests
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

    private static int SeedCategorie(Data.ApplicationDbContext db, int versionId, string nom, string code)
    {
        var c = new SkillCategoryDef { RulesVersionId = versionId, Nom = nom, Code = code };
        db.SkillCategories.Add(c);
        db.SaveChanges();
        return c.Id;
    }

    [Fact]
    public async Task CreerCategorie_Persiste()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.CreerCategorieAsync(versionId, "Dungeon", "DB");
        }

        using (var db = factory.CreateContext())
        {
            var c = await db.SkillCategories.SingleAsync(x => x.RulesVersionId == versionId);
            Assert.Equal("Dungeon", c.Nom);
            Assert.Equal("DB", c.Code);   // code à 2 lettres accepté
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TROP")]
    public async Task CreerCategorie_RefuseCodeInvalide(string code)
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using var ctx = factory.CreateContext();
        var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreerCategorieAsync(versionId, "Bidon", code));
    }

    [Fact]
    public async Task CreerCategorie_RefuseNomOuCodeDejaPris()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext())
        {
            (_, versionId) = SeedVersion(db);
            SeedCategorie(db, versionId, "Agilité", "A");
        }

        using (var ctx = factory.CreateContext())
        {
            var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
            // même nom (casse différente)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.CreerCategorieAsync(versionId, "agilité", "X"));
            // même code (casse différente)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.CreerCategorieAsync(versionId, "Autre", "a"));
        }
    }

    [Fact]
    public async Task ModifierCategorie_RenommeEtChangeLeCode_MemeSiUtilisee()
    {
        using var factory = new TestDbFactory();
        int versionId, catId;
        using (var db = factory.CreateContext())
        {
            (_, versionId) = SeedVersion(db);
            catId = SeedCategorie(db, versionId, "Agilité", "A");
            db.Skills.Add(new Skill { RulesVersionId = versionId, SkillCategoryDefId = catId, Nom = "Esquive" });
            db.SaveChanges();
        }

        using (var ctx = factory.CreateContext())
        {
            var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
            await svc.ModifierCategorieAsync(catId, "Agilité & Vitesse", "AV");
        }

        using (var db = factory.CreateContext())
        {
            var c = await db.SkillCategories.FindAsync(catId);
            Assert.Equal("Agilité & Vitesse", c!.Nom);
            Assert.Equal("AV", c.Code);
            // la compétence reste rattachée : le lien est par identifiant, pas par lettre
            var s = await db.Skills.SingleAsync();
            Assert.Equal(catId, s.SkillCategoryDefId);
        }
    }

    [Fact]
    public async Task SupprimerCategorie_RefuseSiUtilisee_EtIndiqueLeNombre()
    {
        using var factory = new TestDbFactory();
        int versionId, catId;
        using (var db = factory.CreateContext())
        {
            (_, versionId) = SeedVersion(db);
            catId = SeedCategorie(db, versionId, "Force", "F");
            db.Skills.Add(new Skill { RulesVersionId = versionId, SkillCategoryDefId = catId, Nom = "Châtaigne" });
            db.Skills.Add(new Skill { RulesVersionId = versionId, SkillCategoryDefId = catId, Nom = "Bras Multiples" });
            db.SaveChanges();
        }

        using (var ctx = factory.CreateContext())
        {
            var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SupprimerCategorieAsync(catId));
            Assert.Contains("2", ex.Message);
        }

        using (var db = factory.CreateContext())
            Assert.NotNull(await db.SkillCategories.FindAsync(catId));
    }

    [Fact]
    public async Task SupprimerCategorie_OkSiInutilisee()
    {
        using var factory = new TestDbFactory();
        int versionId, catId;
        using (var db = factory.CreateContext())
        {
            (_, versionId) = SeedVersion(db);
            catId = SeedCategorie(db, versionId, "Obsolète", "O");
        }

        using (var ctx = factory.CreateContext())
        {
            var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
            await svc.SupprimerCategorieAsync(catId);
        }

        using (var db = factory.CreateContext())
            Assert.Null(await db.SkillCategories.FindAsync(catId));
    }

    [Fact]
    public async Task ClonerVersion_CopieLesCategories_EtRattacheLesSkillsAuxCopies()
    {
        using var factory = new TestDbFactory();
        int gameId, srcVersionId, catId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; srcVersionId = vId;
            catId = SeedCategorie(db, srcVersionId, "Générale", "G");
            db.Skills.Add(new Skill { RulesVersionId = srcVersionId, SkillCategoryDefId = catId, Nom = "Blocage" });
            db.SaveChanges();
        }

        int newVersionId;
        using (var ctx = factory.CreateContext())
        {
            var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
            var nouvelle = await svc.CreerVersionAsync(gameId, "Saison 4", 2, false, srcVersionId);
            newVersionId = nouvelle.Id;
        }

        using (var db = factory.CreateContext())
        {
            var cat = await db.SkillCategories.SingleAsync(c => c.RulesVersionId == newVersionId);
            Assert.Equal("Générale", cat.Nom);
            Assert.Equal("G", cat.Code);
            Assert.NotEqual(catId, cat.Id); // vraie copie

            var skill = await db.Skills.SingleAsync(s => s.RulesVersionId == newVersionId);
            Assert.Equal(cat.Id, skill.SkillCategoryDefId); // rattaché à la copie, pas à l'original
        }
    }

    [Fact]
    public async Task GetCategories_TrieParNom()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext())
        {
            (_, versionId) = SeedVersion(db);
            SeedCategorie(db, versionId, "Passe", "P");
            SeedCategorie(db, versionId, "Agilité", "A");
            SeedCategorie(db, versionId, "Force", "F");
        }

        using var ctx = factory.CreateContext();
        var svc = new DataEditService(ctx, NullLogger<DataEditService>.Instance);
        var liste = await svc.GetCategoriesAsync(versionId);
        Assert.Equal(["Agilité", "Force", "Passe"], liste.Select(c => c.Nom));
    }

    [Fact]
    public void StandardSkillCategories_CouvreToutLAncienEnum()
    {
        // Garantit que la migration a une correspondance pour chaque valeur de l'ancien enum
        foreach (var valeur in Enum.GetValues<SkillCategory>())
        {
            Assert.False(string.IsNullOrWhiteSpace(StandardSkillCategories.Nom(valeur)));
            var code = StandardSkillCategories.Code(valeur);
            Assert.InRange(code.Length, 1, 2);
        }
    }
}
