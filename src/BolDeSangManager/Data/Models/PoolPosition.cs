using System.ComponentModel.DataAnnotations.Schema;

namespace BolDeSangManager.Data.Models;

/// <summary>
/// Poste "Réserve" : définition de joueur réutilisable au niveau d'une RulesVersion.
/// À l'import dans un TeamType, il est COPIÉ en PlayerPosition (design catalogue).
/// </summary>
public class PoolPosition
{
    public int Id { get; set; }
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;
    public int QuantiteMax { get; set; } = 1;
    public int Cout { get; set; }
    public int Mouvement { get; set; }
    public int Force { get; set; }
    public string Agilite { get; set; } = "3+";
    public string CapacitePasse { get; set; } = "-";
    public string Armure { get; set; } = "9+";

    /// <summary>Accès aux catégories de compétence (principal / secondaire).</summary>
    public ICollection<PoolPositionCategoryAccess> AccesCategories { get; set; } = [];

    /// <summary>Codes d'accès principaux au format seed ("GAF"). Non persisté.</summary>
    [NotMapped]
    public string CompetencesPrincipales { get; set; } = "G";

    /// <summary>Codes d'accès secondaires au format seed ("AS"). Non persisté.</summary>
    [NotMapped]
    public string CompetencesSecondaires { get; set; } = string.Empty;
    public string MotsCles { get; set; } = string.Empty;

    [NotMapped]
    public string _StartingSkillsTemp { get; set; } = string.Empty;

    public ICollection<PoolPositionSkill> CompetencesDepart { get; set; } = [];
}

public class PoolPositionSkill
{
    public int PoolPositionId { get; set; }
    public PoolPosition PoolPosition { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
