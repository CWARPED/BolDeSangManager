using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Export / import du SEUL catalogue de règles spéciales.
///
/// Besoin réel : l'instance de production a déjà ses races, ses ligues et ses
/// équipes. L'import global crée une NOUVELLE version de règles — inutilisable
/// ici, il faudrait tout remigrer. Ce format-ci FUSIONNE les règles dans une
/// version existante, sans toucher au reste.
/// </summary>
public class ReglesSpecialesPortablesTests
{
    private static GameDataExportService Svc(ApplicationDbContext db) =>
        new(db, NullLogger<GameDataExportService>.Instance);

    /// <summary>Version avec deux races, et éventuellement un catalogue.</summary>
    private static async Task<int> SeedVersionAsync(
        TestDbFactory factory, bool avecRegles, string nomSecondeRace = "Snotlings")
    {
        using var db = factory.CreateContext();

        var (game, version) = await DataSeeder.SeedGameAsync(db);

        foreach (var nom in new[] { "Ogres", nomSecondeRace })
            db.TeamTypes.Add(new TeamType
            {
                Nom = nom, GameId = game.Id,
                RulesVersionId = version.Id, CoutRelance = 60_000
            });
        await db.SaveChangesAsync();

        if (avecRegles)
        {
            var regle = new SpecialRule
            {
                RulesVersionId = version.Id, Nom = "Trois-quarts à Vil Prix",
                Description = "Les Trois-quarts comptent pour 0 po dans la VEA.",
                Ordre = 4, Code = SpecialRuleCodes.CoutNulParMotCle
            };
            var descriptive = new SpecialRule
            {
                RulesVersionId = version.Id, Nom = "Déferlement",
                Description = "D3 Trois-quarts supplémentaires.", Ordre = 6, Code = ""
            };
            db.SpecialRules.AddRange(regle, descriptive);
            await db.SaveChangesAsync();

            var ogres = await db.TeamTypes.FirstAsync(t => t.Nom == "Ogres");
            db.TeamTypeSpecialRules.Add(new TeamTypeSpecialRule
            {
                TeamTypeId = ogres.Id, SpecialRuleId = regle.Id, OptionsChoix = "Trois-quart"
            });
            db.TeamTypeSpecialRules.Add(new TeamTypeSpecialRule
            {
                TeamTypeId = ogres.Id, SpecialRuleId = descriptive.Id, OptionsChoix = ""
            });
            await db.SaveChangesAsync();
        }

        return version.Id;
    }

    private static async Task<int> SecondeVersionVideAsync(TestDbFactory factory)
    {
        using var db = factory.CreateContext();
        var game = await db.Games.FirstAsync();

        var v = new RulesVersion { GameId = game.Id, Nom = "Saison 4", Ordre = 2 };
        db.RulesVersions.Add(v);
        await db.SaveChangesAsync();

        foreach (var nom in new[] { "Ogres", "Snotlings" })
            db.TeamTypes.Add(new TeamType
            {
                Nom = nom, GameId = game.Id, RulesVersionId = v.Id, CoutRelance = 60_000
            });
        await db.SaveChangesAsync();

        return v.Id;
    }

    // ── Export ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ContientLesReglesEtLeursRattachements()
    {
        using var factory = new TestDbFactory();
        var versionId = await SeedVersionAsync(factory, avecRegles: true);

        using var db = factory.CreateContext();
        var json = System.Text.Encoding.UTF8.GetString(
            await Svc(db).ExportReglesSpecialesAsync(versionId));

        Assert.Contains("Trois-quarts à Vil Prix", json);
        Assert.Contains(SpecialRuleCodes.CoutNulParMotCle, json);
        // Le rattachement et son paramètre : sans eux le fichier serait inerte.
        Assert.Contains("Ogres", json);
        Assert.Contains("Trois-quart", json);
    }

    /// <summary>Une version sans règle produit un fichier valide, pas une erreur.</summary>
    [Fact]
    public async Task Export_SansRegle_ProduitUnFichierVide()
    {
        using var factory = new TestDbFactory();
        var versionId = await SeedVersionAsync(factory, avecRegles: false);

        using var db = factory.CreateContext();
        var octets = await Svc(db).ExportReglesSpecialesAsync(versionId);

        Assert.NotEmpty(octets);
    }

