namespace BolDeSangManager.Data.Models;

/// <summary>
/// Catégorie de compétence, définie au niveau d'une <see cref="RulesVersion"/> (comme la Réserve).
/// Remplace l'ancien enum figé : l'association peut créer ses propres catégories.
/// </summary>
public class SkillCategoryDef
{
    public int Id { get; set; }

    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    /// <summary>Libellé complet, ex. « Agilité ». Unique par version (insensible à la casse).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Abréviation d'affichage, 1 à 2 caractères, ex. « A » ou « DB ». Unique par version.
    /// Purement cosmétique : les liens vers les compétences se font par identifiant.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public ICollection<Skill> Competences { get; set; } = [];
}
