using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Règle spéciale « Capitaine » : le joueur désigné par le coach gagne une
/// compétence tant qu'il porte le titre.
///
/// La compétence est CALCULÉE ici, jamais écrite dans TeamPlayerSkills. Trois
/// conséquences voulues :
///   • changer de capitaine n'exige aucun nettoyage — l'ancien la perd seul ;
///   • elle n'entre pas dans la valeur de l'équipe (choix produit : c'est un
///     titre, pas une progression gagnée en jouant) ;
///   • un capitaine mort ou parti cesse de l'afficher sans intervention.
///
/// Le NOM de la compétence vient du paramètre de la liaison race↔règle : rien
/// n'est écrit en dur, une future règle offrant autre chose que « Pro » se
/// règle depuis l'admin.
/// </summary>
public static class CapitaineHelper
{
    /// <summary>
    /// Compétence offerte à ce joueur par le titre de capitaine, ou
    /// <c>null</c> s'il n'est pas capitaine, si sa race n'a pas la règle, ou
    /// si le commissaire n'a pas renseigné la compétence.
    /// </summary>
    public static string? CompetenceOfferte(TeamPlayer joueur, TeamType? race)
    {
        if (!joueur.EstCapitaine || race is null)
            return null;

        var lien = race.ReglesSpecialesListe
            .FirstOrDefault(l => l.SpecialRule?.Code == SpecialRuleCodes.CompetenceAuCapitaine);

        if (lien is null)
            return null;

        // Une seule compétence a du sens ici, mais on tolère un CSV : le champ
        // est partagé avec les autres comportements, autant rester cohérent.
        var valeurs = SpecialRuleCodes.DecouperOptions(lien.OptionsChoix);
        return valeurs.Length > 0 ? valeurs[0] : null;
    }

    /// <summary>
    /// Vrai si la race porte la règle « Capitaine » : sert à n'afficher le
    /// sélecteur que là où il a un sens.
    /// </summary>
    public static bool RaceAUnCapitaine(TeamType? race) =>
        race?.ReglesSpecialesListe
            .Any(l => l.SpecialRule?.Code == SpecialRuleCodes.CompetenceAuCapitaine) == true;
}
