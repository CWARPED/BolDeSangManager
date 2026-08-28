using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Catégorie officielle LRB (p.94) portée par <see cref="TeamType.Categorie"/> :
/// 1 = équipes les plus performantes … 4 = les plus faibles, 0 = non renseignée.
///
/// Purement informative. Elle remplace à l'affichage l'ancien « style de jeu »
/// maison (<see cref="TeamCategory"/>), dont la colonne reste en base sous
/// <c>TeamType.StyleJeuObsolete</c> pour ne casser aucune base existante.
/// </summary>
public class CategorieLrbTests
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

    private static async Task<int> CreerTypeAsync(TestDbFactory factory, int versionId, string nom)
    {
        using var db = factory.CreateContext();
        var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
        var t = await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = nom, CoutRelance = 60_000 });
        return t.Id;
    }

    [Fact]
    public async Task NouveauTeamType_NaitSansCategorie()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        var id = await CreerTypeAsync(factory, versionId, "Humains");

        using (var db = factory.CreateContext())
        {
            // 0 = « non renseignée » : les commissaires font la passe de saisie
            // eux-mêmes, il n'y a volontairement ni seed ni backfill.
            Assert.Equal(0, (await db.TeamTypes.FindAsync(id))!.Categorie);
        }
    }

    [Theory]
    [InlineData(0)]  // non renseignée
    [InlineData(1)]
    [InlineData(4)]
    public async Task ModifierTeamType_AccepteLesCategoriesValides(int categorie)
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);
        var id = await CreerTypeAsync(factory, versionId, "Nains");

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ModifierTeamTypeAsync(id, "Nains", categorie, 70_000, "", "");
        }

        using (var db = factory.CreateContext())
            Assert.Equal(categorie, (await db.TeamTypes.FindAsync(id))!.Categorie);
    }

    /// <summary>
    /// Validation côté SERVICE, pas seulement côté UI : le LRB ne définit que
    /// quatre catégories, une valeur hors bornes est une donnée corrompue.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(99)]
    public async Task ModifierTeamType_RefuseUneCategorieHorsBornes(int categorie)
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);
        var id = await CreerTypeAsync(factory, versionId, "Orques");

        using var db2 = factory.CreateContext();
        var svc = new DataEditService(db2, NullLogger<DataEditService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ModifierTeamTypeAsync(id, "Orques", categorie, 60_000, "", ""));
    }

    /// <summary>
    /// Le clonage d'une version doit emporter la catégorie : sans cela, chaque
    /// nouvelle édition de règles obligerait les commissaires à refaire toute
    /// la passe de saisie (principe « l'asso maintient les éditions sans dev »).
    /// </summary>
    [Fact]
    public async Task ClonerVersion_ConserveLaCategorie()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);
        var id = await CreerTypeAsync(factory, versionId, "Elfes Sylvains");

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ModifierTeamTypeAsync(id, "Elfes Sylvains", 3, 50_000, "", "");
        }

        int nouvelleVersionId;
        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var v = await svc.CreerVersionAsync(
                (await db.RulesVersions.FirstAsync()).GameId, "Saison 4", versionId);
            nouvelleVersionId = v.Id;
        }

        using (var db = factory.CreateContext())
        {
            var clone = await db.TeamTypes
                .FirstAsync(t => t.RulesVersionId == nouvelleVersionId && t.Nom == "Elfes Sylvains");
            Assert.Equal(3, clone.Categorie);
        }
    }

    /// <summary>
    /// La feuille imprimée porte la catégorie — c'est le document que les
    /// coaches ont sur la table pendant un match.
    /// </summary>
    [Fact]
    public void FeuilleEquipePdf_AfficheLaCategorieRenseignee()
    {
        var equipe = new Team
        {
            Nom = "Les Marteaux",
            TeamType = new TeamType { Nom = "Nains", CoutRelance = 70_000, Categorie = 2 }
        };

        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, false));

        Assert.Contains("Catégorie 2", texte);
    }

    /// <summary>
    /// Tant que les commissaires n'ont pas fait leur passe de saisie, la
    /// feuille ne doit PAS afficher une catégorie « 0 » trompeuse.
    /// </summary>
    [Fact]
    public void FeuilleEquipePdf_TaitLaCategorieNonRenseignee()
    {
        var equipe = new Team
        {
            Nom = "Les Marteaux",
            TeamType = new TeamType { Nom = "Nains", CoutRelance = 70_000, Categorie = 0 }
        };

        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, false));

        Assert.DoesNotContain("Catégorie", texte);
    }

    private static string LireTextePdf(byte[] pdf)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }
}
