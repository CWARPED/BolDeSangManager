using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class League
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CommissaireId { get; set; } = string.Empty;
    public ApplicationUser Commissaire { get; set; } = null!;
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;
    public LeagueFormat Format { get; set; } = LeagueFormat.RoundRobinAvecPlayoffs;
    public LeagueStatus Statut { get; set; } = LeagueStatus.Creation;
    public int BudgetDepart { get; set; } = 1_000_000;
    public int NombreEquipesPlayoff { get; set; } = 4;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public ICollection<Division> Divisions { get; set; } = [];
    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<PhaseDeReposValidation> ValidationsRepos { get; set; } = [];
    public ICollection<LeagueAward> Awards { get; set; } = [];
    public ICollection<LeagueCommissioner> CommissairesDeLigue { get; set; } = [];
}

public class Division
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public int Ordre { get; set; }

    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<Match> Matchs { get; set; } = [];
}
