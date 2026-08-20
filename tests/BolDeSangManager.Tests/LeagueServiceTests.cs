using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class LeagueServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private LeagueService CreateService(ApplicationDbContext db) =>
        new(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());

    // ─── Setup helpers ────────────────────────────────────────────────────────

    private async Task<(ApplicationUser commissaire, Game game, RulesVersion rv)> SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("commissaire");
        db.Users.Add(commissaire);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        return (commissaire, game, rv);
    }

    // ─── CreerLigueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreerLigue_SetStatutCreation()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var ligue = new League { Nom = "Test", GameId = game.Id, RulesVersionId = rv.Id };
        var result = await svc.CreerLigueAsync(ligue, commissaire.Id);

        Assert.Equal(LeagueStatus.Creation, result.Statut);
    }

    [Fact]
    public async Task CreerLigue_SetCommissaireId()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var ligue = new League { Nom = "Test", GameId = game.Id, RulesVersionId = rv.Id };
        var result = await svc.CreerLigueAsync(ligue, commissaire.Id);

        Assert.Equal(commissaire.Id, result.CommissaireId);
    }

    [Fact]
    public async Task CreerLigue_EstPersistee_EnBase()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var ligue = new League { Nom = "Ligue Persistée", GameId = game.Id, RulesVersionId = rv.Id };
        await svc.CreerLigueAsync(ligue, commissaire.Id);

        await using var db2 = _factory.CreateContext();
        var found = await db2.Leagues.FirstOrDefaultAsync(l => l.Nom == "Ligue Persistée");
        Assert.NotNull(found);
    }

    // ─── DemarrerInscriptionsAsync ────────────────────────────────────────────

    [Fact]
    public async Task DemarrerInscriptions_PasseAuStatutInscription()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        await svc.DemarrerInscriptionsAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var updated = await db2.Leagues.FindAsync(ligue.Id);
        Assert.Equal(LeagueStatus.Inscription, updated!.Statut);
    }

    [Fact]
    public async Task DemarrerInscriptions_LigueInexistante_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DemarrerInscriptionsAsync(99999));
    }

    // ─── LancerSaisonAsync + génération round-robin ───────────────────────────

    [Fact]
    public async Task LancerSaison_MoinsDeDeuxEquipes_ThrowsException()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach = DataSeeder.CreateUser("coach1");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Équipe A");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.LancerSaisonAsync(ligue.Id));
    }

    [Fact]
    public async Task LancerSaison_Avec2Equipes_GenereUnMatch()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach1 = DataSeeder.CreateUser("c1");
        var coach2 = DataSeeder.CreateUser("c2");
        db.Users.AddRange(coach1, coach2);
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "Équipe A");
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "Équipe B");

        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var nbMatchs = await db2.Matches.CountAsync(m => divIds.Contains(m.DivisionId!.Value));
        Assert.Equal(1, nbMatchs);
    }

    [Fact]
    public async Task LancerSaison_Avec4Equipes_Genere6Matchs()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        for (int i = 1; i <= 4; i++)
        {
            var c = DataSeeder.CreateUser($"rr4_{i}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var nbMatchs = await db2.Matches.CountAsync(m => divIds.Contains(m.DivisionId!.Value));
        Assert.Equal(6, nbMatchs);  // 4*(4-1)/2 = 6
    }

    // ─── Nombre impair d'équipes ──────────────────────────────────────────────

    [Fact]
    public async Task LancerSaison_Avec3Equipes_Genere3Matchs()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        for (int i = 1; i <= 3; i++)
        {
            var c = DataSeeder.CreateUser($"rr3_{i}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var matchs = await db2.Matches.Where(m => divIds.Contains(m.DivisionId!.Value)).ToListAsync();

        // 3*(3-1)/2 = 3 matchs
        Assert.Equal(3, matchs.Count);
    }

    [Fact]
    public async Task LancerSaison_Avec3Equipes_ChaqueEquipeJoueContreLesDeuxAutres()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        var ids = new List<int>();
        for (int i = 1; i <= 3; i++)
        {
            var c = DataSeeder.CreateUser($"rr3p_{i}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            var t = await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
            ids.Add(t.Id);
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var matchs = await db2.Matches.Where(m => divIds.Contains(m.DivisionId!.Value)).ToListAsync();

        // Toutes les paires (A,B), (A,C), (B,C) doivent apparaître exactement une fois
        var paires = matchs
            .Select(m => (Math.Min(m.EquipeDomicileId, m.EquipeExterieurId),
                          Math.Max(m.EquipeDomicileId, m.EquipeExterieurId)))
            .ToList();
        Assert.Equal(3, paires.Distinct().Count()); // 3 paires distinctes
        Assert.Equal(3, paires.Count);              // sans doublons

        // Chaque équipe joue exactement 2 fois
        foreach (var id in ids)
        {
            var nbMatchsEquipe = matchs.Count(m => m.EquipeDomicileId == id || m.EquipeExterieurId == id);
            Assert.Equal(2, nbMatchsEquipe);
        }
    }

    [Fact]
    public async Task LancerSaison_Avec3Equipes_Genere3Rondes()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        for (int i = 1; i <= 3; i++)
        {
            var c = DataSeeder.CreateUser($"rr3r_{i}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var rondes = await db2.Matches
            .Where(m => divIds.Contains(m.DivisionId!.Value))
            .Select(m => m.Ronde)
            .Distinct()
            .ToListAsync();

        // 3 rondes avec 1 match chacune (1 équipe a un bye par ronde)
        Assert.Equal(3, rondes.Count);
        Assert.Equal(1, rondes.Min()); // rondes numérotées à partir de 1
    }

    [Fact]
    public async Task LancerSaison_Avec5Equipes_Genere10Matchs()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        for (int i = 1; i <= 5; i++)
        {
            var c = DataSeeder.CreateUser($"rr5_{i}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var divIds = await db2.Divisions.Where(d => d.LeagueId == ligue.Id).Select(d => d.Id).ToListAsync();
        var matchs = await db2.Matches.Where(m => divIds.Contains(m.DivisionId!.Value)).ToListAsync();

        // 5*(5-1)/2 = 10 matchs, aucun doublon
        Assert.Equal(10, matchs.Count);
        var paires = matchs
            .Select(m => (Math.Min(m.EquipeDomicileId, m.EquipeExterieurId),
                          Math.Max(m.EquipeDomicileId, m.EquipeExterieurId)))
            .ToList();
        Assert.Equal(10, paires.Distinct().Count());
    }

    [Fact]
    public async Task LancerSaison_PasseStatutEnCours()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach1 = DataSeeder.CreateUser("lc1");
        var coach2 = DataSeeder.CreateUser("lc2");
        db.Users.AddRange(coach1, coach2);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "A");
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "B");
        var svc = CreateService(db);

        await svc.LancerSaisonAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        var updated = await db2.Leagues.FindAsync(ligue.Id);
        Assert.Equal(LeagueStatus.EnCours, updated!.Statut);
    }

    // ─── GetVersionsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetVersionsAsync_RetourneToutesLesVersions_TrieesParOrdre()
    {
        await using var db = _factory.CreateContext();
        var game = new Game { Nom = "TestGame", Type = GameType.BloodBowl };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        db.RulesVersions.AddRange(
            new RulesVersion { GameId = game.Id, Nom = "V3", EstActive = true, Ordre = 3 },
            new RulesVersion { GameId = game.Id, Nom = "V1", EstActive = false, Ordre = 1 },
            new RulesVersion { GameId = game.Id, Nom = "V2", EstActive = false, Ordre = 2 }
        );
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var versions = await svc.GetVersionsAsync(game.Id);

        Assert.Equal(3, versions.Count);
        Assert.Equal("V1", versions[0].Nom);
        Assert.Equal("V2", versions[1].Nom);
        Assert.Equal("V3", versions[2].Nom);
    }

    [Fact]
    public async Task GetVersionsAsync_IncludeVersionsInactives()
    {
        await using var db = _factory.CreateContext();
        var game = new Game { Nom = "GameV", Type = GameType.BloodBowl };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        db.RulesVersions.AddRange(
            new RulesVersion { GameId = game.Id, Nom = "Ancienne", EstActive = false, Ordre = 1 },
            new RulesVersion { GameId = game.Id, Nom = "Actuelle", EstActive = true, Ordre = 2 }
        );
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var versions = await svc.GetVersionsAsync(game.Id);

        Assert.Equal(2, versions.Count);
        Assert.Contains(versions, v => v.EstActive == false);
    }

    // ─── EstCommissaireAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task EstCommissaire_RetourneVraiPourLeCommissaire()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        // Stub configured to grant access for the commissaire on this specific league
        var stub = new StubAuthorizationService(peutGerer: (commissaire.Id, ligue.Id));
        var svc = new LeagueService(db, NullLogger<LeagueService>.Instance, stub);

        var result = await svc.EstCommissaireAsync(ligue.Id, commissaire.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task EstCommissaire_RetourneFauxPourAutreUtilisateur()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        // Default stub returns false for all — "autre-id" has no access
        var svc = CreateService(db);

        var result = await svc.EstCommissaireAsync(ligue.Id, "autre-id");

        Assert.False(result);
    }

    // ─── LancerPhaseDeReposAsync ──────────────────────────────────────────────

    [Fact]
    public async Task LancerPhaseDeRepos_ChangeStatutEtResetRPM()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c");
        var coach = DataSeeder.CreateUser("co");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        ligue.Statut = LeagueStatus.EnCours;
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
        var j1 = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "j1", Numero = 1, ManqueSuivantMatch = true };
        var j2 = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "j2", Numero = 2, ManqueSuivantMatch = true };
        db.TeamPlayers.AddRange(j1, j2);
        await db.SaveChangesAsync();

        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());
        await service.LancerPhaseDeReposAsync(ligue.Id);

        var maj = await db.Leagues.FindAsync(ligue.Id);
        var joueurs = await db.TeamPlayers.Where(j => j.TeamId == equipe.Id).ToListAsync();

        Assert.Equal(LeagueStatus.PhaseDeRepos, maj!.Statut);
        Assert.All(joueurs, j => Assert.False(j.ManqueSuivantMatch));
    }

    // ─── ValiderApresMatchReposAsync ─────────────────────────────────────────

    [Fact]
    public async Task ValiderApresMatchRepos_CreeValidationEtAppliqueAchats()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c");
        var coach = DataSeeder.CreateUser("co");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        ligue.Statut = LeagueStatus.PhaseDeRepos;
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
        equipe.Tresorerie = 200_000;
        equipe.NombreRelances = 0;
        var joueur = new TeamPlayer
        {
            TeamId = equipe.Id,
            PlayerPositionId = position.Id,
            Nom = "J1", Numero = 1, ValeurActuelle = 50_000, PointsStarPlayer = 6
        };
        db.TeamPlayers.Add(joueur);
        var vId = (await db.RulesVersions.FirstAsync()).Id;
        var catId = await DataSeeder.GetOrCreateCategorieAsync(db, vId);
        var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = vId };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();

        var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());

        await service.ValiderApresMatchReposAsync(
            ligueId: ligue.Id,
            teamId: equipe.Id,
            competences: [(joueur.Id, skill.Id, estPrincipale: true, xpDepensee: 6)],
            nouveauxJoueurs: [],
            nouvellesRelances: 1,
            teamService: teamService);

        var validation = await db.PhaseDeReposValidations.FirstOrDefaultAsync(v => v.LeagueId == ligue.Id && v.TeamId == equipe.Id);
        Assert.NotNull(validation);

        var equipeMaj = await db.Teams.FindAsync(equipe.Id);
        Assert.Equal(1, equipeMaj!.NombreRelances);
        Assert.Equal(200_000 - 100_000, equipeMaj.Tresorerie);

        var jMaj = await db.TeamPlayers.Include(j => j.Improvements).FirstAsync(j => j.Id == joueur.Id);
        Assert.Single(jMaj.Improvements);
    }

    [Fact]
    public async Task ValiderApresMatchRepos_DejaValide_LeveException()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c2");
        var coach = DataSeeder.CreateUser("co2");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        ligue.Statut = LeagueStatus.PhaseDeRepos;
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test2");

        db.PhaseDeReposValidations.Add(new PhaseDeReposValidation { LeagueId = ligue.Id, TeamId = equipe.Id });
        await db.SaveChangesAsync();

        var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValiderApresMatchReposAsync(ligue.Id, equipe.Id, [], [], 0, teamService));
    }

    [Fact]
    public async Task ValiderApresMatchRepos_LigueHorsPhase_LeveException()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c3");
        var coach = DataSeeder.CreateUser("co3");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        ligue.Statut = LeagueStatus.EnCours; // pas en repos
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test3");
        await db.SaveChangesAsync();

        var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValiderApresMatchReposAsync(ligue.Id, equipe.Id, [], [], 0, teamService));
    }

    // ─── GetTop*Async ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopJoueursParPsp_RetourneJoueursDeLaLigueOrdonnesParPSP()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c");
        var coach = DataSeeder.CreateUser("co");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "T");

        db.TeamPlayers.AddRange(
            new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Bas", Numero = 1, PointsStarPlayer = 3 },
            new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Haut", Numero = 2, PointsStarPlayer = 25 },
            new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Moyen", Numero = 3, PointsStarPlayer = 10 }
        );
        await db.SaveChangesAsync();

        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());
        var top = await service.GetTopJoueursParPspAsync(ligue.Id, limit: 2);

        Assert.Equal(2, top.Count);
        Assert.Equal("Haut", top[0].Nom);
        Assert.Equal("Moyen", top[1].Nom);
    }

    // ─── AttribuerAwardAsync + GetAwardsAsync ─────────────────────────────────

    [Fact]
    public async Task AttribuerAward_CreeLeagueAward()
    {
        await using var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var commissaire = DataSeeder.CreateUser("c");
        var coach = DataSeeder.CreateUser("co");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "T");
        var joueur = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Star", Numero = 1, PointsStarPlayer = 50 };
        db.TeamPlayers.Add(joueur);
        await db.SaveChangesAsync();

        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());
        await service.AttribuerAwardAsync(ligue.Id, AwardType.MVP, teamPlayerId: joueur.Id);

        var awards = await service.GetAwardsAsync(ligue.Id);
        Assert.Single(awards);
        Assert.Equal(AwardType.MVP, awards[0].Type);
        Assert.Equal(joueur.Id, awards[0].TeamPlayerId);
    }

    // ─── SupprimerLigueAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SupprimerLigue_EffaceToutesLesDonnees()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach1 = DataSeeder.CreateUser("sd1");
        var coach2 = DataSeeder.CreateUser("sd2");
        db.Users.AddRange(coach1, coach2);
        await db.SaveChangesAsync();

        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        var t1 = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "S1");
        var t2 = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "S2");
        await DataSeeder.SeedPlayerAsync(db, t1.Id, position.Id, "Joueur X");

        var svc = CreateService(db);
        await svc.SupprimerLigueAsync(ligue.Id);

        await using var db2 = _factory.CreateContext();
        Assert.False(await db2.Leagues.AnyAsync(l => l.Id == ligue.Id));
        Assert.False(await db2.Teams.AnyAsync(t => t.LeagueId == ligue.Id));
        Assert.False(await db2.TeamPlayers.AnyAsync(j => j.TeamId == t1.Id));
    }

    // ─── StubAuthorizationService ─────────────────────────────────────────────

    // ─── Format Libre : calendrier composé par le commissaire ─────────────────

    /// <summary>Crée une ligue au format Libre avec N équipes, et la lance.</summary>
    private async Task<(int ligueId, List<int> equipeIds)> SetupLigueLibreAsync(
        int nbEquipes, LeagueFormat format = LeagueFormat.Libre, bool lancer = true)
    {
        await using var db = _factory.CreateContext();

        // commissaire unique : ce helper peut être appelé plusieurs fois dans
        // un même test (AspNetUsers.NormalizedUserName est unique).
        var commissaire = DataSeeder.CreateUser($"comlibre_{Guid.NewGuid():N}");
        db.Users.Add(commissaire);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);

        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id,
            format: format);

        var equipeIds = new List<int>();
        for (int i = 1; i <= nbEquipes; i++)
        {
            var c = DataSeeder.CreateUser($"libre_{Guid.NewGuid():N}");
            db.Users.Add(c);
            await db.SaveChangesAsync();
            var t = await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"Équipe {i}");
            equipeIds.Add(t.Id);
        }

        if (lancer)
        {
            var svc = CreateService(db);
            await svc.LancerSaisonAsync(ligue.Id);
        }
        return (ligue.Id, equipeIds);
    }

    private async Task<int> CompterMatchsAsync(int ligueId)
    {
        await using var db = _factory.CreateContext();
        var divIds = await db.Divisions.Where(d => d.LeagueId == ligueId).Select(d => d.Id).ToListAsync();
        return await db.Matches.CountAsync(m => divIds.Contains(m.DivisionId!.Value));
    }

    [Fact]
    public async Task LancerSaison_FormatLibre_NeGenereAucunMatch()
    {
        var (ligueId, _) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        Assert.Equal(LeagueStatus.EnCours, (await db.Leagues.FindAsync(ligueId))!.Statut);
        Assert.Equal(0, await CompterMatchsAsync(ligueId));
    }

    [Fact]
    public async Task LancerSaison_FormatLibre_CreeQuandMemeLaDivision()
    {
        // le commissaire a besoin d'une division pour y rattacher ses matchs
        var (ligueId, _) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        Assert.Single(await db.Divisions.Where(d => d.LeagueId == ligueId).ToListAsync());
    }

    [Fact]
    public async Task DefinirRonde_CreeLesRencontresDemandees()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1]), (ids[2], ids[3])]);

        await using var db2 = _factory.CreateContext();
        var matchs = await db2.Matches
            .Where(m => m.Division!.LeagueId == ligueId && m.Ronde == 1)
            .ToListAsync();
        Assert.Equal(2, matchs.Count);
        Assert.All(matchs, m => Assert.Equal(MatchStatus.Programme, m.Statut));
        Assert.All(matchs, m => Assert.False(m.EstPlayoff));
    }

    [Fact]
    public async Task DefinirRonde_RefuseSurUneLigueNonLibre()
    {
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        var c1 = DataSeeder.CreateUser("nl1"); var c2 = DataSeeder.CreateUser("nl2");
        db.Users.AddRange(c1, c2); await db.SaveChangesAsync();
        var t1 = await DataSeeder.SeedTeamAsync(db, ligue.Id, c1.Id, teamType.Id, "A");
        var t2 = await DataSeeder.SeedTeamAsync(db, ligue.Id, c2.Id, teamType.Id, "B");

        var svc = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DefinirRondeAsync(ligue.Id, 1, [(t1.Id, t2.Id)]));
    }

    [Fact]
    public async Task DefinirRonde_RefuseUneEquipeDeuxFoisDansLaMemeRonde()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1]), (ids[0], ids[2])]));
        Assert.Contains("deux fois", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DefinirRonde_RefuseUneEquipeContreEllememe()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[0])]));
    }

    [Fact]
    public async Task DefinirRonde_RefuseUneEquipeDuneAutreLigue()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);
        var (_, autresIds) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DefinirRondeAsync(ligueId, 1, [(ids[0], autresIds[0])]));
    }

    [Fact]
    public async Task DefinirRonde_RefuseUnNumeroDeRondeInvalide()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DefinirRondeAsync(ligueId, 0, [(ids[0], ids[1])]));
    }

    [Fact]
    public async Task DefinirRonde_AutoriseLeRepos_UneEquipeNonCitee()
    {
        // 3 équipes : une seule rencontre, la troisième se repose
        var (ligueId, ids) = await SetupLigueLibreAsync(3);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);

        Assert.Equal(1, await CompterMatchsAsync(ligueId));
    }

    [Fact]
    public async Task DefinirRonde_AutoriseLaMemePaireDansUneAutreRonde()
    {
        // organisation entièrement libre : les matchs aller-retour sont permis
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[1], ids[0])]);   // retour

        Assert.Equal(2, await CompterMatchsAsync(ligueId));
    }

    [Fact]
    public async Task DefinirRonde_RedefinirUneRondeNonJouee_RemplaceLesMatchs()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[2], ids[3]), (ids[0], ids[1])]);

        Assert.Equal(2, await CompterMatchsAsync(ligueId));
    }

    [Fact]
    public async Task DefinirRonde_RefuseDeModifierUneRondeDejaJouee()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        }

        // le match est joué
        await using (var db = _factory.CreateContext())
        {
            var m = await db.Matches.FirstAsync(x => x.Ronde == 1);
            m.Statut = MatchStatus.Termine;
            m.ScoreDomicile = 2; m.ScoreExterieur = 1;
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.DefinirRondeAsync(ligueId, 1, [(ids[2], ids[3])]));
            Assert.Contains("déjà", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SupprimerRonde_RetireLesMatchsNonJoues()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[2], ids[3])]);

        await svc.SupprimerRondeAsync(ligueId, 2);

        Assert.Equal(1, await CompterMatchsAsync(ligueId));
    }

    [Fact]
    public async Task SupprimerRonde_RefuseSiUnMatchEstJoue()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        }
        await using (var db = _factory.CreateContext())
        {
            var m = await db.Matches.FirstAsync();
            m.Statut = MatchStatus.Termine;
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.SupprimerRondeAsync(ligueId, 1));
        }
    }

    [Fact]
    public async Task LancerSaison_RoundRobin_GenereToujoursLeCalendrier()
    {
        // non-régression : les formats existants ne changent pas
        var (commissaire, game, rv) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id,
            format: LeagueFormat.RoundRobin);
        for (int i = 1; i <= 4; i++)
        {
            var c = DataSeeder.CreateUser($"nonreg_{i}");
            db.Users.Add(c); await db.SaveChangesAsync();
            await DataSeeder.SeedTeamAsync(db, ligue.Id, c.Id, teamType.Id, $"T{i}");
        }
        var svc = CreateService(db);
        await svc.LancerSaisonAsync(ligue.Id);

        Assert.Equal(6, await CompterMatchsAsync(ligue.Id));
    }

    [Fact]
    public void DisplayHelpers_ConnaitLesFormatsLibres()
    {
        Assert.Equal("Libre", DisplayHelpers.LeagueFormatLabel(LeagueFormat.Libre));
        Assert.Equal("Libre + Play-offs", DisplayHelpers.LeagueFormatLabel(LeagueFormat.LibreAvecPlayoffs));

        Assert.True(DisplayHelpers.EstFormatLibre(LeagueFormat.Libre));
        Assert.True(DisplayHelpers.EstFormatLibre(LeagueFormat.LibreAvecPlayoffs));
        Assert.False(DisplayHelpers.EstFormatLibre(LeagueFormat.RoundRobin));

        Assert.True(DisplayHelpers.AvecPlayoffs(LeagueFormat.LibreAvecPlayoffs));
        Assert.True(DisplayHelpers.AvecPlayoffs(LeagueFormat.RoundRobinAvecPlayoffs));
        Assert.False(DisplayHelpers.AvecPlayoffs(LeagueFormat.Libre));
    }

    [Fact]
    public void LeagueFormat_ValeursPersistees_NeDoiventPasBouger()
    {
        // ces entiers sont stockés en base : les réordonner casserait les ligues
        Assert.Equal(0, (int)LeagueFormat.RoundRobin);
        Assert.Equal(1, (int)LeagueFormat.RoundRobinAvecPlayoffs);
        Assert.Equal(2, (int)LeagueFormat.Libre);
        Assert.Equal(3, (int)LeagueFormat.LibreAvecPlayoffs);
    }

    [Fact]
    public async Task SupprimerRonde_NeSupprimeQueLaRondeVisee()
    {
        // Bug signalé : supprimer la dernière ronde effaçait tout le calendrier.
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1]), (ids[2], ids[3])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[0], ids[2]), (ids[1], ids[3])]);
        await svc.DefinirRondeAsync(ligueId, 3, [(ids[0], ids[3]), (ids[1], ids[2])]);
        Assert.Equal(6, await CompterMatchsAsync(ligueId));

        await svc.SupprimerRondeAsync(ligueId, 3);   // la dernière

        // les rondes 1 et 2 doivent rester intactes
        await using var db2 = _factory.CreateContext();
        var restants = await db2.Matches
            .Where(m => m.Division!.LeagueId == ligueId)
            .Select(m => m.Ronde)
            .ToListAsync();
        Assert.Equal(4, restants.Count);
        Assert.Equal([1, 1, 2, 2], restants.OrderBy(r => r).ToList());
    }

    [Fact]
    public async Task DefinirEcheanceRonde_EnregistreEtModifieLaDate()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);

        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        await svc.DefinirEcheanceRondeAsync(ligueId, 1, date);
        Assert.Equal(date.Date, (await svc.GetEcheancesRondesAsync(ligueId))[1].Date);

        var nouvelle = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);
        await svc.DefinirEcheanceRondeAsync(ligueId, 1, nouvelle);
        var echeances = await svc.GetEcheancesRondesAsync(ligueId);
        Assert.Single(echeances);                 // pas de doublon
        Assert.Equal(nouvelle.Date, echeances[1].Date);
    }

    [Fact]
    public async Task DefinirEcheanceRonde_AvecNull_RetireLecheance()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirEcheanceRondeAsync(ligueId, 1, new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));

        await svc.DefinirEcheanceRondeAsync(ligueId, 1, null);

        Assert.Empty(await svc.GetEcheancesRondesAsync(ligueId));
    }

    [Fact]
    public async Task SupprimerRonde_RetireAussiSonEcheance()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[2], ids[3])]);
        await svc.DefinirEcheanceRondeAsync(ligueId, 1, new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));
        await svc.DefinirEcheanceRondeAsync(ligueId, 2, new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc));

        await svc.SupprimerRondeAsync(ligueId, 2);

        var echeances = await svc.GetEcheancesRondesAsync(ligueId);
        Assert.Single(echeances);
        Assert.True(echeances.ContainsKey(1));     // celle de la ronde 1 est conservée
    }

    [Fact]
    public async Task ProposerAppariements_VarieLesRencontresEntreLesRondes()
    {
        // Le reproche initial : « Compléter » proposait toujours les mêmes matchs.
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var r1 = await svc.ProposerAppariementsAsync(ligueId, 1, ids);
        await svc.DefinirRondeAsync(ligueId, 1, r1);

        var r2 = await svc.ProposerAppariementsAsync(ligueId, 2, ids);
        await svc.DefinirRondeAsync(ligueId, 2, r2);

        var r3 = await svc.ProposerAppariementsAsync(ligueId, 3, ids);

        static string Cle((int a, int b) p) => p.a < p.b ? $"{p.a}-{p.b}" : $"{p.b}-{p.a}";
        var paires = r1.Concat(r2).Concat(r3).Select(p => Cle((p.domicileId, p.exterieurId))).ToList();

        // 4 équipes → 3 rondes de 2 matchs couvrent les 6 affrontements possibles,
        // chacun exactement une fois : aucune répétition.
        Assert.Equal(6, paires.Count);
        Assert.Equal(6, paires.Distinct().Count());
    }

    [Fact]
    public async Task ProposerAppariements_AlterneDomicileEtExterieur()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var r1 = (await svc.ProposerAppariementsAsync(ligueId, 1, ids)).Single();
        await svc.DefinirRondeAsync(ligueId, 1, [r1]);

        var r2 = (await svc.ProposerAppariementsAsync(ligueId, 2, ids)).Single();

        // match retour : celui qui recevait se déplace
        Assert.Equal(r1.domicileId, r2.exterieurId);
        Assert.Equal(r1.exterieurId, r2.domicileId);
    }

    [Fact]
    public async Task ProposerAppariements_LaisseUneEquipeAuReposSiNombreImpair()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(3);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        var propositions = await svc.ProposerAppariementsAsync(ligueId, 1, ids);

        Assert.Single(propositions);   // 3 équipes → 1 match, 1 au repos
    }

    [Fact]
    public async Task ProposerAppariements_TientCompteDesRondesNonEnregistrees()
    {
        // Cas réel : le commissaire enchaîne « Ajouter une ronde » + « Compléter »
        // sans enregistrer entre les deux. Sans dejaComposees, les rondes
        // successives proposaient exactement les mêmes rencontres.
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var r1 = await svc.ProposerAppariementsAsync(ligueId, 1, ids);   // rien en base
        var r2 = await svc.ProposerAppariementsAsync(ligueId, 2, ids, r1);

        static string Cle((int a, int b) p) => p.a < p.b ? $"{p.a}-{p.b}" : $"{p.b}-{p.a}";
        var c1 = r1.Select(p => Cle((p.domicileId, p.exterieurId))).ToHashSet();
        var c2 = r2.Select(p => Cle((p.domicileId, p.exterieurId))).ToHashSet();

        Assert.Empty(c1.Intersect(c2));   // aucune rencontre en commun
    }

    [Fact]
    public async Task RenumeroterRondes_ComblLesTrousApresSuppression()
    {
        // Bug signalé : après suppression, il restait « Ronde 1 » puis « Ronde 4 ».
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[2], ids[3])]);
        await svc.DefinirRondeAsync(ligueId, 3, [(ids[0], ids[2])]);
        await svc.DefinirEcheanceRondeAsync(ligueId, 3, new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));

        await svc.SupprimerRondeAsync(ligueId, 2);      // trou : 1, 3
        Assert.True(await svc.RenumeroterRondesAsync(ligueId));

        await using var db2 = _factory.CreateContext();
        var rondes = await db2.Matches
            .Where(m => m.Division!.LeagueId == ligueId)
            .Select(m => m.Ronde).Distinct().OrderBy(r => r).ToListAsync();
        Assert.Equal([1, 2], rondes);

        // l'échéance suit sa ronde (3 → 2)
        var echeances = await svc.GetEcheancesRondesAsync(ligueId);
        Assert.True(echeances.ContainsKey(2));
    }

    [Fact]
    public async Task RenumeroterRondes_NeToucheRienSiDejaCompact()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);
        await svc.DefinirRondeAsync(ligueId, 2, [(ids[2], ids[3])]);

        Assert.True(await svc.RenumeroterRondesAsync(ligueId));

        await using var db2 = _factory.CreateContext();
        var rondes = await db2.Matches
            .Where(m => m.Division!.LeagueId == ligueId)
            .Select(m => m.Ronde).Distinct().OrderBy(r => r).ToListAsync();
        Assert.Equal([1, 2], rondes);
    }

    [Fact]
    public async Task RenumeroterRondes_RefuseDeDecalerUneRondeCommencee()
    {
        var (ligueId, ids) = await SetupLigueLibreAsync(4);

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            await svc.DefinirRondeAsync(ligueId, 2, [(ids[0], ids[1])]);   // pas de ronde 1
        }
        await using (var db = _factory.CreateContext())
        {
            var m = await db.Matches.FirstAsync();
            m.Statut = MatchStatus.Termine;
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            Assert.False(await svc.RenumeroterRondesAsync(ligueId));   // 2 → 1 interdit
            Assert.Equal(2, (await db.Matches.FirstAsync()).Ronde);
        }
    }

    [Fact]
    public async Task DefinirEcheanceRonde_ConserveLaDateSaisieQuelQueSoitLeFuseau()
    {
        // Piège : une date stockée à minuit bascule d'un jour selon le fuseau
        // et l'heure d'été. Elle est donc normalisée à midi UTC.
        var (ligueId, ids) = await SetupLigueLibreAsync(2);

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.DefinirRondeAsync(ligueId, 1, [(ids[0], ids[1])]);

        // saisie « 25 août » en heure locale non spécifiée, comme le MudDatePicker
        await svc.DefinirEcheanceRondeAsync(ligueId, 1, new DateTime(2026, 8, 25));

        var stockee = (await svc.GetEcheancesRondesAsync(ligueId))[1];
        Assert.Equal(new DateTime(2026, 8, 25), stockee.Date);   // toujours le 25
        Assert.Equal(12, stockee.Hour);                          // midi : marge de 12h
    }

    private class StubAuthorizationService(
        (string userId, int ligueId)? peutGerer = null) : IAuthorizationService
    {
        public Task<bool> EstAdminAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstGrandCommissaireAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId) => Task.FromResult(false);
        public Task<bool> PeutGererLigueAsync(string userId, int ligueId) =>
            Task.FromResult(peutGerer.HasValue && peutGerer.Value.userId == userId && peutGerer.Value.ligueId == ligueId);
        public Task<bool> PeutEditerDonneesAsync(string userId) => Task.FromResult(false);
        public Task<bool> PeutGererSettingsAsync(string userId) => Task.FromResult(false);
    }
}
