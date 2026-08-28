using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Catalogue de règles spéciales d'équipe (LRB p.93-94), porté par la version
/// de règles et éditable par l'association depuis l'Admin.
///
/// Principe : une règle SANS <c>Code</c> est purement descriptive — elle
/// s'affiche sur la feuille d'équipe et rien d'autre. C'est le cas par défaut,
/// et celui de toute règle qu'une future édition amènera : l'association peut
/// donc suivre une nouvelle saison sans dev.
/// </summary>
public class SpecialRuleTests
{
    private static (int gameId, int versionId) SeedVersion(Data.ApplicationDbContext db, string nomVersion = "Saison 3")
    {
        var game = db.Games.FirstOrDefault(g => g.Nom == "Blood Bowl");
        if (game is null)
        {
            game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
            db.Games.Add(game);
            db.SaveChanges();
        }
        var v = new RulesVersion { GameId = game.Id, Nom = nomVersion, EstActive = true, Ordre = 1 };
        db.RulesVersions.Add(v);
        db.SaveChanges();
        return (game.Id, v.Id);
    }

    private static DataEditService Svc(Data.ApplicationDbContext db) =>
        new(db, NullLogger<DataEditService>.Instance);

    // ── Catalogue ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreerRegle_PersisteEtResteDescriptiveParDefaut()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
            await Svc(db).CreerRegleSpecialeAsync(versionId, "Bagarreurs Brutaux",
                "3 PSP au lieu de 2 pour une Élimination, 2 au lieu de 3 pour un Touchdown.");

