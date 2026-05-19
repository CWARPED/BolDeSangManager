using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class Skill
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public SkillCategory Categorie { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool EstElite { get; set; } = false;
    public bool EstTrait { get; set; } = false;

    // Chaque skill appartient à une version précise. Les skills universels sont dupliqués entre versions au seed.
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public ICollection<PlayerPositionSkill> PositionsAvecCompetence { get; set; } = [];
    public ICollection<TeamPlayerSkill> JoueursAvecCompetence { get; set; } = [];
}
