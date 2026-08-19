using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Les 6 catégories de compétence standard (LRB Saison 3).
/// Sert de vocabulaire pour le seed d'une base neuve ET de table de correspondance
/// pour la migration de l'ancien enum <see cref="SkillCategory"/> vers la table SkillCategories.
/// Une fois en base, ces catégories sont des données comme les autres : éditables et supprimables.
/// </summary>
public static class StandardSkillCategories
{
    /// <summary>Nom et code d'affichage de chaque catégorie standard.</summary>
    public static readonly (SkillCategory Enum, string Nom, string Code)[] Toutes =
    [
        (SkillCategory.Agilite,   "Agilité",   "A"),
        (SkillCategory.Force,     "Force",     "F"),
        (SkillCategory.Generale,  "Générale",  "G"),
        (SkillCategory.Mutation,  "Mutation",  "M"),
        (SkillCategory.Passe,     "Passe",     "P"),
        (SkillCategory.Scelerate, "Scélérate", "S"),
    ];

    /// <summary>Nom complet correspondant à une valeur de l'ancien enum.</summary>
    public static string Nom(SkillCategory c) => Toutes.First(t => t.Enum == c).Nom;

    /// <summary>Code d'affichage correspondant à une valeur de l'ancien enum.</summary>
    public static string Code(SkillCategory c) => Toutes.First(t => t.Enum == c).Code;
}