        using (var db = factory.CreateContext())
        {
            var regle = await db.SpecialRules.SingleAsync();
            Assert.Equal("Bagarreurs Brutaux", regle.Nom);
            // Pas de code => descriptive. C'est le défaut voulu.
            Assert.Equal("", regle.Code);
        }
    }

    [Fact]
    public async Task CreerRegle_RefuseUnNomDejaPrisDansLaVersion()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
            await Svc(db).CreerRegleSpecialeAsync(versionId, "Capitaine", "…");

        using var db2 = factory.CreateContext();
        // insensible à la casse, comme partout ailleurs dans le projet
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Svc(db2).CreerRegleSpecialeAsync(versionId, "CAPITAINE", "…"));
    }

    [Fact]
    public async Task SupprimerRegle_RefuseSiEncoreRattachee()
    {
        using var factory = new TestDbFactory();
        int versionId, regleId, teamTypeId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            regleId = (await svc.CreerRegleSpecialeAsync(versionId, "Capitaine", "…")).Id;
            teamTypeId = (await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = "Humains" })).Id;
            await svc.AssocierRegleSpecialeAsync(teamTypeId, regleId);
        }

        using (var db = factory.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Svc(db).SupprimerRegleSpecialeAsync(regleId));
            // Message explicite plutôt qu'une erreur SQLite de contrainte.
            Assert.Contains("fiche(s) d'équipe", ex.Message);
        }

        // Une fois dissociée, la suppression passe.
        using (var db = factory.CreateContext())
        {
            await Svc(db).DissocierRegleSpecialeAsync(teamTypeId, regleId);
            await Svc(db).SupprimerRegleSpecialeAsync(regleId);
        }

        using (var db = factory.CreateContext())
            Assert.Empty(await db.SpecialRules.ToListAsync());
    }

    // ── Rattachement aux fiches d'équipe ─────────────────────────────────────

    [Fact]
    public async Task AssocierRegle_EnregistreLesOptionsEtLesNormalise()
    {
        using var factory = new TestDbFactory();
        int versionId, regleId, teamTypeId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            regleId = (await svc.CreerRegleSpecialeAsync(versionId, "Favori de…", "…", SpecialRuleCodes.FavoriDe)).Id;
            teamTypeId = (await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = "Renégats du Chaos" })).Id;
            // CSV saisi à la main : espaces parasites et entrée vide.
            await svc.AssocierRegleSpecialeAsync(teamTypeId, regleId, " Khorne , Nurgle ,, Tzeentch ");
        }

        using (var db = factory.CreateContext())
        {
            var lien = await db.TeamTypeSpecialRules.SingleAsync();
            Assert.Equal("Khorne,Nurgle,Tzeentch", lien.OptionsChoix);
        }
    }

    [Fact]
    public async Task AssocierRegle_DeuxFois_MetAJourSansDoublonner()
    {
        using var factory = new TestDbFactory();
        int versionId, regleId, teamTypeId;
        using (var db = factory.CreateContext()) (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            regleId = (await svc.CreerRegleSpecialeAsync(versionId, "Favori de…", "…", SpecialRuleCodes.FavoriDe)).Id;
            teamTypeId = (await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = "Nordiques" })).Id;
            await svc.AssocierRegleSpecialeAsync(teamTypeId, regleId, "Khorne");
            await svc.AssocierRegleSpecialeAsync(teamTypeId, regleId, "Khorne,Nurgle");
        }

        using (var db = factory.CreateContext())
        {
            var lien = await db.TeamTypeSpecialRules.SingleAsync();
            Assert.Equal("Khorne,Nurgle", lien.OptionsChoix);
        }
    }

    /// <summary>
    /// Garde-fou contre la corruption « entité d'une autre version » : une FK
    /// choisie par l'utilisateur dans une entité scopée par version doit être
    /// vérifiée côté SERVICE, pas seulement dans l'écran.
    /// </summary>
    [Fact]
    public async Task AssocierRegle_RefuseUneRegleDuneAutreVersion()
    {
        using var factory = new TestDbFactory();
        int versionA, versionB, regleB, teamTypeA;
        using (var db = factory.CreateContext())
        {
            (_, versionA) = SeedVersion(db, "Saison 3");
            (_, versionB) = SeedVersion(db, "Saison 4");
        }

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            regleB = (await svc.CreerRegleSpecialeAsync(versionB, "Capitaine", "…")).Id;
            teamTypeA = (await svc.CreerTeamTypeAsync(versionA, new TeamType { Nom = "Humains" })).Id;
        }

        using (var db = factory.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Svc(db).AssocierRegleSpecialeAsync(teamTypeA, regleB));
            Assert.Contains("autre version", ex.Message);
        }
    }

    // ── Obligations « version » : clonage, suppression ───────────────────────

    /// <summary>
    /// Cloner une version doit emporter le catalogue ET les rattachements,
    /// avec des FK pointant vers les copies — jamais vers la version source.
    /// Sans cela, chaque nouvelle édition repartirait sans règles spéciales.
    /// </summary>
    [Fact]
    public async Task ClonerVersion_CloneReglesEtRattachements()
    {
        using var factory = new TestDbFactory();
        int versionId, gameId;
        using (var db = factory.CreateContext()) (gameId, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            var regle = await svc.CreerRegleSpecialeAsync(versionId, "Favori de…", "Culte d'un Dieu du Chaos.", SpecialRuleCodes.FavoriDe);
            var tt = await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = "Pestiférés de Nurgle" });
            await svc.AssocierRegleSpecialeAsync(tt.Id, regle.Id, "Nurgle");
        }

        int nouvelleVersionId;
        using (var db = factory.CreateContext())
            nouvelleVersionId = (await Svc(db).CreerVersionAsync(gameId, "Saison 4", versionId)).Id;

        using (var db = factory.CreateContext())
        {
            var regleClonee = await db.SpecialRules
                .SingleAsync(r => r.RulesVersionId == nouvelleVersionId);
            Assert.Equal("Favori de…", regleClonee.Nom);
            Assert.Equal(SpecialRuleCodes.FavoriDe, regleClonee.Code);

            var ttClone = await db.TeamTypes
                .SingleAsync(t => t.RulesVersionId == nouvelleVersionId);

            var lien = await db.TeamTypeSpecialRules
                .SingleAsync(l => l.TeamTypeId == ttClone.Id);

            // Le rattachement pointe vers la règle CLONÉE, pas vers l'originale.
            Assert.Equal(regleClonee.Id, lien.SpecialRuleId);
            Assert.Equal("Nurgle", lien.OptionsChoix);
        }
    }

    /// <summary>
    /// Supprimer une version doit emporter son catalogue sans laisser de
    /// résidu : l'ordre de suppression est le piège habituel (FK Restrict).
    /// </summary>
    [Fact]
    public async Task SupprimerVersion_NeLaissePasDeRegleOrpheline()
    {
        using var factory = new TestDbFactory();
        int gameId, versionSource, versionJetable;
        using (var db = factory.CreateContext()) (gameId, versionSource) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            var regle = await svc.CreerRegleSpecialeAsync(versionSource, "Déferlement", "…");
            var tt = await svc.CreerTeamTypeAsync(versionSource, new TeamType { Nom = "Snotlings" });
            await svc.AssocierRegleSpecialeAsync(tt.Id, regle.Id);
        }

        // La version supprimée ne doit pas être l'active : on en crée une seconde.
        using (var db = factory.CreateContext())
            versionJetable = (await Svc(db).CreerVersionAsync(gameId, "Jetable", versionSource)).Id;

        using (var db = factory.CreateContext())
        {
            var v = await db.RulesVersions.FindAsync(versionJetable);
            v!.EstActive = false;
            await db.SaveChangesAsync();
        }

        using (var db = factory.CreateContext())
            await Svc(db).SupprimerVersionAsync(versionJetable);

        using (var db = factory.CreateContext())
        {
            Assert.Empty(await db.SpecialRules.Where(r => r.RulesVersionId == versionJetable).ToListAsync());
            // Le catalogue de la version SOURCE est intact.
            Assert.NotEmpty(await db.SpecialRules.Where(r => r.RulesVersionId == versionSource).ToListAsync());
        }
    }

    // ── Export / import ──────────────────────────────────────────────────────

    /// <summary>
    /// Aller-retour complet : un JSON exporté doit se réimporter avec son
    /// catalogue ET ses rattachements, résolus par NOM pour rester portable
    /// d'une instance à l'autre (le VPS de l'asso et une base locale n'ont pas
    /// les mêmes identifiants).
    /// </summary>
    [Fact]
    public async Task ExportImport_ConserveCatalogueEtRattachements()
    {
        using var factory = new TestDbFactory();
        int gameId, versionId;
        using (var db = factory.CreateContext()) (gameId, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = Svc(db);
            var regle = await svc.CreerRegleSpecialeAsync(
                versionId, "Favori de…", "Culte d'un Dieu du Chaos.", SpecialRuleCodes.FavoriDe, ordre: 2);
            var tt = await svc.CreerTeamTypeAsync(versionId, new TeamType { Nom = "Nains du Chaos" });
            await svc.AssocierRegleSpecialeAsync(tt.Id, regle.Id, "Hashut");
        }

        byte[] json;
        using (var db = factory.CreateContext())
            json = await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance)
                .ExportAsync(versionId);

        using (var db = factory.CreateContext())
        {
            using var stream = new MemoryStream(json);
            await new GameDataExportService(db, NullLogger<GameDataExportService>.Instance)
                .ImportAsync(stream, gameId, "Import");
        }

        using (var db = factory.CreateContext())
        {
            var versionImportee = await db.RulesVersions.SingleAsync(v => v.Nom == "Import");

            var regle = await db.SpecialRules.SingleAsync(r => r.RulesVersionId == versionImportee.Id);
            Assert.Equal("Favori de…", regle.Nom);
            Assert.Equal(SpecialRuleCodes.FavoriDe, regle.Code);
            Assert.Equal(2, regle.Ordre);

            var tt = await db.TeamTypes.SingleAsync(t => t.RulesVersionId == versionImportee.Id);
            var lien = await db.TeamTypeSpecialRules.SingleAsync(l => l.TeamTypeId == tt.Id);

            Assert.Equal(regle.Id, lien.SpecialRuleId);
            Assert.Equal("Hashut", lien.OptionsChoix);
        }
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Le seed rattache les règles par NOM d'équipe et NOM de règle. Une faute
    /// de frappe y serait silencieuse (le rattachement est simplement ignoré au
    /// démarrage), donc ce test vérifie que chaque nom cité existe réellement.
    ///
    /// C'est exactement l'erreur commise en écrivant ce seed : quatre noms
    /// d'équipe inventés (« Morts-Vivants », « Khemri »…) au lieu des vrais
    /// (« Morts-Ambulants », « Rois des Tombes »…).
    /// </summary>
    [Fact]
    public void SeedReglesSpeciales_NeCiteQueDesNomsExistants()
    {
        var nomsRegles = SpecialRuleSeedData.GetRegles(1).Select(r => r.Nom).ToHashSet();
        var nomsEquipes = BloodBowlTeamSeedData.GetTeams(1, 1)
            .Select(t => t.Type.Nom).ToHashSet();

        var reglesInconnues = new List<string>();
        var equipesInconnues = new List<string>();

        foreach (var (regle, equipe, _) in SpecialRuleSeedData.GetRattachements())
        {
            if (!nomsRegles.Contains(regle)) reglesInconnues.Add(regle);
            if (!nomsEquipes.Contains(equipe)) equipesInconnues.Add(equipe);
        }

        Assert.Empty(reglesInconnues);
        Assert.Empty(equipesInconnues);
    }

    /// <summary>
    /// « Favori de… » est la seule règle qui déclenche un comportement. Chaque
    /// équipe qui la porte doit donc proposer au moins une divinité, sinon le
    /// commissaire ouvrirait une liste de choix vide.
    /// </summary>
    [Fact]
    public void SeedFavoriDe_ProposeToujoursAuMoinsUneDivinite()
    {
        var sansOption = SpecialRuleSeedData.GetRattachements()
            .Where(r => r.Regle == "Favori de…" && string.IsNullOrWhiteSpace(r.Options))
            .Select(r => r.Equipe)
            .ToList();

        Assert.Empty(sansOption);
    }

    /// <summary>
    /// La plupart des règles restent DESCRIPTIVES : elles s'affichent et se
    /// jouent à la table. Seules celles listées ici portent un comportement
    /// automatique. Ce test rend l'inventaire explicite pour qu'ajouter un Code
    /// reste un geste conscient — et pour que le mot-clé visé ne soit jamais
    /// oublié sur une règle qui en a besoin.
    /// </summary>
    [Fact]
    public void SeedCatalogue_SeulesLesReglesAttenduesSontBranchees()
    {
        var codesParRegle = SpecialRuleSeedData.GetRegles(1)
            .Where(r => !string.IsNullOrEmpty(r.Code))
            .ToDictionary(r => r.Nom, r => r.Code);

        Assert.Equal(3, codesParRegle.Count);
        Assert.Equal(SpecialRuleCodes.FavoriDe, codesParRegle["Favori de…"]);
        Assert.Equal(SpecialRuleCodes.CoutNulParMotCle, codesParRegle["Trois-quarts à Vil Prix"]);
        Assert.Equal(SpecialRuleCodes.RecrutementGratuitParMotCle, codesParRegle["Maîtres de la Non-Vie"]);
    }

    /// <summary>
    /// Une règle à comportement automatique sans paramètre ne ferait RIEN
    /// (aucun mot-clé visé, aucune divinité proposée) : le seed doit donc
    /// renseigner OptionsChoix sur chacun de ses rattachements.
    /// </summary>
    [Fact]
    public void SeedRattachements_ToutesLesReglesBrancheesOntUnParametre()
    {
        var reglesBranchees = SpecialRuleSeedData.GetRegles(1)
            .Where(r => !string.IsNullOrEmpty(r.Code))
            .Select(r => r.Nom)
            .ToHashSet();

        var sansParametre = SpecialRuleSeedData.GetRattachements()
            .Where(r => reglesBranchees.Contains(r.Regle) && string.IsNullOrWhiteSpace(r.Options))
            .Select(r => $"{r.Regle} / {r.Equipe}")
            .ToList();

        Assert.Empty(sansParametre);
    }

    // ── Feuille imprimée ─────────────────────────────────────────────────────

    /// <summary>
    /// Les règles rattachées à la race doivent apparaître sur la feuille
    /// imprimée : c'est le document posé sur la table pendant le match.
    /// </summary>
    [Fact]
    public void FeuilleEquipePdf_ImprimeLesReglesSpeciales()
    {
        var teamType = new TeamType { Nom = "Nurgle", CoutRelance = 70_000 };
        teamType.ReglesSpecialesListe.Add(new TeamTypeSpecialRule
        {
            OptionsChoix = "Nurgle",
            SpecialRule = new SpecialRule
            {
                Nom = "Favori de…", Ordre = 1, Code = SpecialRuleCodes.FavoriDe,
                Description = "L'équipe rend hommage à un Dieu du Chaos."
            }
        });
        teamType.ReglesSpecialesListe.Add(new TeamTypeSpecialRule
        {
            SpecialRule = new SpecialRule
            {
                Nom = "Bagarreurs Brutaux", Ordre = 2,
                Description = "3 PSP au lieu de 2 pour une Élimination."
            }
        });

        var equipe = new Team { Nom = "Les Pustuleux", TeamType = teamType };
        // Version COMPLÈTE : c'est elle qui porte les descriptions. La version
        // compacte ne garde que les noms (voir FavoriDeTests).
        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, true));

        Assert.Contains("Règles spéciales", texte);
        Assert.Contains("Favori de", texte);
        Assert.Contains("Bagarreurs Brutaux", texte);
        // La description est imprimée, pas seulement le nom.
        Assert.Contains("3 PSP au lieu de 2", texte);
    }

    /// <summary>Une race sans règle spéciale n'imprime pas de bloc vide.</summary>
    [Fact]
    public void FeuilleEquipePdf_SansRegle_NImprimePasLeBloc()
    {
        var equipe = new Team
        {
            Nom = "Les Testeurs",
            TeamType = new TeamType { Nom = "Humains", CoutRelance = 50_000 }
        };

        var texte = LireTextePdf(new PdfService().GenererFeuilleEquipe(equipe, false));

        Assert.DoesNotContain("Règles spéciales", texte);
    }

    private static string LireTextePdf(byte[] pdf)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }
}
