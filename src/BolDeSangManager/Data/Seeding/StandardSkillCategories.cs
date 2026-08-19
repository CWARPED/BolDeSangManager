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
    /// <summary>Nom, code d'affichage et ordre de chaque catégorie standard.</summary>
    public static readonly (SkillCategory Enum, string Nom, string Code, int Ordre)[] Toutes =
    [
        (SkillCategory.Agilite,   "Agilité",   "A", 1),
        (SkillCategory.Force,     "Force",     "F", 2),
        (SkillCategory.Generale,  "Générale",  "G", 3),
        (SkillCategory.Mutation,  "Mutation",  "M", 4),
        (SkillCategory.Passe,     "Passe",     "P", 5),
        (SkillCategory.Scelerate, "Scélérate", "S", 6),
    ];

    /// <summary>Nom complet correspondant à une valeur de l'ancien enum.</summary>
    public static string Nom(SkillCategory c) => Toutes.First(t => t.Enum == c).Nom;

    /// <summary>Code d'affichage correspondant à une valeur de l'ancien enum.</summary>
    public static string Code(SkillCategory c) => Toutes.First(t => t.Enum == c).Code;
}
