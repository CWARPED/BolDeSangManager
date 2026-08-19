using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class Skill
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Ancienne catégorie figée (enum). Conservée le temps de la migration vers
    /// <see cref="SkillCategoryDefId"/> ; ne plus lire ce champ dans du code neuf.
    /// </summary>
    public SkillCategory Categorie { get; set; }

    /// <summary>Catégorie de la compétence, définie au niveau de la version de règles.</summary>
    public int SkillCategoryDefId { get; set; }
    public SkillCategoryDef SkillCategoryDef { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public bool EstElite { get; set; } = false;
    public bool EstTrait { get; set; } = false;

    // Chaque skill appartient à une version précise. Les skills universels sont dupliqués entre versions au seed.
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public ICollection<PlayerPositionSkill> PositionsAvecCompetence { get; set; } = [];
    public ICollection<TeamPlayerSkill> JoueursAvecCompetence { get; set; } = [];
}