    // ── Import ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Le cas d'usage : on transporte le catalogue vers une version EXISTANTE
    /// qui a déjà ses races. Rien n'est recréé, tout est fusionné.
    /// </summary>
    [Fact]
    public async Task Import_FusionneDansUneVersionExistante()
    {
        using var factory = new TestDbFactory();
        var source = await SeedVersionAsync(factory, avecRegles: true);
        var cible = await SecondeVersionVideAsync(factory);

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await Svc(db).ExportReglesSpecialesAsync(source);

        using (var db = factory.CreateContext())
        {
            using var stream = new MemoryStream(fichier);
            var (ok, erreurs) = await Svc(db).ImportReglesSpecialesAsync(cible, stream);
            Assert.True(ok, string.Join(" | ", erreurs));
        }

        using (var db = factory.CreateContext())
        {
            var regles = await db.SpecialRules.Where(r => r.RulesVersionId == cible).ToListAsync();
            Assert.Equal(2, regles.Count);

            var vilPrix = regles.First(r => r.Nom == "Trois-quarts à Vil Prix");
            Assert.Equal(SpecialRuleCodes.CoutNulParMotCle, vilPrix.Code);

            // Le rattachement suit, avec son mot-clé.
            var lien = await db.TeamTypeSpecialRules
                .Include(l => l.TeamType).Include(l => l.SpecialRule)
                .FirstAsync(l => l.SpecialRule.RulesVersionId == cible
                              && l.SpecialRule.Nom == "Trois-quarts à Vil Prix");
            Assert.Equal("Ogres", lien.TeamType.Nom);
            Assert.Equal("Trois-quart", lien.OptionsChoix);
        }
    }

    /// <summary>
    /// Réimporter le même fichier ne doit rien dupliquer : un commissaire qui
    /// clique deux fois ne doit pas se retrouver avec deux catalogues.
    ///
    /// ⚠️ Vérifier le comptage NE SUFFIT PAS : un index unique
    /// (RulesVersionId, Nom) fait échouer un doublon, l'import part en rollback
    /// et les données restent correctes — le test passerait alors qu'un
    /// commissaire recevrait une erreur. On exige donc que le second import
    /// RÉUSSISSE, ce qui est la vraie propriété attendue.
    /// </summary>
    [Fact]
    public async Task Import_EstIdempotent()
    {
        using var factory = new TestDbFactory();
        var source = await SeedVersionAsync(factory, avecRegles: true);
        var cible = await SecondeVersionVideAsync(factory);

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await Svc(db).ExportReglesSpecialesAsync(source);

        for (var i = 1; i <= 2; i++)
            using (var db = factory.CreateContext())
            {
                using var stream = new MemoryStream(fichier);
                var (ok, erreurs) = await Svc(db).ImportReglesSpecialesAsync(cible, stream);
                Assert.True(ok, $"Import n°{i} a échoué : {string.Join(" | ", erreurs)}");
            }

        using (var db = factory.CreateContext())
        {
            Assert.Equal(2, await db.SpecialRules.CountAsync(r => r.RulesVersionId == cible));
            Assert.Equal(2, await db.TeamTypeSpecialRules
                .CountAsync(l => l.SpecialRule.RulesVersionId == cible));
        }
    }

