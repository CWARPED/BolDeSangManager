using System.ComponentModel.DataAnnotations.Schema;
using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class TeamType
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public string ReglesSpeciales { get; set; } = string.Empty;
    public int CoutRelance { get; set; } = 50000;
    public TeamCategory Categorie { get; set; } = TeamCategory.Specialist;

    // CSV des règles spéciales d'éligibilité aux ligues thématiques.
    // Ex: "OldWorldClassic,BadlandsBrawl". Vide = aucune règle spéciale.
    public string ReglesSpecialesLigue { get; set; } = string.Empty;

    public ICollection<PlayerPosition> Postes { get; set; } = [];
    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<TeamTypeKeywordLimit> LimitesMotsCles { get; set; } = [];
}

public class PlayerPosition
{
    public int Id { get; set; }
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public int QuantiteMax { get; set; }
    public int Cout { get; set; }
    public int Mouvement { get; set; }
    public int Force { get; set; }
    public string Agilite { get; set; } = "3+";     // "2+", "3+", "4+", "5+", "6+", "-"
    public string CapacitePasse { get; set; } = "-"; // idem
    public string Armure { get; set; } = "9+";       // "6+", "7+", ..., "11+"

    /// <summary>
    /// Accès aux catégories de compétence (principal / secondaire).
    /// Remplace les anciennes chaînes de lettres : voir <see cref="PlayerPositionCategoryAccess"/>.
    /// </summary>
    public ICollection<PlayerPositionCategoryAccess> AccesCategories { get; set; } = [];

    /// <summary>Codes d'accès principaux au format seed ("GAF"). Non persisté — cf. DbSeeder.</summary>
    [NotMapped]
    public string CompetencesPrincipales { get; set; } = "G";

    /// <summary>Codes d'accès secondaires au format seed ("AS"). Non persisté — cf. DbSeeder.</summary>
    [NotMapped]
    public string CompetencesSecondaires { get; set; } = string.Empty;

    // CSV des mots-clés du poste (ex: "Trois-quart,Humain,Squelette,Mort-Vivant").
    // Utilisés par les compétences/traits qui ciblent des keywords (ex: Haine (X)).
    public string MotsCles { get; set; } = string.Empty;

    [NotMapped]
    public string _StartingSkillsTemp { get; set; } = string.Empty;

    public ICollection<PlayerPositionSkill> CompetencesDepart { get; set; } = [];
    public ICollection<TeamPlayer> Joueurs { get; set; } = [];
}

public class PlayerPositionSkill
{
    public int PlayerPositionId { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
