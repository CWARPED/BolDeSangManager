using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Accès d'un poste aux catégories de compétence.
///
/// Historique : les accès étaient stockés en chaînes de lettres (« GAF » = Générale,
/// Agilité, Force), découpées caractère par caractère. Depuis R2b, ils sont une vraie
/// relation vers <see cref="SkillCategoryDef"/> — ce qui permet des noms libres et des
/// codes à 2 caractères. Ces helpers ne servent plus qu'au **seed** et à la **migration**
/// des données historiques, ainsi qu'à l'affichage compact.
/// </summary>
public static class CategoryAccessHelpers
{
    /// <summary>
    /// Convertit une chaîne de codes historique (« GAF ») en catégories, en résolvant
    /// chaque caractère par son <see cref="SkillCategoryDef.Code"/>. Les codes inconnus
    /// sont ignorés silencieusement (données héritées potentiellement incohérentes).
    /// </summary>
    public static List<SkillCategoryDef> ResoudreCodesHistoriques(
        string? codes, IEnumerable<SkillCategoryDef> categoriesDeLaVersion)
    {
        if (string.IsNullOrWhiteSpace(codes)) return [];

        var parCode = categoriesDeLaVersion
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var resultat = new List<SkillCategoryDef>();
        foreach (var lettre in codes.Where(char.IsLetter))
        {
            if (parCode.TryGetValue(lettre.ToString(), out var cat) && !resultat.Contains(cat))
                resultat.Add(cat);
        }
        return resultat;
    }

    /// <summary>
    /// Rendu compact des accès d'un poste, ex. « G · A · F ». Renvoie « — » si aucun accès.
    /// </summary>
    public static string FormatAcces(IEnumerable<SkillCategoryDef> categories)
    {
        var codes = categories.OrderBy(c => c.Nom).Select(c => c.Code).ToList();
        return codes.Count == 0 ? "—" : string.Join(" · ", codes);
    }
}
