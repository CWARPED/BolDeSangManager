using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Tests.Helpers;

/// <summary>
/// Insère les données minimales nécessaires aux tests dans le DbContext fourni.
/// </summary>
public static class DataSeeder
{
    public static ApplicationUser CreateUser(string suffix = "")
    {
        var id = Guid.NewGuid().ToString();
        var email = $"user{suffix}@test.fr";
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PseudoCoach = $"Coach{suffix}"
        };
    }

    public static async Task<(Game game, RulesVersion version)> SeedGameAsync(ApplicationDbContext db)
    {
        var game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var version = new RulesVersion { GameId = game.Id, Nom = "Saison 3", EstActive = true, Ordre = 1 };
        db.RulesVersions.Add(version);
        await db.SaveChangesAsync();

        return (game, version);
    }

    public static async Task<(TeamType teamType, PlayerPosition position)> SeedTeamTypeAsync(
        ApplicationDbContext db, int gameId)
    {
        var versionId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(db.RulesVersions.Where(v => v.GameId == gameId).Select(v => v.Id));

        var teamType = new TeamType
        {
            GameId = gameId,
            RulesVersionId = versionId,
            Nom = "Humains",
            CoutRelance = 50_000,
            Categorie = TeamCategory.Staller
        };
        db.TeamTypes.Add(teamType);
        await db.SaveChangesAsync();

        var position = new PlayerPosition
        {
            TeamTypeId = teamType.Id,
            Nom = "Lineman",
            QuantiteMax = 16,
            Cout = 50_000,
            Mouvement = 6,
            Force = 3,
            Agilite = "3+",
            CapacitePasse = "4+",
            Armure = "9+",
            CompetencesPrincipales = "G",
            CompetencesSecondaires = "AF"
        };
        db.PlayerPositions.Add(position);
        await db.SaveChangesAsync();

        return (teamType, position);
    }

    public static async Task<(Skill normal, Skill elite)> SeedSkillsAsync(ApplicationDbContext db)
    {
        var versionId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstAsync(db.RulesVersions.Select(v => v.Id));

        var normal = new Skill
        {
            RulesVersionId = versionId,
            Nom = "Blocage",
            Categorie = SkillCategory.Generale,
            Description = "Compétence de blocage.",
            EstElite = false,
            EstTrait = false
        };
        var elite = new Skill
        {
            RulesVersionId = versionId,
            Nom = "Meurtre Prémédité",
            Categorie = SkillCategory.Scelerate,
            Description = "Compétence élite.",
            EstElite = true,
            EstTrait = false
        };
        db.Skills.AddRange(normal, elite);
        await db.SaveChangesAsync();
        return (normal, elite);
    }

    public static async Task<League> SeedLeagueAsync(
        ApplicationDbContext db, int gameId, int rulesVersionId, string commissaireId,
        LeagueStatus statut = LeagueStatus.Inscription)
    {
        var ligue = new League
        {
            Nom = "Ligue de Test",
            Description = "Tests automatisés",
            CommissaireId = commissaireId,
            GameId = gameId,
            RulesVersionId = rulesVersionId,
            Format = LeagueFormat.RoundRobinAvecPlayoffs,
            BudgetDepart = 1_000_000,
            NombreEquipesPlayoff = 4,
            Statut = statut,
            CreeLe = DateTime.UtcNow
        };
        db.Leagues.Add(ligue);
        await db.SaveChangesAsync();
        return ligue;
    }

    public static async Task<Team> SeedTeamAsync(
        ApplicationDbContext db, int ligueId, string coachId, int teamTypeId, string nom = "Équipe A")
    {
        var team = new Team
        {
            Nom = nom,
            CoachId = coachId,
            LeagueId = ligueId,
            TeamTypeId = teamTypeId,
            Tresorerie = 500_000,
            FansDevoues = 1,
            NombreRelances = 2,
            CreeLe = DateTime.UtcNow
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    public static async Task<TeamPlayer> SeedPlayerAsync(
        ApplicationDbContext db, int teamId, int positionId, string nom = "Joueur 1", int numero = 1)
    {
        var player = new TeamPlayer
        {
            TeamId = teamId,
            PlayerPositionId = positionId,
            Nom = nom,
            Numero = numero,
            ValeurActuelle = 50_000,
            RecruteLe = DateTime.UtcNow
        };
        db.TeamPlayers.Add(player);
        await db.SaveChangesAsync();
        return player;
    }

    /// <summary>
    /// Crée un match minimal (sans division) reliant deux équipes.
    /// </summary>
    public static async Task<Match> SeedMatchAsync(
        ApplicationDbContext db, int domicileId, int exterieurId, int? divisionId = null)
    {
        var match = new Match
        {
            DivisionId = divisionId,
            Ronde = 1,
            EquipeDomicileId = domicileId,
            EquipeExterieurId = exterieurId,
            Statut = MatchStatus.AJouer,
            EstPlayoff = false
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }
}
