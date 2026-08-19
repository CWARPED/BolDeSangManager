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

    /// <summary>
    /// Mode brouillard (#2) : masque le calendrier à venir aux coaches.
    /// Un coach ne voit alors que son prochain match programmé et l'ensemble des
    /// matchs déjà joués (les siens comme ceux des autres). But : éviter qu'on
    /// joue un match en anticipant les rencontres suivantes.
    /// Les commissaires ne sont jamais concernés.
    /// </summary>
    public bool ModeBrouillard { get; set; } = false;

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
