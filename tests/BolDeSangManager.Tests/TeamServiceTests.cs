using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class TeamServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private TeamService CreateService(ApplicationDbContext db) =>
        new(db, NullLogger<TeamService>.Instance);

    // ─── Setup ────────────────────────────────────────────────────────────────

    private async Task<(ApplicationUser coach, TeamType teamType, PlayerPosition position, League ligue)>
        SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("comm");
        var coach = DataSeeder.CreateUser("coach");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        return (coach, teamType, position, ligue);
    }

    // ─── CreerEquipeAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreerEquipe_RecruteJoueursInitiaux()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "Les Broyeurs",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id
        };
        var joueurs = new List<(int, string, int)>
        {
            (position.Id, "Gromag", 1),
            (position.Id, "Skulkar", 2)
        };

        await svc.CreerEquipeAsync(equipe, joueurs);

        await using var db2 = _factory.CreateContext();
        var count = await db2.TeamPlayers.CountAsync(j => j.TeamId == equipe.Id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CreerEquipe_JoueursOntBonnValeurActuelle()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "Test VEA",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id
        };

        await svc.CreerEquipeAsync(equipe, [(position.Id, "Marc", 1)]);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FirstAsync(j => j.TeamId == equipe.Id);
        Assert.Equal(position.Cout, joueur.ValeurActuelle);
    }

    [Fact]
    public async Task CreerEquipe_AssigneCompetencesDeDepart()
    {
        // Crée un skill et l'attache au poste
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("cskill");
        var coach = DataSeeder.CreateUser("cskcoach");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var (skill, _) = await DataSeeder.SeedSkillsAsync(db);
        db.PlayerPositionSkills.Add(new PlayerPositionSkill
            { PlayerPositionId = position.Id, SkillId = skill.Id });
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);

        var svc = CreateService(db);
        var equipe = new Team
        {
            Nom = "TestComp",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id
        };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "Bob", 1)]);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FirstAsync(j => j.TeamId == equipe.Id);
        var hasSkill = await db2.TeamPlayerSkills
            .AnyAsync(s => s.TeamPlayerId == joueur.Id && s.SkillId == skill.Id && s.EstCompetenceDepart);
        Assert.True(hasSkill);
    }

    // ─── RecruterJoueurAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RecruterJoueur_DebiteLeTresor()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var team = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id);
        var tresoInitiale = team.Tresorerie;
        var svc = CreateService(db);

        await svc.RecruterJoueurAsync(team.Id, position.Id, "Nouveau", 99);

        await using var db2 = _factory.CreateContext();
        var updated = await db2.Teams.FindAsync(team.Id);
        Assert.Equal(tresoInitiale - position.Cout, updated!.Tresorerie);
    }

    [Fact]
    public async Task RecruterJoueur_FondsInsuffisants_ThrowsException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var team = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id);
        // Vider la trésorerie
        team.Tresorerie = 0;
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RecruterJoueurAsync(team.Id, position.Id, "Pauvre", 10));
    }

    // ─── CalculerVEA ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculerVEA_SommeJoueursRelancesFans()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "VEA Test",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id,
            NombreRelances = 2,    // 2 × 50 000 = 100 000
            FansDevoues = 3,       // 3 × 10 000 = 30 000
            NombreCoachsAssistants = 1,  // 10 000
            NombreCheerleaders = 0,
            Apothicaire = false
        };
        equipe.Joueurs.Add(new TeamPlayer
        {
            PlayerPositionId = position.Id,
            Nom = "J1", Numero = 1,
            ValeurActuelle = 80_000,
            RecruteLe = DateTime.UtcNow
        });

        var vea = svc.CalculerVEA(equipe);

        // 80k joueur + 100k relances + 30k fans + 10k coach = 220 000
        Assert.Equal(220_000, vea);
    }

    [Fact]
    public async Task CalculerVEA_ExclutJoueursMortEtRetraite()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "VEA Mort",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id,
            NombreRelances = 0,
            FansDevoues = 0
        };
        equipe.Joueurs.Add(new TeamPlayer
            { PlayerPositionId = position.Id, Nom = "Vivant", Numero = 1, ValeurActuelle = 60_000, RecruteLe = DateTime.UtcNow });
        equipe.Joueurs.Add(new TeamPlayer
            { PlayerPositionId = position.Id, Nom = "Mort", Numero = 2, ValeurActuelle = 60_000, EstMort = true, RecruteLe = DateTime.UtcNow });
        equipe.Joueurs.Add(new TeamPlayer
            { PlayerPositionId = position.Id, Nom = "Retraité", Numero = 3, ValeurActuelle = 60_000, EstRetraite = true, RecruteLe = DateTime.UtcNow });

        var vea = svc.CalculerVEA(equipe);

        Assert.Equal(60_000, vea);  // Seul le joueur vivant compte
    }

    [Fact]
    public async Task CalculerVEA_AvecApothicaire_Ajoute50k()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "VEA Apo",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id,
            NombreRelances = 0,
            FansDevoues = 0,
            Apothicaire = true
        };

        var vea = svc.CalculerVEA(equipe);
        Assert.Equal(50_000, vea);
    }

    // ─── AppliquerAmeliorationAsync ───────────────────────────────────────────

    [Fact]
    public async Task AppliquerAmelioration_PalierNonAtteint_LeveException()
    {
        await using var db = _factory.CreateContext();
        var (game, _) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach = DataSeeder.CreateUser("p1");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, (await db.RulesVersions.FirstAsync()).Id, coach.Id);
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
        var joueur = new TeamPlayer
        {
            TeamId = equipe.Id,
            PlayerPositionId = position.Id,
            Nom = "Test", Numero = 1, ValeurActuelle = 50_000,
            PointsStarPlayer = 3 // Moins que le seuil de 6
        };
        db.TeamPlayers.Add(joueur);
        await db.SaveChangesAsync();

        var vId = (await db.RulesVersions.FirstAsync()).Id;
        var catId = await DataSeeder.GetOrCreateCategorieAsync(db, vId);
        var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = vId };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();

        var service = new TeamService(db, NullLogger<TeamService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AppliquerAmeliorationAsync(joueur.Id, ImprovementType.SelectionPrimaire, skillId: skill.Id));
    }

    [Fact]
    public async Task AppliquerAmelioration_PalierAtteint_CreeImprovementEtAugmenteValeur()
    {
        await using var db = _factory.CreateContext();
        var (game, _) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var coach = DataSeeder.CreateUser("p2");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, (await db.RulesVersions.FirstAsync()).Id, coach.Id);
        var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
        var joueur = new TeamPlayer
        {
            TeamId = equipe.Id,
            PlayerPositionId = position.Id,
            Nom = "Test", Numero = 1, ValeurActuelle = 50_000,
            PointsStarPlayer = 6
        };
        db.TeamPlayers.Add(joueur);
        await db.SaveChangesAsync();

        var vId = (await db.RulesVersions.FirstAsync()).Id;
        var catId = await DataSeeder.GetOrCreateCategorieAsync(db, vId);
        var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = vId };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();

        var service = new TeamService(db, NullLogger<TeamService>.Instance);
        // R4 : l'XP dépensée est saisie par le coach et débitée de la cagnotte
        await service.AppliquerAmeliorationAsync(joueur.Id, ImprovementType.SelectionPrimaire,
            skillId: skill.Id, xpDepensee: 6);

        var maj = await db.TeamPlayers.Include(j => j.Improvements).Include(j => j.Competences).FirstAsync(j => j.Id == joueur.Id);
        Assert.Single(maj.Improvements);
        Assert.Equal(1, maj.Improvements.First().Palier);
        Assert.Equal(ImprovementType.SelectionPrimaire, maj.Improvements.First().Type);
        Assert.Equal(70_000, maj.ValeurActuelle); // 50_000 + 20_000
        Assert.Equal(0, maj.PointsStarPlayer);    // 6 XP dépensés sur 6
        Assert.Contains(maj.Competences, c => c.SkillId == skill.Id && !c.EstCompetenceDepart);
    }

    // ─── Quota par poste dans CreerEquipeAsync ────────────────────────────────

    [Fact]
    public async Task CreerEquipe_TropDeJoueursMemePoste_LeveException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team
        {
            Nom = "Les Débordants",
            CoachId = coach.Id,
            LeagueId = ligue.Id,
            TeamTypeId = teamType.Id
        };

        // position.QuantiteMax vaut 16 (valeur seeded) ; on en demande 17
        var joueurs = Enumerable.Range(1, position.QuantiteMax + 1)
            .Select(i => (position.Id, $"Joueur{i}", i))
            .ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreerEquipeAsync(equipe, joueurs));
    }

    // ─── Limites par mot-clé ──────────────────────────────────────────────────

    [Fact]
    public async Task CreerEquipe_DepasseLimiteMotCle_LeveException()
    {
        await using var db = _factory.CreateContext();
        var (game, _) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);

        // Ajouter 2 postes "Gros Bras" et une limite à 1
        var posA = new PlayerPosition
        {
            TeamTypeId = teamType.Id, Nom = "Big A", QuantiteMax = 1,
            Cout = 100_000, Mouvement = 4, Force = 5, Agilite = "4+", CapacitePasse = "5+", Armure = "10+",
            CompetencesPrincipales = "F", CompetencesSecondaires = "G",
            MotsCles = "Gros Bras,Troll"
        };
        var posB = new PlayerPosition
        {
            TeamTypeId = teamType.Id, Nom = "Big B", QuantiteMax = 1,
            Cout = 100_000, Mouvement = 4, Force = 5, Agilite = "4+", CapacitePasse = "5+", Armure = "10+",
            CompetencesPrincipales = "F", CompetencesSecondaires = "G",
            MotsCles = "Gros Bras,Ogre"
        };
        db.PlayerPositions.AddRange(posA, posB);
        db.Set<TeamTypeKeywordLimit>().Add(new TeamTypeKeywordLimit { TeamTypeId = teamType.Id, MotCle = "Gros Bras", Max = 1 });
        await db.SaveChangesAsync();

        var coach = DataSeeder.CreateUser("kw");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, (await db.RulesVersions.FirstAsync()).Id, coach.Id);

        var equipe = new Team
        {
            Nom = "DeuxGB", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id,
            Tresorerie = 999_999_999
        };
        var joueurs = new List<(int positionId, string nom, int numero)>
        {
            (posA.Id, "GB1", 1),
            (posB.Id, "GB2", 2),
        };

        var service = new TeamService(db, NullLogger<TeamService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerEquipeAsync(equipe, joueurs));
    }

    // ─── Phase Inscription : guards + multi-équipes + édition + suppression ───

    [Fact]
    public async Task CreerEquipe_LigueEnCreation_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var coach = DataSeeder.CreateUser("creation");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, coach.Id, LeagueStatus.Creation);

        var svc = CreateService(db);
        var equipe = new Team { Nom = "X", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]));
    }

    [Fact]
    public async Task CreerEquipe_LigueEnCours_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var coach = DataSeeder.CreateUser("encours");
        db.Users.Add(coach);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, coach.Id, LeagueStatus.EnCours);

        var svc = CreateService(db);
        var equipe = new Team { Nom = "X", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]));
    }

    [Fact]
    public async Task CreerEquipe_MultiEquipesMemeCoach_Succes()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        await svc.CreerEquipeAsync(
            new Team { Nom = "Première", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id },
            [(position.Id, "J1", 1)]);
        await svc.CreerEquipeAsync(
            new Team { Nom = "Seconde", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id },
            [(position.Id, "J2", 1)]);

        await using var db2 = _factory.CreateContext();
        var count = await db2.Teams.CountAsync(t => t.CoachId == coach.Id && t.LeagueId == ligue.Id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ModifierEquipe_PhaseInscription_MetAJourNomEtRoster()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team { Nom = "Avant", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "Vieux", 1)]);

        await svc.ModifierEquipeAsync(
            equipe.Id, coach.Id, "Après", tresorerie: 400_000,
            nombreRelances: 2, fansDevoues: 3, coachsAssistants: 1, cheerleaders: 0, apothicaire: true,
            joueurs: [(position.Id, "Nouveau1", 1), (position.Id, "Nouveau2", 2), (position.Id, "Nouveau3", 3)]);

        await using var db2 = _factory.CreateContext();
        var team = await db2.Teams.Include(t => t.Joueurs).FirstAsync(t => t.Id == equipe.Id);
        Assert.Equal("Après", team.Nom);
        Assert.Equal(400_000, team.Tresorerie);
        Assert.Equal(2, team.NombreRelances);
        Assert.Equal(3, team.FansDevoues);
        Assert.True(team.Apothicaire);
        Assert.Equal(3, team.Joueurs.Count);
        Assert.DoesNotContain(team.Joueurs, j => j.Nom == "Vieux");
    }

    [Fact]
    public async Task ModifierEquipe_HorsPhaseInscription_ThrowsException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team { Nom = "Test", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]);

        // Faire passer la ligue en EnCours
        await using (var dbUpdate = _factory.CreateContext())
        {
            var l = await dbUpdate.Leagues.FindAsync(ligue.Id);
            l!.Statut = LeagueStatus.EnCours;
            await dbUpdate.SaveChangesAsync();
        }

        await using var db3 = _factory.CreateContext();
        var svc2 = CreateService(db3);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc2.ModifierEquipeAsync(equipe.Id, coach.Id, "Renommée", 0, 0, 0, 0, 0, false,
                [(position.Id, "Z", 1)]));
    }

    [Fact]
    public async Task ModifierEquipe_NonProprietaire_ThrowsException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var autre = DataSeeder.CreateUser("intrus");
        db.Users.Add(autre);
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var equipe = new Team { Nom = "Test", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ModifierEquipeAsync(equipe.Id, autre.Id, "Hack", 0, 0, 0, 0, 0, false,
                [(position.Id, "Z", 1)]));
    }

    [Fact]
    public async Task SupprimerEquipe_PhaseInscription_SupprimeEquipeEtJoueurs()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team { Nom = "ASupprimer", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1), (position.Id, "J2", 2)]);

        await svc.SupprimerEquipeAsync(equipe.Id, coach.Id);

        await using var db2 = _factory.CreateContext();
        Assert.Null(await db2.Teams.FindAsync(equipe.Id));
        Assert.Equal(0, await db2.TeamPlayers.CountAsync(j => j.TeamId == equipe.Id));
    }

    [Fact]
    public async Task SupprimerEquipe_HorsPhaseInscription_ThrowsException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var equipe = new Team { Nom = "T", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]);

        await using (var dbUpdate = _factory.CreateContext())
        {
            var l = await dbUpdate.Leagues.FindAsync(ligue.Id);
            l!.Statut = LeagueStatus.EnCours;
            await dbUpdate.SaveChangesAsync();
        }

        await using var db3 = _factory.CreateContext();
        var svc2 = CreateService(db3);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc2.SupprimerEquipeAsync(equipe.Id, coach.Id));
    }

    [Fact]
    public async Task SupprimerEquipe_NonProprietaire_ThrowsException()
    {
        var (coach, teamType, position, ligue) = await SetupAsync();
        await using var db = _factory.CreateContext();
        var autre = DataSeeder.CreateUser("intrusD");
        db.Users.Add(autre);
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var equipe = new Team { Nom = "T", CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = teamType.Id };
        await svc.CreerEquipeAsync(equipe, [(position.Id, "J1", 1)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.SupprimerEquipeAsync(equipe.Id, autre.Id));
    }
}
