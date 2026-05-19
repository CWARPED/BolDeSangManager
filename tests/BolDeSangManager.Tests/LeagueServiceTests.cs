using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
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
        var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, RulesVersionId = (await db.RulesVersions.FirstAsync()).Id };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();

        var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
        var service = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuthorizationService());

        await service.ValiderApresMatchReposAsync(
            ligueId: ligue.Id,
            teamId: equipe.Id,
            competences: [(joueur.Id, skill.Id, estPrincipale: true)],
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
