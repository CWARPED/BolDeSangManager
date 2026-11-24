using System.Text;
using System.Text.Json.Nodes;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Ligue en « Saisie libre » : les stars passent par un coup de pouce « Star
/// player » (une star du catalogue par ligne, accessible à la race), et un coup
/// de pouce à prix variable reçoit le prix saisi par le coach. Prix compté
/// dans la VEA, star imprimée avec ses caractéristiques, rien débité.
/// Chaque règle a sa contre-épreuve.
/// </summary>
public class StarPlayersCoupDePouceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static TeamService Teams(ApplicationDbContext db) => new(db, NullLogger<TeamService>.Instance);

    private static SaisieLibreService Libre(ApplicationDbContext db, bool commissaire = false) =>
        new(db, new StubAuth(commissaire), Teams(db),
            new JoueurEditionService(db, new StubAuth(commissaire), NullLogger<JoueurEditionService>.Instance),
            NullLogger<SaisieLibreService>.Instance);

    private static LeagueExportService Export(ApplicationDbContext db) =>
        new(db, NullLogger<LeagueExportService>.Instance, new StaffService(db, NullLogger<StaffService>.Instance));

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
        int Varag, int Griff, int Morg, int Interdit, int Sp1, int Sp2, int Pots, int Cuistot);

    /// <summary>
    /// Race « Humains » rattachée à la ligue thématique « Vieux Monde ».
    /// Varag (sans ligue = accessible à tous), Griff (Vieux Monde), Morg (sans
    /// ligue) sont accessibles ; Interdit (« Badlands » seulement) ne l'est pas.
    /// Coups de pouce : Star Player 1 / 2 (star, prix variable), Pots-de-vin
    /// (100 000, fixe), Chef Cuistot (prix variable).
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

        var sp1 = new Inducement { RulesVersionId = rv.Id, Nom = "Star Player 1", Cout = 0, QuantiteMax = 1, EstStarPlayer = true, Ordre = 17 };
        var sp2 = new Inducement { RulesVersionId = rv.Id, Nom = "Star Player 2", Cout = 0, QuantiteMax = 1, EstStarPlayer = true, Ordre = 18 };
        var pots = new Inducement { RulesVersionId = rv.Id, Nom = "Pots-de-vin", Cout = 100_000, QuantiteMax = 3, Ordre = 7 };
        var cuistot = new Inducement { RulesVersionId = rv.Id, Nom = "Chef Cuistot", Cout = 0, QuantiteMax = 1, Ordre = 13 };
        db.Inducements.AddRange(sp1, sp2, pots, cuistot);
        await db.SaveChangesAsync();
        db.Add(new StarPlayerThemedLeague { StarPlayerId = griff.Id, ThemedLeagueId = vieuxMonde.Id });
        db.Add(new StarPlayerThemedLeague { StarPlayerId = interdit.Id, ThemedLeagueId = badlands.Id });
        await db.SaveChangesAsync();

        return new Fixture(coach.Id, commissaire.Id, ligue.Id, teamType.Id, position.Id,
            varag.Id, griff.Id, morg.Id, interdit.Id, sp1.Id, sp2.Id, pots.Id, cuistot.Id);
    }

    private static Team NouvelleEquipe(Fixture f) => new()
    {
        Nom = "Les Revenants", CoachId = f.Coach, LeagueId = f.LigueId, TeamTypeId = f.TeamTypeId, Tresorerie = 100_000
    };

    private static Task<Team> Creer(ApplicationDbContext db, Fixture f, Dictionary<int, CoupDePouceSaisi>? cdp) =>
        Teams(db).CreerEquipeAsync(NouvelleEquipe(f), [(f.PositionId, "J1", 1)], null, cdp);

    private async Task<int> CreerAsync(Fixture f, Dictionary<int, CoupDePouceSaisi>? cdp = null)
    {
        await using var db = _factory.CreateContext();
        return (await Creer(db, f, cdp)).Id;
    }

    private static CoupDePouceSaisi Star(int starId, int? prix = null) => new(1, prix, starId);

    private async Task<int> VeaDeAsync(int teamId)
    {
        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        return svc.CalculerVEA((await svc.GetEquipeAsync(teamId))!);
    }

    // ── Accessibilité ───────────────────────────────────────────────────────

    [Fact]
    public async Task Proposees_StarsSansLigueEtLigueCommune_PasLesAutres()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var noms = (await Teams(db).GetStarPlayersRecrutablesAsync(f.TeamTypeId)).Select(s => s.Nom).ToList();
        Assert.Equal(["Griff", "Morg", "Varag"], noms);
    }

    // ── Création : stars ────────────────────────────────────────────────────

    [Fact]
    public async Task Creation_StarSurCoupDePouce_PrixDeLaStarParDefaut_CompteDansLaVea_RienDebite()
    {
        var f = await SetupAsync(libre: true);
        var sans = await CreerAsync(f);
        var avec = await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag), [f.Sp2] = Star(f.Griff) });

        Assert.Equal(280_000 + 300_000, await VeaDeAsync(avec) - await VeaDeAsync(sans));
        await using var db = _factory.CreateContext();
        var lignes = await db.TeamInducements.Where(x => x.TeamId == avec).OrderBy(x => x.InducementId).ToListAsync();
        Assert.Equal([(f.Varag, 280_000), (f.Griff, 300_000)], lignes.Select(l => (l.StarPlayerId!.Value, l.PrixSaisi!.Value)));
        Assert.Equal(100_000, (await db.Teams.FindAsync(avec))!.Tresorerie);
    }

    [Fact]
    public async Task Creation_PrixSaisi_RemplaceLePrixDeLaStar()
    {
        var f = await SetupAsync(libre: true);
        var sans = await CreerAsync(f);
        var avec = await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag, prix: 150_000) });
        Assert.Equal(150_000, await VeaDeAsync(avec) - await VeaDeAsync(sans));
    }

    /// <summary>Une ligne star = une star : la quantité est ramenée à 1.</summary>
    [Fact]
    public async Task Creation_StarChoisie_QuantiteRameneeAUn()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Sp1] = new CoupDePouceSaisi(2, null, f.Varag) });
        await using var db = _factory.CreateContext();
        Assert.Equal(1, (await db.TeamInducements.SingleAsync(x => x.TeamId == id)).Quantite);
    }

    /// <summary>Tout ce que somme la VEA doit être chargé par les TROIS chargeurs d'équipe.</summary>
    [Fact]
    public async Task ChargeursEquipe_ChargentLaStarDuCoupDePouce()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag) });
        await using var db = _factory.CreateContext();
        var svc = Teams(db);
        Assert.NotNull((await svc.GetEquipeAsync(id))!.CoupsDePouce.Single().StarPlayer);
        Assert.NotNull((await svc.GetEquipesLigueAsync(f.LigueId)).Single().CoupsDePouce.Single().StarPlayer);
        Assert.NotNull((await svc.GetEquipesCoachAsync(f.Coach)).Single().CoupsDePouce.Single().StarPlayer);
    }

    [Fact]
    public async Task Creation_LigueClassique_Refusee()
    {
        var f = await SetupAsync(libre: false);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Creer(db, f, new() { [f.Sp1] = Star(f.Varag) }));
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_StarNonAccessibleALaRace_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Creer(db, f, new() { [f.Sp1] = Star(f.Interdit) }));
        Assert.Contains("Interdit", ex.Message);
        Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Creation_MemeStarSurDeuxLignes_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Creer(db, f, new() { [f.Sp1] = Star(f.Varag), [f.Sp2] = Star(f.Varag) }));
    }

    [Fact]
    public async Task Creation_StarSurCoupDePouceOrdinaire_Refusee()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Creer(db, f, new() { [f.Cuistot] = Star(f.Varag) }));
        Assert.Contains("Chef Cuistot", ex.Message);
    }

    // ── Création : prix saisi ───────────────────────────────────────────────

    /// <summary>Le cas de merlinc : le Chef Cuistot à 100 000 po pour des Halflings.</summary>
    [Fact]
    public async Task Creation_PrixVariableSaisi_CompteDansLaVea_FoisLaQuantite()
    {
        var f = await SetupAsync(libre: true);
        var sans = await CreerAsync(f);
        var cuistot = await CreerAsync(f, new() { [f.Cuistot] = new CoupDePouceSaisi(1, 100_000) });
        var deux = await CreerAsync(f, new() { [f.Cuistot] = new CoupDePouceSaisi(2, 100_000) });
        Assert.Equal(100_000, await VeaDeAsync(cuistot) - await VeaDeAsync(sans));
        Assert.Equal(200_000, await VeaDeAsync(deux) - await VeaDeAsync(sans));
    }

    [Fact]
    public async Task Creation_PrixSaisiSurPrixFixe_Refuse()
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Creer(db, f, new() { [f.Pots] = new CoupDePouceSaisi(1, 50_000) }));
        Assert.Contains("fixé", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(CoupsDePouceEquipe.PrixMaxSaisi + 1)]
    public async Task Creation_PrixHorsBornes_Refuse(int prix)
    {
        var f = await SetupAsync(libre: true);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Creer(db, f, new() { [f.Cuistot] = new CoupDePouceSaisi(1, prix) }));
    }

    [Fact]
    public void PrixUnitaire_SansPrixSaisi_PrixDuCatalogue()
    {
        var pots = new Inducement { Nom = "Pots-de-vin", Cout = 100_000 };
        Assert.Equal(100_000, CoupsDePouceEquipe.PrixUnitaire(new TeamInducement { Inducement = pots }));
        Assert.Equal(250_000, CoupsDePouceEquipe.Total(new TeamInducement
            { Inducement = new Inducement { Cout = 0 }, PrixSaisi = 50_000, Quantite = 5 }));
    }

    // ── Refonte en phase d'inscription ──────────────────────────────────────

    [Fact]
    public async Task ModifierEquipe_Nul_GardeLaStar_EtatFourni_LeRemplace()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag) });

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)]);
        await using (var db = _factory.CreateContext())
            Assert.Equal(f.Varag, (await db.TeamInducements.SingleAsync()).StarPlayerId);

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(id, f.Coach, "Les Revenants", 0, new Dictionary<int, int>(),
                [(f.PositionId, "J1", 1)], new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Morg, 200_000) });
        await using (var db = _factory.CreateContext())
        {
            var l = await db.TeamInducements.SingleAsync();
            Assert.Equal((f.Morg, 200_000), (l.StarPlayerId!.Value, l.PrixSaisi!.Value));
        }
    }

    /// <summary>
    /// L'Admin retire Griff de la ligue thématique de la race : l'équipe qui
    /// l'a déjà doit pouvoir s'enregistrer sans le perdre.
    /// </summary>
    [Fact]
    public async Task ModifierEquipe_StarDevenueInaccessible_DejaDetenue_Conservee()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Sp1] = Star(f.Griff) });
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
                [(f.PositionId, "J1", 1)],
                new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Griff), [f.Sp2] = Star(f.Varag) });

        await using var verif = _factory.CreateContext();
        Assert.Equal(2, await verif.TeamInducements.CountAsync(x => x.TeamId == id && x.StarPlayerId != null));
    }

    // ── Depuis la fiche d'équipe ────────────────────────────────────────────

    private async Task<int> EquipeEnCoursAsync(Fixture f)
    {
        await using var db = _factory.CreateContext();
        return (await DataSeeder.SeedTeamAsync(db, f.LigueId, f.Coach, f.TeamTypeId)).Id;
    }

    [Fact]
    public async Task Fiche_ChoisirPuisChangerDeStar_Trace_SaisonLancee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Varag) });
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Morg) });

        await using var verif = _factory.CreateContext();
        Assert.Equal(f.Morg, (await verif.TeamInducements.SingleAsync(x => x.TeamId == id)).StarPlayerId);
        var traces = await verif.ModificationsLibres.Where(m => m.TeamId == id).OrderBy(m => m.Id).ToListAsync();
        Assert.Equal(2, traces.Count);
        Assert.All(traces, t => Assert.Equal("Coup de pouce : Star Player 1", t.Champ));
        Assert.Equal("0", traces[0].Avant);
        Assert.Contains("Varag", traces[0].Apres);
        Assert.Contains("Varag", traces[1].Avant);
        Assert.Contains("Morg", traces[1].Apres);
    }

    [Fact]
    public async Task Fiche_StarNonAccessible_Refusee()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierCoupsDePouceAsync(id, f.Coach, new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Interdit) }));
    }

    [Fact]
    public async Task Fiche_CoachTiers_Refuse_CommissaireAccepte()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using var db = _factory.CreateContext();
        var voulu = new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Varag) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).ModifierCoupsDePouceAsync(id, "un-autre", voulu));
        await Libre(db, commissaire: true).ModifierCoupsDePouceAsync(id, f.Commissaire, voulu);
        Assert.Equal(f.Varag, (await db.TeamInducements.SingleAsync(x => x.TeamId == id)).StarPlayerId);
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
        int id, veteranId;
        await using (var db = _factory.CreateContext())
        {
            id = await EquipeEnCoursAsync(f);
            var (skill, _) = await DataSeeder.SeedSkillsAsync(db);
            var veteran = await DataSeeder.SeedPlayerAsync(db, id, f.PositionId, "Vétéran", 1);
            veteran.PointsStarPlayer = 7;
            db.TeamPlayerSkills.Add(new TeamPlayerSkill { TeamPlayerId = veteran.Id, SkillId = skill.Id, EstCompetenceDepart = false });
            db.PlayerInjuries.Add(new PlayerInjury { TeamPlayerId = veteran.Id, Type = InjuryType.BlessurePersistante });
            await db.SaveChangesAsync();
            veteranId = veteran.Id;
        }

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierEquipeEnCoursAsync(id, f.Coach, "Les Revenants II", 123_000,
                new Dictionary<int, int>(),
                new Dictionary<int, CoupDePouceSaisi> { [f.Pots] = new(2), [f.Sp1] = Star(f.Griff) },
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
        Assert.Equal(f.Griff, (await verif.TeamInducements.SingleAsync(x => x.TeamId == id && x.InducementId == f.Sp1)).StarPlayerId);
        Assert.Equal(2, (await verif.TeamInducements.SingleAsync(x => x.TeamId == id && x.InducementId == f.Pots)).Quantite);
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
                    new Dictionary<int, CoupDePouceSaisi> { [f.Sp1] = Star(f.Interdit) }, [(f.PositionId, "Bleu", 2)]));
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
            Libre(db2).ModifierEquipeEnCoursAsync(id, f.Coach, "X", 0, new Dictionary<int, int>(),
                new Dictionary<int, CoupDePouceSaisi>(), [(autrePoste, "Intrus", 1)]));
    }

    [Fact]
    public async Task ModifierEnCours_CoachTiers_Refuse_CommissaireAccepte()
    {
        var f = await SetupAsync(libre: true, LeagueStatus.EnCours);
        var id = await EquipeEnCoursAsync(f);
        await using (var db = _factory.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Libre(db).ModifierEquipeEnCoursAsync(id, "un-autre", "X", 0, new Dictionary<int, int>(),
                    new Dictionary<int, CoupDePouceSaisi>(), []));
            await Libre(db, commissaire: true).ModifierEquipeEnCoursAsync(id, f.Commissaire, "Par le commissaire", 0,
                new Dictionary<int, int>(), new Dictionary<int, CoupDePouceSaisi>(), []);
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
            Libre(db2).ModifierEquipeEnCoursAsync(id2, g.Coach, "X", 0, new Dictionary<int, int>(),
                new Dictionary<int, CoupDePouceSaisi>(), []));
    }

    // ── Catalogue, suppression, export, PDF ─────────────────────────────────

    [Fact]
    public async Task SupprimerStarDuCatalogue_Detenue_Refusee_SinonAcceptee()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag) });
        await using var db = _factory.CreateContext();
        var data = new DataEditService(db, NullLogger<DataEditService>.Instance);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => data.SupprimerStarPlayerAsync(f.Varag));
        Assert.Contains("Varag", ex.Message);
        await data.SupprimerStarPlayerAsync(f.Morg);
        Assert.Equal(["Griff", "Interdit", "Varag"], await db.StarPlayers.OrderBy(s => s.Nom).Select(s => s.Nom).ToListAsync());
    }

    /// <summary>SupprimerLigueAsync passe par ExecuteDelete : aucune cascade EF.</summary>
    [Fact]
    public async Task SupprimerLigue_AvecStarSurCoupDePouce_Reussit()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag) });
        await using (var db = _factory.CreateContext())
            await new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuth(true),
                new StaffService(db, NullLogger<StaffService>.Instance)).SupprimerLigueAsync(f.LigueId);
        await using var verif = _factory.CreateContext();
        Assert.Empty(await verif.TeamInducements.ToListAsync());
        Assert.Empty(await verif.Teams.ToListAsync());
    }

    private async Task<int> ExporterImporterAsync(Fixture f, Func<string, string>? retoucher = null)
    {
        byte[] json;
        await using (var db = _factory.CreateContext())
            json = await Export(db).ExportAsync(f.LigueId);
        if (retoucher is not null)
            json = Encoding.UTF8.GetBytes(retoucher(Encoding.UTF8.GetString(json)));
        await using (var db = _factory.CreateContext())
            return (await Export(db).ImportAsync(new MemoryStream(json), f.Commissaire)).Id;
    }

    [Fact]
    public async Task ExportImportLigue_ConserveStarEtPrixSaisi()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag, 150_000), [f.Cuistot] = new(1, 100_000), [f.Pots] = new(2) });

        var importee = await ExporterImporterAsync(f);

        await using var verif = _factory.CreateContext();
        var lignes = await verif.TeamInducements.Where(x => x.Team.LeagueId == importee)
            .OrderBy(x => x.Inducement.Nom)
            .Select(x => x.Inducement.Nom + "×" + x.Quantite + ":" + x.PrixSaisi + ":" + (x.StarPlayer == null ? "" : x.StarPlayer.Nom))
            .ToListAsync();
        Assert.Equal(["Chef Cuistot×1:100000:", "Pots-de-vin×2::", "Star Player 1×1:150000:Varag"], lignes);
    }

    /// <summary>Un fichier exporté avant cette version n'a que nom → quantité.</summary>
    [Fact]
    public async Task ImportAncienFichier_SansDetail_RetombeSurLesQuantites()
    {
        var f = await SetupAsync(libre: true);
        await CreerAsync(f, new() { [f.Pots] = new(2) });

        var importee = await ExporterImporterAsync(f, texte =>
        {
            var racine = JsonNode.Parse(texte)!;
            var retirees = 0;
            foreach (var e in racine["equipes"]!.AsArray())
                if (e!.AsObject().Remove("coupsDePouceDetail")) retirees++;
            Assert.Equal(1, retirees);   // le test simule bien un ancien fichier
            return racine.ToJsonString();
        });

        await using var verif = _factory.CreateContext();
        var l = await verif.TeamInducements.SingleAsync(x => x.Team.LeagueId == importee);
        Assert.Equal((f.Pots, 2, (int?)null), (l.InducementId, l.Quantite, l.PrixSaisi));
    }

    [Fact]
    public async Task Pdf_FeuilleEquipe_StarAvecSesCaracteristiques_EtPrixSaisis()
    {
        var f = await SetupAsync(libre: true);
        var id = await CreerAsync(f, new() { [f.Sp1] = Star(f.Varag, 150_000), [f.Cuistot] = new(1, 100_000) });
        await using var db = _factory.CreateContext();
        var equipe = (await Teams(db).GetEquipeAsync(id))!;

        using var doc = UglyToad.PdfPig.PdfDocument.Open(new PdfService().GenererFeuilleEquipe(equipe, false));
        var texte = string.Join("\n", doc.GetPages().Select(p => p.Text));
        Assert.Contains("Varag", texte);
        Assert.Contains("Star player", texte);
        Assert.Contains("10+", texte);       // armure de la star : la ligne porte bien ses caractéristiques
        Assert.Contains("150k", texte);      // prix saisi, pas les 280k du catalogue
        Assert.DoesNotContain("280k", texte);
        Assert.Contains("Chef Cuistot (100k)", texte);
    }
}
