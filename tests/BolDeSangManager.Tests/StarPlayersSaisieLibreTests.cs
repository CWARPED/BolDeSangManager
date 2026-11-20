using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Star players recrutés dans une ligue en « Saisie libre » (TeamStarPlayer) :
/// choisis parmi ceux accessibles à la race, à la création et depuis la fiche,
/// comptés dans la VEA, tracés, une même star une seule fois, au-delà de 2 en
/// simple avertissement. Chaque règle a sa contre-épreuve.
/// </summary>
public class StarPlayersSaisieLibreTests : IDisposable
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
        int Varag, int Griff, int Morg, int Interdit);

    /// <summary>
    /// Race « Humains » rattachée à la ligue thématique « Vieux Monde ».
    /// Varag (sans ligue = accessible à tous), Griff (Vieux Monde), Morg (sans
    /// ligue) sont recrutables ; Interdit (ligue « Badlands » seulement) ne l'est pas.
    /// </summary>
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

        var vieuxMonde = new ThemedLeague { RulesVersionId = rv.Id, Nom = "Vieux Monde" };
        var badlands = new ThemedLeague { RulesVersionId = rv.Id, Nom = "Badlands" };
        db.ThemedLeagues.AddRange(vieuxMonde, badlands);
        await db.SaveChangesAsync();
        db.Add(new TeamTypeThemedLeague { TeamTypeId = teamType.Id, ThemedLeagueId = vieuxMonde.Id });

        StarPlayer Star(string nom, int cout) => new()
        {
            RulesVersionId = rv.Id, Nom = nom, Cout = cout, Mouvement = 5, Force = 4,
            Agilite = "4+", CapacitePasse = "5+", Armure = "10+", Competences = "Blocage, Châtaigne"
        };
        var varag = Star("Varag", 280_000);
        var griff = Star("Griff", 300_000);
        var morg = Star("Morg", 340_000);
        var interdit = Star("Interdit", 100_000);
        db.StarPlayers.AddRange(varag, griff, morg, interdit);
        await db.SaveChangesAsync();
        db.Add(new StarPlayerThemedLeague { StarPlayerId = griff.Id, ThemedLeagueId = vieuxMonde.Id });
        db.Add(new StarPlayerThemedLeague { StarPlayerId = interdit.Id, ThemedLeagueId = badlands.Id });
        await db.SaveChangesAsync();

        return new Fixture(coach.Id, commissaire.Id, ligue.Id, teamType.Id, position.Id,
            varag.Id, griff.Id, morg.Id, interdit.Id);
    }

    private static Team NouvelleEquipe(Fixture f) => new()
    {
        Nom = "Les Revenants", CoachId = f.Coach, LeagueId = f.LigueId, TeamTypeId = f.TeamTypeId, Tresorerie = 100_000
    };

    private async Task<int> CreerAsync(Fixture f, params int[] stars)
    {
        await using var db = _factory.CreateContext();
        var t = await Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, stars);
        return t.Id;
    }

    // ── Accessibilité ───────────────────────────────────────────────────────

    [Fact]
    public async Task Recrutables_StarsSansLigueEtLigueCommune_PasLesAutres()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var noms = (await Teams(db).GetStarPlayersRecrutablesAsync(f.TeamTypeId)).Select(s => s.Nom).ToList();
        Assert.Equal(["Griff", "Morg", "Varag"], noms);
    }

    // ── Création ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Creation_SaisieLibre_AvecStars_EnregistreesEtCompteesDansLaVea()
    {
        var f = await SetupAsync(libre: true);
        var sansStar = await CreerAsync(f);
        var avecStars = await CreerAsync(f, f.Varag, f.Griff);

        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        var a = (await svc.GetEquipeAsync(sansStar))!;
        var b = (await svc.GetEquipeAsync(avecStars))!;
        Assert.Equal(["Griff", "Varag"], b.StarPlayers.Select(x => x.StarPlayer.Nom).OrderBy(n => n));
        Assert.Equal(280_000 + 300_000, svc.CalculerVEA(b) - svc.CalculerVEA(a));
        // La trésorerie saisie reste celle du coach : aucune star n'est débitée.
        Assert.Equal(100_000, b.Tresorerie);
    }

    /// <summary>Les listes d'équipes alimentent aussi des VEA : la star doit y être chargée.</summary>
    [Fact]
    public async Task ListesEquipes_ChargentLesStars()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, f.Varag);
        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        Assert.NotNull((await svc.GetEquipesLigueAsync(f.LigueId)).Single().StarPlayers.Single().StarPlayer);
        Assert.NotNull((await svc.GetEquipesCoachAsync(f.Coach)).Single().StarPlayers.Single().StarPlayer);
    }

    [Fact]
    public async Task Creation_LigueClassique_AvecStar_Refusee()
    {
        var f = await SetupAsync(libre: false);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, [f.Varag]));
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_StarNonAccessibleALaRace_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, [f.Interdit]));
        Assert.Contains("Interdit", ex.Message);
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_MemeStarDeuxFois_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, [f.Varag, f.Varag]));
    }

    /// <summary>Au-delà de 2 : avertissement à l'écran, jamais un refus.</summary>
    [Fact]
    public async Task Creation_TroisStars_Acceptee()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, f.Varag, f.Griff, f.Morg);
        await using var db = _factory.CreateContext();
        Assert.Equal(3, await db.TeamStarPlayers.CountAsync(x => x.TeamId == id));
    }

    // ── Refonte en phase d'inscription ──────────────────────────────────────

    [Fact]
    public async Task ModifierEquipe_ListeNulle_GardeLesStars_ListeFournie_LesRemplace()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, f.Varag);

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)]);
        await using (var db = _factory.CreateContext())
            Assert.Equal([f.Varag], await db.TeamStarPlayers.Select(x => x.StarPlayerId).ToListAsync());

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)], [f.Griff, f.Morg]);
        await using (var db = _factory.CreateContext())
            Assert.Equal([f.Griff, f.Morg],
                await db.TeamStarPlayers.OrderBy(x => x.StarPlayerId).Select(x => x.StarPlayerId).ToListAsync());
    }

    /// <summary>
    /// L'Admin retire Griff de la ligue thématique de la race : l'équipe qui
    /// l'a déjà doit pouvoir s'enregistrer sans le perdre.
    /// </summary>
    [Fact]
    public async Task ModifierEquipe_StarDevenueInaccessible_DejaRecrutee_Conservee()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, f.Griff);
        await using (var db = _factory.CreateContext())
        {
            db.RemoveRange(db.Set<StarPlayerThemedLeague>().Where(x => x.StarPlayerId == f.Griff));
            db.Add(new StarPlayerThemedLeague
            {
                StarPlayerId = f.Griff, ThemedLeagueId = (await db.ThemedLeagues.SingleAsync(l => l.Nom == "Badlands")).Id
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)], [f.Griff, f.Varag]);

        await using var verif = _factory.CreateContext();
        Assert.Equal(2, await verif.TeamStarPlayers.CountAsync(x => x.TeamId == id));
    }

    // ── Depuis la fiche d'équipe ────────────────────────────────────────────

    private async Task<int> EquipeEnCoursAsync(Fixture f)
    {
        await using var db = _factory.CreateContext();
        return (await DataSeeder.SeedTeamAsync(db, f.LigueId, f.Coach, f.TeamTypeId)).Id;
    }

    [Fact]
    public async Task Fiche_AjouterEtRetirer_Traces_SaisonLancee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);

        await using (var db = _factory.CreateContext())
        {
            Assert.Equal(1, await Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Varag));
            Assert.Equal(2, await Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Griff));
            // Troisième : acceptée, le compte renvoyé fait avertir l'écran.
            Assert.Equal(3, await Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Morg));
        }
        await using (var db = _factory.CreateContext())
            await Libre(db).RetirerStarPlayerAsync(id, f.Coach, f.Griff);

        await using var verif = _factory.CreateContext();
        Assert.Equal([f.Varag, f.Morg],
            await verif.TeamStarPlayers.Where(x => x.TeamId == id).Select(x => x.StarPlayerId).ToListAsync());
        var traces = await verif.ModificationsLibres.Where(m => m.TeamId == id).OrderBy(m => m.Id)
            .Select(m => $"{m.Champ}:{m.Avant}>{m.Apres}").ToListAsync();
        Assert.Equal(["Star player:—>Varag", "Star player:—>Griff", "Star player:—>Morg", "Star player:Griff>—"], traces);
    }

    [Fact]
    public async Task Fiche_MemeStarDeuxFois_Refusee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Varag);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Varag));
        Assert.Contains("déjà", ex.Message);
    }

    [Fact]
    public async Task Fiche_StarNonAccessible_Refusee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Interdit));
    }

    [Fact]
    public async Task Fiche_LigueClassique_Refusee()
    {
        var f = await SetupAsync(libre: false, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).AjouterStarPlayerAsync(id, f.Coach, f.Varag));
    }

    [Fact]
    public async Task Fiche_CoachTiers_Refuse_CommissaireAccepte()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).AjouterStarPlayerAsync(id, "un-autre", f.Varag));
        Assert.Equal(1, await Libre(db, commissaire: true).AjouterStarPlayerAsync(id, f.Commissaire, f.Varag));
    }

    // ── Écran « Modifier » pendant le tournoi ───────────────────────────────

    /// <summary>
    /// Le mode tournoi ne reconstruit pas l'effectif : un joueur qui a vécu
    /// (PSP, compétence acquise, blessure) garde tout, on ne fait qu'ajouter.
    /// </summary>
    [Fact]
    public async Task ModifierEnCours_GardeLesJoueursExistants_AjouteEtReecritLeReste()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        int id, veteranId, potsId;
        await using (var db = _factory.CreateContext())
        {
            id = await EquipeEnCoursAsync(f);
            var (skill, _) = await DataSeeder.SeedSkillsAsync(db);
            var veteran = await DataSeeder.SeedPlayerAsync(db, id, f.PositionId, "Vétéran", 1);
            veteran.PointsStarPlayer = 7;
            db.TeamPlayerSkills.Add(new TeamPlayerSkill { TeamPlayerId = veteran.Id, SkillId = skill.Id, EstCompetenceDepart = false });
            db.PlayerInjuries.Add(new PlayerInjury { TeamPlayerId = veteran.Id, Type = InjuryType.BlessurePersistante });
            db.TeamStarPlayers.Add(new TeamStarPlayer { TeamId = id, StarPlayerId = f.Varag });
            var pots = new Inducement { RulesVersionId = (await db.Leagues.FindAsync(f.LigueId))!.RulesVersionId, Nom = "Pots-de-vin", Cout = 100_000 };
            db.Inducements.Add(pots);
            await db.SaveChangesAsync();
            (veteranId, potsId) = (veteran.Id, pots.Id);
        }

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierEquipeEnCoursAsync(id, f.Coach, "Les Revenants II", 123_000,
                new Dictionary<int, int>(), [f.Griff], new Dictionary<int, int> { [potsId] = 2 },
                [(f.PositionId, "Bleu", 2)]);

        await using var verif = _factory.CreateContext();
        var t = await verif.Teams.Include(x => x.Joueurs).SingleAsync(x => x.Id == id);
        Assert.Equal("Les Revenants II", t.Nom);
        Assert.Equal(123_000, t.Tresorerie);   // saisie : la recrue n'est pas débitée
        var veteranApres = t.Joueurs.Single(j => j.Id == veteranId);
        Assert.Equal(7, veteranApres.PointsStarPlayer);
        Assert.Equal(1, await verif.TeamPlayerSkills.CountAsync(s => s.TeamPlayerId == veteranId && !s.EstCompetenceDepart));
        Assert.Equal(1, await verif.PlayerInjuries.CountAsync(b => b.TeamPlayerId == veteranId));
        Assert.Contains(t.Joueurs, j => j.Nom == "Bleu" && j.Numero == 2);
        Assert.Equal([f.Griff], await verif.TeamStarPlayers.Where(x => x.TeamId == id).Select(x => x.StarPlayerId).ToListAsync());
        Assert.Equal(2, (await verif.TeamInducements.SingleAsync(x => x.TeamId == id)).Quantite);
        var champs = await verif.ModificationsLibres.Where(m => m.TeamId == id).Select(m => m.Champ).ToListAsync();
        Assert.Contains("Nom", champs);
        Assert.Contains("Joueur ajouté", champs);
        Assert.Contains("Trésorerie", champs);
    }

    /// <summary>Un refus au milieu (star interdite) annule TOUT, joueurs ajoutés compris.</summary>
    [Fact]
    public async Task ModifierEnCours_Refus_RienNEstEcrit()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        string nomAvant;
        await using (var db = _factory.CreateContext())
            nomAvant = (await db.Teams.FindAsync(id))!.Nom;

        await using (var db = _factory.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Libre(db).ModifierEquipeEnCoursAsync(id, f.Coach, "Autre nom", 0, new Dictionary<int, int>(),
                    [f.Interdit], new Dictionary<int, int>(), [(f.PositionId, "Bleu", 2)]));
            // Même contexte (scopé au circuit) : rien de la tentative ne doit y rester.
            Assert.Equal(nomAvant, (await db.Teams.FindAsync(id))!.Nom);
        }

        await using var verif = _factory.CreateContext();
        Assert.Equal(nomAvant, (await verif.Teams.FindAsync(id))!.Nom);
        Assert.Empty(await verif.TeamPlayers.Where(j => j.TeamId == id).ToListAsync());
        Assert.Empty(await verif.ModificationsLibres.ToListAsync());
    }

    [Fact]
    public async Task ModifierEnCours_PosteDUneAutreRace_Refuse()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        int autrePoste;
        await using (var db = _factory.CreateContext())
        {
            var game = await db.Games.FirstAsync();
            var autre = new TeamType { GameId = game.Id, RulesVersionId = (await db.Leagues.FindAsync(f.LigueId))!.RulesVersionId, Nom = "Orques" };
            db.TeamTypes.Add(autre);
            await db.SaveChangesAsync();
            var p = new PlayerPosition { TeamTypeId = autre.Id, Nom = "Blitzer", Cout = 80_000, QuantiteMax = 4 };
            db.PlayerPositions.Add(p);
            await db.SaveChangesAsync();
            autrePoste = p.Id;
        }
        await using var db2 = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db2).ModifierEquipeEnCoursAsync(id, f.Coach, "X", 0, new Dictionary<int, int>(), [],
                new Dictionary<int, int>(), [(autrePoste, "Intrus", 1)]));
    }

    [Fact]
    public async Task ModifierEnCours_CoachTiersEtLigueClassique_Refuses_CommissaireAccepte()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using (var db = _factory.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Libre(db).ModifierEquipeEnCoursAsync(id, "un-autre", "X", 0, new Dictionary<int, int>(), [],
                    new Dictionary<int, int>(), []));
            await Libre(db, commissaire: true).ModifierEquipeEnCoursAsync(id, f.Commissaire, "Par le commissaire", 0,
                new Dictionary<int, int>(), [], new Dictionary<int, int>(), []);
        }
        await using var verif = _factory.CreateContext();
        Assert.Equal("Par le commissaire", (await verif.Teams.FindAsync(id))!.Nom);
    }

    [Fact]
    public async Task ModifierEnCours_LigueClassique_Refuse()
    {
        var g = await SetupAsync(libre: false, LeagueStatus.EnCours);
        var id2 = await EquipeEnCoursAsync(g);
        await using var db2 = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db2).ModifierEquipeEnCoursAsync(id2, g.Coach, "X", 0, new Dictionary<int, int>(), [],
                new Dictionary<int, int>(), []));
    }

    // ── Catalogue, suppression, export, PDF ─────────────────────────────────

    [Fact]
    public async Task SupprimerStarDuCatalogue_Recrutee_Refusee_SinonAcceptee()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, f.Varag);
        await using var db = _factory.CreateContext();
        var data = new DataEditService(db, NullLogger<DataEditService>.Instance);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => data.SupprimerStarPlayerAsync(f.Varag));
        Assert.Contains("Varag", ex.Message);
        await data.SupprimerStarPlayerAsync(f.Morg);
        Assert.Equal(["Griff", "Interdit", "Varag"], await db.StarPlayers.OrderBy(s => s.Nom).Select(s => s.Nom).ToListAsync());
    }

    /// <summary>SupprimerLigueAsync passe par ExecuteDelete : aucune cascade EF.</summary>
    [Fact]
    public async Task SupprimerLigue_AvecStarsRecrutees_Reussit()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, f.Varag);
        await using (var db = _factory.CreateContext())
            await new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuth(true),
                new StaffService(db, NullLogger<StaffService>.Instance)).SupprimerLigueAsync(f.LigueId);
        await using var verif = _factory.CreateContext();
        Assert.Empty(await verif.TeamStarPlayers.ToListAsync());
        Assert.Empty(await verif.Teams.ToListAsync());
    }

    [Fact]
    public async Task ExportImportLigue_ConserveLesStarsParNom()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, f.Varag, f.Griff);

        byte[] json;
        await using (var db = _factory.CreateContext())
            json = await new LeagueExportService(db, NullLogger<LeagueExportService>.Instance,
                new StaffService(db, NullLogger<StaffService>.Instance)).ExportAsync(f.LigueId);

        int importee;
        await using (var db = _factory.CreateContext())
            importee = (await new LeagueExportService(db, NullLogger<LeagueExportService>.Instance,
                new StaffService(db, NullLogger<StaffService>.Instance)).ImportAsync(new MemoryStream(json), f.Commissaire)).Id;

        await using var verif = _factory.CreateContext();
        var noms = await verif.TeamStarPlayers.Where(x => x.Team.LeagueId == importee)
            .Select(x => x.StarPlayer.Nom).OrderBy(n => n).ToListAsync();
        Assert.Equal(["Griff", "Varag"], noms);
    }

    [Fact]
    public async Task Pdf_FeuilleEquipe_ListeLesStarsRecrutees()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, f.Varag);
        await using var db = _factory.CreateContext();
        var equipe = (await Teams(db).GetEquipeAsync(id))!;

        using var doc = UglyToad.PdfPig.PdfDocument.Open(new PdfService().GenererFeuilleEquipe(equipe, false));
        var texte = string.Join("\n", doc.GetPages().Select(p => p.Text));
        Assert.Contains("Varag", texte);
        Assert.Contains("280k", texte);
    }
}
