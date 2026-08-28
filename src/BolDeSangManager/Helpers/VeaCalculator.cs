using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Calcul de la Valeur d'Équipe Actuelle (VEA) — <b>source unique</b>.
///
/// ⚠️ Historique : la VEA était calculée à deux endroits. <c>PdfService</c>
/// refaisait la somme à partir des colonnes de staff HISTORIQUES de
/// <see cref="Team"/> (<c>FansDevoues</c>, <c>NombreRelances</c>…), que le
/// modèle documente pourtant comme « ne pas lire dans du code nouveau » depuis
/// que le staff est porté par la liste ouverte <see cref="Team.Staff"/>.
/// La feuille d'équipe imprimée affichait donc une VEA amputée de tout le
/// staff (80k au lieu de 300k sur l'équipe de référence des tests).
///
/// Toute nouvelle règle touchant la VEA — par exemple « Trois-quarts à Vil
/// Prix », qui traite le coût des Trois-quarts comme nul — s'écrit ICI et
/// nulle part ailleurs, sinon écran et PDF divergent à nouveau.
/// </summary>
public static class VeaCalculator
{
    /// <summary>
    /// Somme la valeur des joueurs actifs et du staff détenu.
    ///
    /// Les joueurs morts ou retraités sont exclus. Le staff compte sa quantité
    /// TOTALE, y compris les unités offertes à la création (<c>MinCreation</c>) :
    /// elles ne sont pas facturées au budget de départ, mais elles ont une
    /// valeur — voir <see cref="StaffService.UnitesFacturees"/>.
    /// </summary>
    public static int Calculer(Team equipe)
    {
        var totalJoueurs = equipe.Joueurs
            .Where(j => !j.EstMort && !j.EstRetraite)
            .Sum(j => j.ValeurActuelle);

        var totalStaff = equipe.Staff
            .Where(s => s.LeagueStaffType is not null)
            .Sum(s => s.Quantite * StaffService.CoutUnitaire(s.LeagueStaffType, equipe.TeamType));

        return totalJoueurs + totalStaff;
    }
}
