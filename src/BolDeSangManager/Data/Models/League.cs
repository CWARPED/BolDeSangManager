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

    /// <summary>
    /// Règlement de la ligue en markdown brut (R5). Rédigé par les commissaires,
    /// consultable par tous les participants et exportable en PDF.
    /// Le HTML brut est désactivé au rendu : voir MarkdownService.
    /// </summary>
    public string Reglement { get; set; } = string.Empty;

    // ── Barème d'XP de la ligue (R6) ──────────────────────────────────────────
    // Valeurs par défaut = LRB Saison 3. Le touchdown vaut 5 en Dungeon Bowl :
    // à la création, le formulaire pré-remplit selon le jeu choisi.
    // Les matchs déjà saisis conservent leur XP : rien n'est recalculé.

    /// <summary>XP par touchdown.</summary>
    public int XpParTouchdown { get; set; } = 3;

    /// <summary>XP par passe complétée.</summary>
    public int XpParPasse { get; set; } = 1;

    /// <summary>XP par interception.</summary>
    public int XpParInterception { get; set; } = 2;

    /// <summary>XP par élimination infligée.</summary>
    public int XpParElimination { get; set; } = 2;

    /// <summary>XP bonus pour le joueur désigné MVP.</summary>
    public int XpBonusMvp { get; set; } = 4;

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

/// <summary>
/// Date indicative de fin d'une ronde : la date à laquelle les matchs de cette
/// ronde devraient être joués. Purement informative — rien n'est bloqué ni
/// clôturé automatiquement à son échéance.
///
/// Table dédiée plutôt qu'une colonne sur <see cref="Match"/> : une ronde n'est
/// pas une entité en base (juste un numéro porté par ses matchs), et dupliquer
/// la date sur chaque match la rendrait incohérente dès la première édition.
/// </summary>
public class EcheanceRonde
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;

    /// <summary>Numéro de ronde concerné (même numérotation que Match.Ronde).</summary>
    public int Ronde { get; set; }

    /// <summary>Date limite conseillée, stockée en UTC.</summary>
    public DateTime DateLimite { get; set; }
}
