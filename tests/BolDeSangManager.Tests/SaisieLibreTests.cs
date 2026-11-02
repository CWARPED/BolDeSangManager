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
/// #29 — Ligue en « Saisie libre » : reprendre une équipe qui a déjà vécu.
/// Pas de budget bloquant, trésorerie et staff saisis, limites de composition
/// en avertissement, modifications tracées. Chaque règle a sa contre-épreuve
/// sur une ligue classique, qui doit garder ses refus.
/// </summary>
public class SaisieLibreTests : IDisposable
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

    private async Task<(string coach, int ligueId, int teamTypeId, int positionId, int relancesId)> SetupAsync(
        bool libre, LeagueStatus statut = LeagueStatus.Inscription)
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("comm");
        var coach = DataSeeder.CreateUser("coach");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        position.QuantiteMax = 4;
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id, statut);
        ligue.SaisieLibre = libre;
        await db.SaveChangesAsync();
        var relances = await db.LeagueStaffTypes.FirstAsync(s => s.LeagueId == ligue.Id && s.Nom == StaffService.NomRelances);
        return (coach.Id, ligue.Id, teamType.Id, position.Id, relances.Id);
    }

    /// <summary>
    /// Équipe d'exemple du ticket : 6 joueurs d'un poste limité à 4, 10 relances
    /// (plafond 8), caisse de 140 000 po — et un roster qui coûte 6 × 50 000 +
    /// 10 × 50 000 = 800 000 po que personne ne déduit.
    /// </summary>
    private static (Team equipe, List<(int, string, int)> joueurs, Dictionary<int, int> staff) EquipeVecue(
        string coach, int ligueId, int teamTypeId, int positionId, int relancesId) =>
        (new Team { Nom = "Les Revenants", CoachId = coach, LeagueId = ligueId, TeamTypeId = teamTypeId, Tresorerie = 140_000 },
         Enumerable.Range(1, 6).Select(i => (positionId, $"J{i}", i)).ToList(),
         new Dictionary<int, int> { [relancesId] = 10 });

    // ── Création d'équipe ───────────────────────────────────────────────────

    [Fact]
    public async Task Creation_SaisieLibre_TresorerieSaisie_SansBudgetNiLimites()
    {
        var (coach, ligueId, ttId, posId, relId) = await SetupAsync(libre: true);
        var (equipe, joueurs, staff) = EquipeVecue(coach, ligueId, ttId, posId, relId);

        await using (var db = _factory.CreateContext())
            await Teams(db).CreerEquipeAsync(equipe, joueurs, staff);

        await using var verif = _factory.CreateContext();
        var t = await verif.Teams.Include(x => x.Joueurs).Include(x => x.Staff).FirstAsync();
        Assert.Equal(140_000, t.Tresorerie);
        Assert.Equal(6, t.Joueurs.Count);
        Assert.Equal(10, t.NombreRelances);
        Assert.Equal(10, t.Staff.Single(s => s.LeagueStaffTypeId == relId).Quantite);
    }

    /// <summary>Contre-épreuve : la même équipe dans une ligue classique est refusée.</summary>
    [Fact]
    public async Task Creation_LigueClassique_GardeLesRefus()
    {
        var (coach, ligueId, ttId, posId, relId) = await SetupAsync(libre: false);
        var (equipe, joueurs, staff) = EquipeVecue(coach, ligueId, ttId, posId, relId);
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Teams(db).CreerEquipeAsync(equipe, joueurs, staff));

        // Limites respectées mais trésorerie postée : recalculée depuis le budget.
        var (e2, _, _) = EquipeVecue(coach, ligueId, ttId, posId, relId);
        await using (var db = _factory.CreateContext())
            await Teams(db).CreerEquipeAsync(e2, [(posId, "J1", 1)]);
        await using var verif = _factory.CreateContext();
        Assert.Equal(1_000_000 - 50_000, (await verif.Teams.SingleAsync()).Tresorerie);
    }

    [Fact]
    public async Task Creation_SaisieLibre_TresorerieNegative_Refusee()
    {
        var (coach, ligueId, ttId, posId, relId) = await SetupAsync(libre: true);
        var (equipe, joueurs, _) = EquipeVecue(coach, ligueId, ttId, posId, relId);
        equipe.Tresorerie = -1;
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Teams(db).CreerEquipeAsync(equipe, joueurs));
    }

    /// <summary>Refonte du roster en phase Inscription : même régime qu'à la création.</summary>
    [Fact]
    public async Task ModifierEquipe_SaisieLibre_GardeLaTresorerieSaisie()
    {
        var (coach, ligueId, ttId, posId, relId) = await SetupAsync(libre: true);
        var (equipe, joueurs, staff) = EquipeVecue(coach, ligueId, ttId, posId, relId);
        await using (var db = _factory.CreateContext())
            await Teams(db).CreerEquipeAsync(equipe, joueurs, staff);

        await using (var db = _factory.CreateContext())
            await Teams(db).ModifierEquipeAsync(equipe.Id, coach, "Les Revenants", 90_000, staff,
                Enumerable.Range(1, 7).Select(i => (posId, $"J{i}", i)).ToList());

        await using var verif = _factory.CreateContext();
        var t = await verif.Teams.Include(x => x.Joueurs).SingleAsync();
        Assert.Equal(90_000, t.Tresorerie);
        Assert.Equal(7, t.Joueurs.Count);
    }

    // ── Trésorerie et staff à tout moment ───────────────────────────────────

    private async Task<int> EquipeEnCoursAsync(bool libre, string? coachId = null)
    {
        var (coach, ligueId, ttId, _, _) = await SetupAsync(libre, LeagueStatus.EnCours);
        await using var db = _factory.CreateContext();
        var t = await DataSeeder.SeedTeamAsync(db, ligueId, coachId ?? coach, ttId);
        return t.Id;
    }

    [Fact]
    public async Task Ressources_SaisieLibre_SaisonLancee_EtTracees()
    {
        var teamId = await EquipeEnCoursAsync(libre: true);
        string coach; int relId, apoId;
        await using (var db = _factory.CreateContext())
        {
            var t = await db.Teams.FindAsync(teamId);
            coach = t!.CoachId;
            relId = (await db.LeagueStaffTypes.FirstAsync(s => s.LeagueId == t.LeagueId && s.Nom == StaffService.NomRelances)).Id;
            apoId = (await db.LeagueStaffTypes.FirstAsync(s => s.LeagueId == t.LeagueId && s.Nom == StaffService.NomApothicaire)).Id;
        }

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierRessourcesEquipeAsync(teamId, coach, 75_000,
                new Dictionary<int, int> { [relId] = 9, [apoId] = 1 });

        await using var verif = _factory.CreateContext();
        var e = await verif.Teams.Include(x => x.Staff).FirstAsync(x => x.Id == teamId);
        Assert.Equal(75_000, e.Tresorerie);
        Assert.Equal(9, e.NombreRelances);          // colonne miroir tenue
        Assert.True(e.Apothicaire);

        var histo = await Libre(verif).GetHistoriqueAsync(teamId);
        Assert.Contains(histo, h => h.Champ == "Trésorerie" && h.Avant == $"{500_000:N0} po" && h.Apres == $"{75_000:N0} po" && h.UserId == coach);
        Assert.Contains(histo, h => h.Champ == StaffService.NomRelances && h.Avant == "0" && h.Apres == "9");
        Assert.DoesNotContain(histo, h => h.Champ == StaffService.NomFans);   // inchangé = pas de trace
    }

    [Fact]
    public async Task Ressources_LigueClassique_Refusees()
    {
        var teamId = await EquipeEnCoursAsync(libre: false);
        await using var db = _factory.CreateContext();
        var coach = (await db.Teams.FindAsync(teamId))!.CoachId;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierRessourcesEquipeAsync(teamId, coach, 1, new Dictionary<int, int>()));
    }

    [Fact]
    public async Task Ressources_AutreCoachNon_CommissaireOui()
    {
        var teamId = await EquipeEnCoursAsync(libre: true);
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Libre(db).ModifierRessourcesEquipeAsync(teamId, "intrus", 1, new Dictionary<int, int>()));
        await using (var db = _factory.CreateContext())
            await Libre(db, commissaire: true).ModifierRessourcesEquipeAsync(teamId, "commissaire", 1, new Dictionary<int, int>());
        await using var verif = _factory.CreateContext();
        Assert.Equal(1, (await verif.Teams.FindAsync(teamId))!.Tresorerie);
    }

    /// <summary>Un refus n'écrit RIEN, même ce qui précédait la ligne fautive.</summary>
    [Fact]
    public async Task Ressources_StaffNegatif_RienNestEcrit()
    {
        var teamId = await EquipeEnCoursAsync(libre: true);
        await using var db = _factory.CreateContext();
        var t = (await db.Teams.FindAsync(teamId))!;
        var types = await db.LeagueStaffTypes.Where(s => s.LeagueId == t.LeagueId).OrderBy(s => s.Ordre).ToListAsync();
        var staff = types.ToDictionary(s => s.Id, s => 3);
        staff[types.Last().Id] = -1;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierRessourcesEquipeAsync(teamId, t.CoachId, 12_345, staff));
        Assert.False(db.ChangeTracker.HasChanges());

        await using var verif = _factory.CreateContext();
        Assert.Equal(500_000, (await verif.Teams.FindAsync(teamId))!.Tresorerie);
        Assert.Empty(await verif.ModificationsLibres.ToListAsync());
    }

    // ── Recrutement et relances sans contrôle de fonds ──────────────────────

    [Fact]
    public async Task Recrutement_SaisieLibre_SansFondsNiLimite_DebitConserve()
    {
        var (coach, ligueId, ttId, posId, _) = await SetupAsync(libre: true, LeagueStatus.EnCours);
        int teamId;
        await using (var db = _factory.CreateContext())
        {
            var t = await DataSeeder.SeedTeamAsync(db, ligueId, coach, ttId);
            t.Tresorerie = 20_000;
            for (var i = 1; i <= 4; i++) await DataSeeder.SeedPlayerAsync(db, t.Id, posId, $"J{i}", i);
            await db.SaveChangesAsync();
            teamId = t.Id;
        }

        await using (var db = _factory.CreateContext())
            await Teams(db).RecruterJoueurAsync(teamId, posId, "Cinquième", 5);

        await using var verif = _factory.CreateContext();
        Assert.Equal(5, await verif.TeamPlayers.CountAsync(p => p.TeamId == teamId));
        Assert.Equal(20_000 - 50_000, (await verif.Teams.FindAsync(teamId))!.Tresorerie);
    }

    [Fact]
    public async Task Recrutement_LigueClassique_FondsInsuffisants_Refuse()
    {
        var (coach, ligueId, ttId, posId, _) = await SetupAsync(libre: false, LeagueStatus.EnCours);
        await using var db = _factory.CreateContext();
        var t = await DataSeeder.SeedTeamAsync(db, ligueId, coach, ttId);
        t.Tresorerie = 20_000;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Teams(db).RecruterJoueurAsync(t.Id, posId, "X", 1));
    }

    [Fact]
    public async Task RelancesPhaseDeRepos_SaisieLibre_SansFondsNiPlafond()
    {
        var (coach, ligueId, ttId, _, _) = await SetupAsync(libre: true, LeagueStatus.PhaseDeRepos);
        int teamId;
        await using (var db = _factory.CreateContext())
        {
            var t = await DataSeeder.SeedTeamAsync(db, ligueId, coach, ttId);
            t.Tresorerie = 0;
            t.NombreRelances = 8;
            await db.SaveChangesAsync();
            teamId = t.Id;
        }
        await using (var db = _factory.CreateContext())
        {
            var ligues = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuth(false),
                new StaffService(db, NullLogger<StaffService>.Instance));
            await ligues.ValiderApresMatchReposAsync(ligueId, teamId, [], [], 1, Teams(db));
        }
        await using var verif = _factory.CreateContext();
        var e = (await verif.Teams.FindAsync(teamId))!;
        Assert.Equal(9, e.NombreRelances);
        Assert.Equal(-100_000, e.Tresorerie);
    }

    // ── Lot 2 : progression d'un joueur à la main ───────────────────────────

    private record Joueur(int Id, string Coach, int Blocage, int Meurtre);

    /// <summary>Lineman (50 000 po, accès principal Générale), ligue lancée.</summary>
    private async Task<Joueur> JoueurLibreAsync(bool libre = true)
    {
        var (coach, ligueId, ttId, posId, _) = await SetupAsync(libre, LeagueStatus.EnCours);
        await using var db = _factory.CreateContext();
        var (normal, elite) = await DataSeeder.SeedSkillsAsync(db);
        db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
        {
            PlayerPositionId = posId, SkillCategoryDefId = normal.SkillCategoryDefId, EstPrincipale = true
        });
        var t = await DataSeeder.SeedTeamAsync(db, ligueId, coach, ttId);
        var j = await DataSeeder.SeedPlayerAsync(db, t.Id, posId, "Grishnak", 5);
        await db.SaveChangesAsync();
        return new Joueur(j.Id, coach, normal.Id, elite.Id);
    }

    private static ProgressionLibre Vide(int psp = 0) => new(psp, false, [], [], []);

    private async Task<TeamPlayer> RelireAsync(int id)
    {
        await using var db = _factory.CreateContext();
        return await db.TeamPlayers
            .Include(j => j.PlayerPosition)
            .Include(j => j.Improvements).ThenInclude(i => i.Skill)
            .Include(j => j.Competences)
            .Include(j => j.Blessures)
            .AsNoTracking().FirstAsync(j => j.Id == id);
    }

    [Fact]
    public async Task Progression_Ajouts_CreentLesMemesLignesQuunApresMatch()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, new ProgressionLibre(
                Psp: 4, RateProchainMatch: true,
                CompetencesAcquises: [j.Blocage, j.Meurtre],
                Caracteristiques: [AffectedStat.Agilite, AffectedStat.Agilite],
                Blessures: [new(null, InjuryType.Sequelle, AffectedStat.Armure), new(null, InjuryType.BlessurePersistante)]));

        var r = await RelireAsync(j.Id);
        Assert.Equal(4, r.PointsStarPlayer);
        Assert.True(r.ManqueSuivantMatch);
        Assert.Equal(ImprovementType.SelectionPrimaire, r.Improvements.Single(i => i.SkillId == j.Blocage).Type);
        Assert.Equal(ImprovementType.SelectionSecondaire, r.Improvements.Single(i => i.SkillId == j.Meurtre).Type);
        Assert.All(r.Improvements, i => Assert.Equal(0, i.PspDepensee));
        Assert.Equal(2, r.Competences.Count(c => !c.EstCompetenceDepart));
        Assert.Equal(2, r.ModAgilite);
        Assert.Equal(-1, r.ModArmure);
        Assert.Equal(2, r.Blessures.Count(b => b.MatchId == null));
        Assert.Equal(1, BlessureHelpers.NombreBP(r));

        // La valeur suit (décision 5) : poste + hausses du barème.
        var bareme = BaremeAmelioration.DeVersion(null);
        Assert.Equal(50_000
                + bareme.Hausse(ImprovementType.SelectionPrimaire)
                + bareme.Hausse(ImprovementType.SelectionSecondaire, null, estElite: true)
                + 2 * bareme.Hausse(ImprovementType.AmeliorationCarac, AffectedStat.Agilite),
            ValeurJoueurCalculator.Calculer(r, bareme));

        await using var verif = _factory.CreateContext();
        var histo = await Libre(verif).GetHistoriqueAsync(r.TeamId);
        Assert.Equal(8, histo.Count);   // PSP, RPM, 2 compétences, 2 carac, 2 blessures
        Assert.All(histo, h => Assert.Equal("#5 Grishnak", h.Sujet));
        Assert.Contains(histo, h => h.Champ == "Compétence" && h.Apres == "Blocage");
    }

    [Fact]
    public async Task Progression_Retraits_InversentLesEffets()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, new ProgressionLibre(3, false,
                [j.Blocage], [AffectedStat.Force], [new(null, InjuryType.Sequelle, AffectedStat.Mouvement)]));

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, Vide(psp: 3));

        var r = await RelireAsync(j.Id);
        Assert.Empty(r.Improvements);
        Assert.DoesNotContain(r.Competences, c => !c.EstCompetenceDepart);
        Assert.Empty(r.Blessures);
        Assert.Equal(0, r.ModForce);
        Assert.Equal(0, r.ModMouvement);
        Assert.Equal(3, r.PointsStarPlayer);
    }

    /// <summary>Deux +1 AG puis un seul voulu : un seul est retiré.</summary>
    [Fact]
    public async Task Progression_Caracteristiques_ComparaisonParNombre()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach,
                new ProgressionLibre(0, false, [], [AffectedStat.Agilite, AffectedStat.Agilite], []));
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach,
                new ProgressionLibre(0, false, [], [AffectedStat.Agilite], []));
        var r = await RelireAsync(j.Id);
        Assert.Equal(1, r.ModAgilite);
        Assert.Single(r.Improvements);
    }

    /// <summary>Joueur sans nom (« #5 ») : le sujet n'est pas « #5 #5 ».</summary>
    [Fact]
    public async Task Historique_JoueurSansNom_SujetSansDoublon()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
        {
            (await db.TeamPlayers.FindAsync(j.Id))!.Nom = "#5";
            await db.SaveChangesAsync();
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, Vide(psp: 1));
        }
        await using var verif = _factory.CreateContext();
        Assert.Equal("#5", Assert.Single(await verif.ModificationsLibres.ToListAsync()).Sujet);
    }

    [Fact]
    public async Task Progression_Mort_PuisRetrait()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
        {
            (await db.TeamPlayers.FindAsync(j.Id))!.EstCapitaine = true;
            await db.SaveChangesAsync();
        }
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach,
                new ProgressionLibre(0, false, [], [], [new(null, InjuryType.Mort)]));
        var mort = await RelireAsync(j.Id);
        Assert.True(mort.EstMort);
        Assert.False(mort.EstCapitaine);

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, Vide());
        Assert.False((await RelireAsync(j.Id)).EstMort);
    }

    /// <summary>Les « Amoché » des feuilles ne sont pas dans la liste éditée : jamais retirés.</summary>
    [Fact]
    public async Task Progression_NeTouchePasAuxAmoches()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
        {
            db.PlayerInjuries.Add(new PlayerInjury { TeamPlayerId = j.Id, Type = InjuryType.ManqueSuivant });
            await db.SaveChangesAsync();
        }
        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierProgressionAsync(j.Id, j.Coach, Vide());
        Assert.Single((await RelireAsync(j.Id)).Blessures);
    }

    [Theory]
    [InlineData("sequelle_sans_stat")]
    [InlineData("competence_du_poste")]
    [InlineData("competence_en_double")]
    [InlineData("psp_negatifs")]
    [InlineData("amoche_ajoute")]
    [InlineData("deux_morts")]
    public async Task Progression_Refus_RienNestEcrit(string cas)
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
        {
            db.TeamPlayerSkills.Add(new TeamPlayerSkill { TeamPlayerId = j.Id, SkillId = j.Blocage, EstCompetenceDepart = true });
            await db.SaveChangesAsync();
        }
        var p = cas switch
        {
            "sequelle_sans_stat"   => new ProgressionLibre(7, false, [], [], [new(null, InjuryType.Sequelle)]),
            "competence_du_poste"  => new ProgressionLibre(7, false, [j.Blocage], [], []),
            "competence_en_double" => new ProgressionLibre(7, false, [j.Meurtre, j.Meurtre], [], []),
            "psp_negatifs"         => new ProgressionLibre(-1, false, [], [], []),
            "amoche_ajoute"        => new ProgressionLibre(7, false, [], [], [new(null, InjuryType.ManqueSuivant)]),
            _                      => new ProgressionLibre(7, false, [], [], [new(null, InjuryType.Mort), new(null, InjuryType.Mort)]),
        };
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).ModifierProgressionAsync(j.Id, j.Coach, p));
        var r = await RelireAsync(j.Id);
        Assert.Equal(0, r.PointsStarPlayer);
        Assert.Empty(r.Improvements);
        Assert.Empty(r.Blessures);
    }

    [Fact]
    public async Task Progression_LigueClassique_Refusee()
    {
        var j = await JoueurLibreAsync(libre: false);
        await using var db = _factory.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Libre(db).ModifierProgressionAsync(j.Id, j.Coach, Vide(psp: 5)));
    }

    /// <summary>✓ de la ligne : nom et progression en une transaction, tout ou rien.</summary>
    [Fact]
    public async Task ModifierJoueur_ToutOuRien()
    {
        var j = await JoueurLibreAsync();
        await using (var db = _factory.CreateContext())
        {
            var svc = Libre(db);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ModifierJoueurAsync(j.Id, j.Coach,
                "Nouveau nom", [], null, new ProgressionLibre(5, false, [], [], [new(null, InjuryType.Sequelle)])));
            Assert.False(db.ChangeTracker.HasChanges());
            // Le contexte vit avec le circuit : l'entité suivie ne doit pas
            // garder le nom refusé, sinon l'écran l'afficherait.
            Assert.Equal("Grishnak", (await db.TeamPlayers.FindAsync(j.Id))!.Nom);
        }
        Assert.Equal("Grishnak", (await RelireAsync(j.Id)).Nom);

        await using (var db = _factory.CreateContext())
            await Libre(db).ModifierJoueurAsync(j.Id, j.Coach, "Nouveau nom", ["Nain"], null, Vide(psp: 5));
        var r = await RelireAsync(j.Id);
        Assert.Equal("Nouveau nom", r.Nom);
        Assert.Equal("Nain", r.Haines);
        Assert.Equal(5, r.PointsStarPlayer);
    }

    // ── Lot 3 : historique lisible, réservé au commissaire ──────────────────

    [Fact]
    public async Task Historique_CommissaireSeulement_PseudoJamaisEmail()
    {
        var teamId = await EquipeEnCoursAsync(libre: true);
        string coach;
        await using (var db = _factory.CreateContext())
        {
            coach = (await db.Teams.FindAsync(teamId))!.CoachId;
            (await db.Users.FindAsync(coach))!.PseudoCoach = "Gorbag";
            await db.SaveChangesAsync();
            await Libre(db).ModifierRessourcesEquipeAsync(teamId, coach, 1, new Dictionary<int, int>());
            db.ModificationsLibres.Add(new ModificationLibre { TeamId = teamId, UserId = "disparu", Sujet = "Équipe", Champ = "Trésorerie" });
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Libre(db).GetHistoriqueAfficheAsync(teamId, coach));

        await using var verif = _factory.CreateContext();
        var lignes = await Libre(verif, commissaire: true).GetHistoriqueAfficheAsync(teamId, "commissaire");
        Assert.Contains(lignes, l => l.auteur == "Gorbag" && l.modif.Champ == "Trésorerie");
        Assert.Contains(lignes, l => l.auteur == "compte supprimé");
        Assert.DoesNotContain(lignes, l => l.auteur.Contains('@'));
    }

    // ── Le mode ne change pas en cours de ligue ─────────────────────────────

    [Fact]
    public async Task ModeFigeDesLaPremiereEquipe()
    {
        var (coach, ligueId, ttId, _, _) = await SetupAsync(libre: false, LeagueStatus.Creation);
        League copie;
        await using (var db = _factory.CreateContext())
            copie = await db.Leagues.AsNoTracking().FirstAsync(l => l.Id == ligueId);
        LeagueService Svc(ApplicationDbContext db) => new(db, NullLogger<LeagueService>.Instance,
            new StubAuth(true), new StaffService(db, NullLogger<StaffService>.Instance));

        copie.SaisieLibre = true;
        await using (var db = _factory.CreateContext())
            await Svc(db).ModifierLigueAsync(ligueId, copie, "commissaire");
        await using (var db = _factory.CreateContext())
        {
            Assert.True((await db.Leagues.FindAsync(ligueId))!.SaisieLibre);
            await DataSeeder.SeedTeamAsync(db, ligueId, coach, ttId);
        }

        copie.SaisieLibre = false;
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Svc(db).ModifierLigueAsync(ligueId, copie, "commissaire"));
        await using var verif = _factory.CreateContext();
        Assert.True((await verif.Leagues.FindAsync(ligueId))!.SaisieLibre);
    }
}
