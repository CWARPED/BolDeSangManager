using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Filtrage des compétences proposées à l'après-match selon les accès de
/// catégorie du poste (R7).
///
/// Avant cette règle, la liste proposait <b>toutes</b> les compétences de la
/// version : rien n'empêchait un Trois-quart à accès Générale de prendre une
/// compétence de Mutation, et la case « Principale (+20k) » était cochée à la
/// main sans rapport avec les accès réels. C'était un problème d'équilibrage,
/// pas seulement d'ergonomie.
///
/// Règles retenues (décidées avec David) :
///  • par défaut on ne propose que les catégories accessibles au poste ;
///  • principal / secondaire est <b>déduit</b> de l'accès, plus coché librement ;
///  • les compétences Élite (⭐) sont masquées par défaut ;
///  • une case « hors accès » réaffiche l'intégralité — garde-fou franchissable
///    en connaissance de cause, pour les règles maison et tirages spéciaux.
/// </summary>
public static class AccesCompetencesHelpers
{
    /// <summary>Résultat du filtrage pour une compétence proposée.</summary>
    /// <param name="Skill">La compétence.</param>
    /// <param name="EstPrincipale">
    /// Vrai si la catégorie de la compétence est un accès <b>principal</b> du poste.
    /// Détermine la hausse de valeur (+20k contre +40k).
    /// </param>
    /// <param name="HorsAcces">Vrai si la compétence sort des accès du poste.</param>
    public record CompetenceProposee(Skill Skill, bool EstPrincipale, bool HorsAcces);

    /// <summary>
    /// Compétences à proposer pour un poste donné.
    /// </summary>
    /// <param name="skills">Toutes les compétences de la version de règles.</param>
    /// <param name="acces">Accès de catégorie du poste du joueur.</param>
    /// <param name="inclureHorsAcces">
    /// Case « hors accès » cochée : on renvoie tout, y compris les Élite,
    /// en marquant ce qui sort des accès.
    /// </param>
    public static List<CompetenceProposee> Filtrer(
        IEnumerable<Skill> skills,
        IEnumerable<PlayerPositionCategoryAccess> acces,
        bool inclureHorsAcces = false)
    {
        var parCategorie = acces
            .GroupBy(a => a.SkillCategoryDefId)
            .ToDictionary(g => g.Key, g => g.Any(a => a.EstPrincipale));

        var resultat = new List<CompetenceProposee>();

        foreach (var skill in skills)
        {
            var accessible = parCategorie.TryGetValue(skill.SkillCategoryDefId, out var estPrincipale);

            // Élite : masquées tant que « hors accès » n'est pas coché.
            if (skill.EstElite && !inclureHorsAcces) continue;

            if (!accessible && !inclureHorsAcces) continue;

            resultat.Add(new CompetenceProposee(
                skill,
                EstPrincipale: accessible && estPrincipale,
                HorsAcces: !accessible));
        }

        return resultat
            .OrderByDescending(c => !c.HorsAcces)      // accessibles d'abord
            .ThenByDescending(c => c.EstPrincipale)    // puis principales
            .ThenBy(c => c.Skill.SkillCategoryDef?.Nom ?? string.Empty)
            .ThenBy(c => c.Skill.Nom)
            .ToList();
    }

    /// <summary>
    /// Une compétence relève-t-elle d'un accès principal du poste ? Sert à
    /// recalculer la hausse de valeur côté serveur, sans faire confiance à l'UI.
    /// </summary>
    public static bool EstAccesPrincipal(
        Skill skill,
        IEnumerable<PlayerPositionCategoryAccess> acces) =>
        acces.Any(a => a.SkillCategoryDefId == skill.SkillCategoryDefId && a.EstPrincipale);
}