    /// <summary>
    /// Une race absente de la cible ne doit pas faire échouer tout l'import :
    /// les autres rattachements passent, et le commissaire est informé.
    /// C'est le cas réel d'une instance dont une race porte un autre nom.
    /// </summary>
    [Fact]
    public async Task Import_RaceInconnue_NInterromptPasLeReste()
    {
        using var factory = new TestDbFactory();
        // La source rattache la règle aux « Ogres » ; la cible ne les a pas.
        var source = await SeedVersionAsync(factory, avecRegles: true);

        int cible;
        using (var db = factory.CreateContext())
        {
            var game = await db.Games.FirstAsync();
            var v = new RulesVersion { GameId = game.Id, Nom = "Sans Ogres", Ordre = 3 };
            db.RulesVersions.Add(v);
            await db.SaveChangesAsync();
            db.TeamTypes.Add(new TeamType
            {
                Nom = "Humains", GameId = game.Id, RulesVersionId = v.Id, CoutRelance = 50_000
            });
            await db.SaveChangesAsync();
            cible = v.Id;
        }

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await Svc(db).ExportReglesSpecialesAsync(source);

        List<string> erreurs;
        using (var db = factory.CreateContext())
        {
            using var stream = new MemoryStream(fichier);
            (_, erreurs) = await Svc(db).ImportReglesSpecialesAsync(cible, stream);
        }

        using (var db = factory.CreateContext())
        {
            // Les règles sont bien créées…
            Assert.Equal(2, await db.SpecialRules.CountAsync(r => r.RulesVersionId == cible));
            // …mais aucun rattachement, et le rapport le dit.
            Assert.Equal(0, await db.TeamTypeSpecialRules
                .CountAsync(l => l.SpecialRule.RulesVersionId == cible));
        }

        Assert.Contains(erreurs, e => e.Contains("Ogres"));
    }

    /// <summary>
    /// Une règle déjà présente est MISE À JOUR, pas dupliquée : c'est ainsi
    /// qu'on propage un correctif de description ou l'ajout d'un comportement
    /// automatique sur une instance déjà en service.
    /// </summary>
    [Fact]
    public async Task Import_MetAJourUneRegleExistante()
    {
        using var factory = new TestDbFactory();
        var source = await SeedVersionAsync(factory, avecRegles: true);
        var cible = await SecondeVersionVideAsync(factory);

        // La cible a déjà la règle, mais purement descriptive et mal décrite.
        using (var db = factory.CreateContext())
        {
            db.SpecialRules.Add(new SpecialRule
            {
                RulesVersionId = cible, Nom = "Trois-quarts à Vil Prix",
                Description = "ancienne description", Ordre = 99, Code = ""
            });
            await db.SaveChangesAsync();
        }

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await Svc(db).ExportReglesSpecialesAsync(source);

        using (var db = factory.CreateContext())
        {
            using var stream = new MemoryStream(fichier);
            await Svc(db).ImportReglesSpecialesAsync(cible, stream);
        }

        using (var db = factory.CreateContext())
        {
            var regles = await db.SpecialRules
                .Where(r => r.RulesVersionId == cible && r.Nom == "Trois-quarts à Vil Prix")
                .ToListAsync();

            Assert.Single(regles);   // pas de doublon
            Assert.Equal(SpecialRuleCodes.CoutNulParMotCle, regles[0].Code);
            Assert.DoesNotContain("ancienne", regles[0].Description);
        }
    }

    /// <summary>Un fichier illisible est refusé proprement, sans exception.</summary>
    [Fact]
    public async Task Import_FichierInvalide_RetourneUneErreur()
    {
        using var factory = new TestDbFactory();
        var cible = await SeedVersionAsync(factory, avecRegles: false);

        using var db = factory.CreateContext();
        using var stream = new MemoryStream("ceci n'est pas du JSON"u8.ToArray());
        var (ok, erreurs) = await Svc(db).ImportReglesSpecialesAsync(cible, stream);

        Assert.False(ok);
        Assert.NotEmpty(erreurs);
    }

    /// <summary>
    /// L'import ne doit toucher QUE les règles : races, postes et compétences
    /// de la version cible restent intacts.
    /// </summary>
    [Fact]
    public async Task Import_NeTouchePasAuResteDeLaVersion()
    {
        using var factory = new TestDbFactory();
        var source = await SeedVersionAsync(factory, avecRegles: true);
        var cible = await SecondeVersionVideAsync(factory);

        int racesAvant;
        using (var db = factory.CreateContext())
            racesAvant = await db.TeamTypes.CountAsync(t => t.RulesVersionId == cible);

        byte[] fichier;
        using (var db = factory.CreateContext())
            fichier = await Svc(db).ExportReglesSpecialesAsync(source);

        using (var db = factory.CreateContext())
        {
            using var stream = new MemoryStream(fichier);
            await Svc(db).ImportReglesSpecialesAsync(cible, stream);
        }

        using (var db = factory.CreateContext())
            Assert.Equal(racesAvant, await db.TeamTypes.CountAsync(t => t.RulesVersionId == cible));
    }
}
