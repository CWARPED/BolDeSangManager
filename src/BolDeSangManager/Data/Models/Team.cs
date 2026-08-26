using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class Team
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string CoachId { get; set; } = string.Empty;
    public ApplicationUser Coach { get; set; } = null!;
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int? DivisionId { get; set; }
    public Division? Division { get; set; }
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;

    // Finances
    public int Tresorerie { get; set; } = 0;

    // Staff — colonnes HISTORIQUES.
    // Conservées en base (migration purement additive, principe #1 : aucune
    // équipe live ne doit casser), mais elles ne sont plus la source de vérité :
    // le staff est désormais porté par la collection Staff ci-dessous, qui gère
    // une liste ouverte de types configurés dans les règles.
    // Ne pas lire ces champs dans du code nouveau.
    public int FansDevoues { get; set; } = 0;
    public int NombreRelances { get; set; } = 0;
    public int NombreCoachsAssistants { get; set; } = 0;
    public int NombreCheerleaders { get; set; } = 0;
    public bool Apothicaire { get; set; } = false;

    // Statistiques de ligue
    public int PointsLigue { get; set; } = 0;
    public int TouchdownsMarques { get; set; } = 0;
    public int TouchdownsConcedes { get; set; } = 0;
    public int EliminationsInfligees { get; set; } = 0;
    public int NombreMatchsJoues { get; set; } = 0;
    public int NombreVictoires { get; set; } = 0;
    public int NombreDefaites { get; set; } = 0;
    public int NombreNuls { get; set; } = 0;

    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public ICollection<TeamPlayer> Joueurs { get; set; } = [];

    /// <summary>Staff détenu par l'équipe (source de vérité depuis le dev staff).</summary>
    public ICollection<TeamStaff> Staff { get; set; } = [];
}

public class TeamPlayer
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int PlayerPositionId { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public int Numero { get; set; }

    // XP et progression
    public int PointsStarPlayer { get; set; } = 0;
    // Collection des améliorations de palier (voir PlayerImprovement).
    // Le nombre de paliers consommés = Improvements.Count.
    public ICollection<PlayerImprovement> Improvements { get; set; } = [];
    public int ValeurActuelle { get; set; } = 0;

    // Modificateurs de caractéristiques (positif = amélioration, négatif = réduction)
    public int ModMouvement { get; set; } = 0;
    public int ModForce { get; set; } = 0;
    public int ModAgilite { get; set; } = 0;
    public int ModCapacitePasse { get; set; } = 0;
    public int ModArmure { get; set; } = 0;

    // Statut
    public bool ManqueSuivantMatch { get; set; } = false;
    public bool EstMort { get; set; } = false;
    public bool EstRetraite { get; set; } = false;

    public DateTime RecruteLe { get; set; } = DateTime.UtcNow;

    public ICollection<TeamPlayerSkill> Competences { get; set; } = [];
    public ICollection<PlayerInjury> Blessures { get; set; } = [];
    public ICollection<MatchPlayerRecord> RecordsMatchs { get; set; } = [];
}

public class TeamPlayerSkill
{
    public int Id { get; set; }
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public bool EstCompetenceDepart { get; set; } = true; // Inherited from position
    public bool EnAttenteValidation { get; set; } = false; // Waiting commissioner validation
}

public class PlayerInjury
{
    public int Id { get; set; }
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;
    public int? MatchId { get; set; }
    public Match? Match { get; set; }
    public InjuryType Type { get; set; }
    public AffectedStat? StatAffectee { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
