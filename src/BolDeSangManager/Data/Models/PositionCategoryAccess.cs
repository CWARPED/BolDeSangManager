namespace BolDeSangManager.Data.Models;

/// <summary>
/// Accès d'un poste (<see cref="PlayerPosition"/>) à une catégorie de compétence.
/// Remplace les anciennes chaînes de lettres « GAF » / « AS » : le lien se fait par
/// identifiant, donc renommer une catégorie ou lui donner un code à 2 lettres n'a
/// aucun impact sur les accès.
/// </summary>
public class PlayerPositionCategoryAccess
{
    public int PlayerPositionId { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = null!;

    public int SkillCategoryDefId { get; set; }
    public SkillCategoryDef SkillCategoryDef { get; set; } = null!;

    /// <summary>true = accès principal, false = accès secondaire.</summary>
    public bool EstPrincipale { get; set; }
}

/// <summary>Jumeau de <see cref="PlayerPositionCategoryAccess"/> pour la Réserve.</summary>
public class PoolPositionCategoryAccess
{
    public int PoolPositionId { get; set; }
    public PoolPosition PoolPosition { get; set; } = null!;

    public int SkillCategoryDefId { get; set; }
    public SkillCategoryDef SkillCategoryDef { get; set; } = null!;

    /// <summary>true = accès principal, false = accès secondaire.</summary>
    public bool EstPrincipale { get; set; }
}
