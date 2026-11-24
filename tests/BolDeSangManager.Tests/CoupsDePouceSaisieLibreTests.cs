using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Coups de pouce détenus par une équipe d'une ligue en « Saisie libre »
/// (TeamInducement) : quantité, coût × quantité dans la VEA, rien débité,
/// au-delà du maximum en simple avertissement, tracés, imprimés sur le PDF.
/// Chaque règle a sa contre-épreuve.
/// </summary>
public class CoupsDePouceSaisieLibreTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static TeamService Teams(ApplicationDbContext db) => new(db, NullLogger<TeamService>.Instance);

    private static SaisieLibreService Libre(ApplicationDbContext db, bool commissaire = false) =>
        new(db, new StubAuth(commissaire), Teams(db),
            new JoueurEditionService(db, new StubAuth(commissaire), NullLogger<JoueurEditionService>.Instance),
            NullLogger<SaisieLibreService>.Instance);

    private sealed class StubAuth(bool commissaire) : IAuthorizationService
    {
        public Task<bool> EstAdminAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstGrandCommissaireAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId) => Task.FromResult(commissaire);
        public Task<bool> PeutGererLigueAsync(string userId, int ligueId) => Task.FromResult(commissaire);
        public Task<bool> PeutEditerDonneesAsync(string userId) => Task.FromResult(false);
        public Task<bool> PeutGererSettingsAsync(string userId) => Task.FromResult(false);
    }

    private sealed record Fixture(string Coach, string Commissaire, int LigueId, int TeamTypeId, int PositionId,
        int PotsDeVin, int Prieres, int Arbitre, int AutreVersion);

    private async Task<Fixture> SetupAsync(bool libre, LeagueStatus statut = LeagueStatus.Inscription)
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("comm");
        var coach = DataSeeder.CreateUser("coach");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id, statut);
        ligue.SaisieLibre = libre;

        var autreVersion = new RulesVersion { GameId = game.Id, Nom = "Autre" };
        db.RulesVersions.Add(autreVersion);
        await db.SaveChangesAsync();

        var pots = new Inducement { RulesVersionId = rv.Id, Nom = "Pots-de-vin", Cout = 100_000, QuantiteMax = 3 };
        var prieres = new Inducement { RulesVersionId = rv.Id, Nom = "Prières à Nuffle", Cout = 10_000, QuantiteMax = 3 };
        var arbitre = new Inducement { RulesVersionId = rv.Id, Nom = "Arbitre Partial", Cout = 0, QuantiteMax = 1 };
        var etranger = new Inducement { RulesVersionId = autreVersion.Id, Nom = "Pots-de-vin", Cout = 100_000 };
        db.Inducements.AddRange(pots, prieres, arbitre, etranger);
        await db.SaveChangesAsync();

        return new Fixture(coach.Id, commissaire.Id, ligue.Id, teamType.Id, position.Id,
            pots.Id, prieres.Id, arbitre.Id, etranger.Id);
    }

    private static Team NouvelleEquipe(Fixture f) => new()
    {
        Nom = "Les Revenants", CoachId = f.Coach, LeagueId = f.LigueId, TeamTypeId = f.TeamTypeId, Tresorerie = 100_000
    };

    private async Task<int> CreerAsync(Fixture f, Dictionary<int, int>? cdp = null)
    {
        await using var db = _factory.CreateContext();
        return (await Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, cdp is null ? null : Q(cdp))).Id;
    }

    /// <summary>id → quantité, sans prix ni star.</summary>
    private static Dictionary<int, CoupDePouceSaisi> Q(Dictionary<int, int> quantites) =>
        quantites.ToDictionary(kv => kv.Key, kv => new CoupDePouceSaisi(kv.Value));

    // ── Création ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Creation_SaisieLibre_CoutFoisQuantiteDansLaVea_RienDebite()
    {
        var f = await SetupAsync(libre: true);
        var sans = await CreerAsync(f);
        var avec = await CreerAsync(f, new() { [f.PotsDeVin] = 2, [f.Prieres] = 1, [f.Arbitre] = 1 });

        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        var a = (await svc.GetEquipeAsync(sans))!;
        var b = (await svc.GetEquipeAsync(avec))!;
        Assert.Equal(2 * 100_000 + 10_000, svc.CalculerVEA(b) - svc.CalculerVEA(a));   // l'arbitre (prix variable = 0) n'ajoute rien
        Assert.Equal(100_000, b.Tresorerie);
        Assert.Equal(2, b.CoupsDePouce.Single(c => c.InducementId == f.PotsDeVin).Quantite);
    }

    [Fact]
    public async Task ListesEquipes_ChargentLesCoupsDePouce()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.PotsDeVin] = 1 });
        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        Assert.NotNull((await svc.GetEquipesLigueAsync(f.LigueId)).Single().CoupsDePouce.Single().Inducement);
        Assert.NotNull((await svc.GetEquipesCoachAsync(f.Coach)).Single().CoupsDePouce.Single().Inducement);
    }

    [Fact]
    public async Task Creation_QuantiteAuDelaDuMaximum_Acceptee()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Prieres] = 4 });
        await using var db = _factory.CreateContext();
        Assert.Equal(4, (await db.TeamInducements.SingleAsync(x => x.TeamId == id)).Quantite);
    }

    [Fact]
    public async Task Creation_LigueClassique_Refusee()
    {
        var f = await SetupAsync(libre: false);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, Q(new Dictionary<int, int> { [f.PotsDeVin] = 1 })));
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_CoupDePouceDUneAutreVersion_Refuse()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, Q(new Dictionary<int, int> { [f.AutreVersion] = 1 })));
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_QuantiteNegative_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, Q(new Dictionary<int, int> { [f.PotsDeVin] = -1 })));
    }

    [Fact]
    public async Task ModifierEquipe_NulGarde_DictionnaireRemplace()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.PotsDeVin] = 2 });

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)]);
        await using (var db = _factory.CreateContext())
            Assert.Equal(2, (await db.TeamInducements.SingleAsync()).Quantite);

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)], Q(new Dictionary<int, int> { [f.Prieres] = 3 }));
        await using (var db = _factory.CreateContext())
        {
            var l = await db.TeamInducements.SingleAsync();
            Assert.Equal((f.Prieres, 3), (l.InducementId, l.Quantite));
        }
    }

    // ── Fiche d'équipe ──────────────────────────────────────────────────────

    private async Task<int> EquipeEnCoursAsync(Fixture f)
    {
        await using var db = _factory.CreateContext();
        return (await DataSeeder.SeedTeamAsync(db, f.LigueId, f.Coach, f.TeamTypeId)).Id;
    }

    [Fact]
    public async Task Fiche_AjoutModifRetrait_Traces_SaisonLancee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);

        await using (var db = _factory.CreateContext())
            Assert.Empty(await Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, Q(new Dictionary<int, int> { [f.PotsDeVin] = 1, [f.Prieres] = 2 })));
        await using (var db = _factory.CreateContext())
            // Prières passe à 4 (> 3) : accepté, signalé. Pots-de-vin retiré (absent = 0).
            Assert.Equal(["Prières à Nuffle"],
                await Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, Q(new Dictionary<int, int> { [f.Prieres] = 4 })));

        await using var verif = _factory.CreateContext();
        var l = await verif.TeamInducements.SingleAsync(x => x.TeamId == id);
        Assert.Equal((f.Prieres, 4), (l.InducementId, l.Quantite));
        var traces = await verif.ModificationsLibres.Where(m => m.TeamId == id).OrderBy(m => m.Id)
            .Select(m => $"{m.Champ}:{m.Avant}>{m.Apres}").ToListAsync();
        // Ordre d'écriture au sein d'un même enregistrement non garanti : on compare les ensembles.
        Assert.Equal(new[]
        {
            "Coup de pouce : Pots-de-vin:0>1", "Coup de pouce : Prières à Nuffle:0>2",
            "Coup de pouce : Prières à Nuffle:2>4", "Coup de pouce : Pots-de-vin:1>0"
        }.OrderBy(t => t), traces.OrderBy(t => t));
    }

    [Fact]
    public async Task Fiche_LigueClassique_Refusee()
    {
        var f = await SetupAsync(libre: false, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, Q(new Dictionary<int, int> { [f.PotsDeVin] = 1 })));
    }

    [Fact]
    public async Task Fiche_CoachTiers_Refuse_CommissaireAccepte()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierCoupsDePouceAsync(id, "un-autre", Q(new Dictionary<int, int> { [f.PotsDeVin] = 1 })));
        await Libre(db, commissaire: true).ModifierCoupsDePouceAsync(id, f.Commissaire, Q(new Dictionary<int, int> { [f.PotsDeVin] = 1 }));
        Assert.Equal(1, await db.TeamInducements.CountAsync(x => x.TeamId == id));
    }

    // ── Catalogue, suppression, export, PDF ─────────────────────────────────

    [Fact]
    public async Task SupprimerDuCatalogue_Detenu_Refuse_SinonAccepte()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.PotsDeVin] = 1 });
        await using var db = _factory.CreateContext();
        var data = new DataEditService(db, NullLogger<DataEditService>.Instance);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => data.SupprimerCoupDePouceAsync(f.PotsDeVin));
        Assert.Contains("Pots-de-vin", ex.Message);
        await data.SupprimerCoupDePouceAsync(f.Arbitre);
        Assert.False(await db.Inducements.AnyAsync(i => i.Id == f.Arbitre));
    }

    [Fact]
    public async Task SupprimerLigue_AvecCoupsDePouce_Reussit()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.PotsDeVin] = 1 });
        await using (var db = _factory.CreateContext())
            await new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuth(true),
                new StaffService(db, NullLogger<StaffService>.Instance)).SupprimerLigueAsync(f.LigueId);
        await using var verif = _factory.CreateContext();
        Assert.Empty(await verif.TeamInducements.ToListAsync());
    }

    [Fact]
    public async Task ExportImportLigue_ConserveLesCoupsDePouce()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.PotsDeVin] = 2, [f.Prieres] = 1 });

        byte[] json;
        await using (var db = _factory.CreateContext())
            json = await new LeagueExportService(db, NullLogger<LeagueExportService>.Instance,
                new StaffService(db, NullLogger<StaffService>.Instance)).ExportAsync(f.LigueId);
        int importee;
        await using (var db = _factory.CreateContext())
            importee = (await new LeagueExportService(db, NullLogger<LeagueExportService>.Instance,
                new StaffService(db, NullLogger<StaffService>.Instance)).ImportAsync(new MemoryStream(json), f.Commissaire)).Id;

        await using var verif = _factory.CreateContext();
        var lignes = await verif.TeamInducements.Where(x => x.Team.LeagueId == importee)
            .OrderBy(x => x.Inducement.Nom).Select(x => $"{x.Inducement.Nom}×{x.Quantite}").ToListAsync();
        Assert.Equal(["Pots-de-vin×2", "Prières à Nuffle×1"], lignes);
    }

    [Fact]
    public async Task Pdf_FeuilleEquipe_ImprimeLesCoupsDePouceDetenus()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.PotsDeVin] = 2 });
        await using var db = _factory.CreateContext();
        var equipe = (await Teams(db).GetEquipeAsync(id))!;

        using var doc = UglyToad.PdfPig.PdfDocument.Open(new PdfService().GenererFeuilleEquipe(equipe, false));
        var texte = string.Join("\n", doc.GetPages().Select(p => p.Text));
        Assert.Contains("Coups de pouce", texte);
        Assert.Contains("Pots-de-vin ×2", texte);
    }
}
