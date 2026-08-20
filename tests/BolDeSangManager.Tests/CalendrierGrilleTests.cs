using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Construction de la grille du calendrier mensuel (#1).
///
/// Le calcul est court mais piégeux : DayOfWeek place dimanche à 0 alors que
/// la semaine française commence le lundi. Une erreur d'un jour décalerait
/// tout le mois sans que rien ne plante — d'où ces tests.
/// </summary>
public class CalendrierGrilleTests
{
    /// <summary>
    /// Réplique exacte de la logique de Ligues/Detail.razor (JoursAffiches).
    /// Si l'une change, l'autre doit suivre.
    /// </summary>
    private static List<DateTime> JoursAffiches(DateTime mois)
    {
        var premier = new DateTime(mois.Year, mois.Month, 1);
        var decalage = ((int)premier.DayOfWeek + 6) % 7;
        var debut = premier.AddDays(-decalage);
        return Enumerable.Range(0, 42).Select(i => debut.AddDays(i)).ToList();
    }

    [Fact]
    public void LaGrille_CommenceToujoursUnLundi()
    {
        // douze mois d'affilée, pour couvrir toutes les configurations
        for (var i = 0; i < 12; i++)
        {
            var jours = JoursAffiches(new DateTime(2026, 1, 1).AddMonths(i));
            Assert.Equal(DayOfWeek.Monday, jours[0].DayOfWeek);
        }
    }

    [Fact]
    public void LaGrille_CouvreSixSemainesPleines()
    {
        var jours = JoursAffiches(new DateTime(2026, 9, 1));

        Assert.Equal(42, jours.Count);
        Assert.Equal(DayOfWeek.Sunday, jours[^1].DayOfWeek);
    }

    [Fact]
    public void LaGrille_ContientTousLesJoursDuMois()
    {
        var mois = new DateTime(2026, 9, 1);
        var jours = JoursAffiches(mois);

        for (var j = 1; j <= DateTime.DaysInMonth(2026, 9); j++)
            Assert.Contains(new DateTime(2026, 9, j), jours);
    }

    [Fact]
    public void UnMoisCommencantUnLundi_NAffichePasDeJourAvant()
    {
        // juin 2026 commence un lundi : aucun débordement en tête
        var jours = JoursAffiches(new DateTime(2026, 6, 1));

        Assert.Equal(new DateTime(2026, 6, 1), jours[0]);
    }

    [Fact]
    public void UnMoisCommencantUnDimanche_AfficheSixJoursDuMoisPrecedent()
    {
        // février 2026 commence un dimanche : c'est le pire cas du décalage
        var jours = JoursAffiches(new DateTime(2026, 2, 1));

        Assert.Equal(new DateTime(2026, 1, 26), jours[0]);
        Assert.Equal(DayOfWeek.Monday, jours[0].DayOfWeek);
        Assert.Equal(new DateTime(2026, 2, 1), jours[6]);
    }

    [Fact]
    public void FevrierBissextile_EstCouvertEntierement()
    {
        var jours = JoursAffiches(new DateTime(2028, 2, 1));

        Assert.Contains(new DateTime(2028, 2, 29), jours);
    }

    [Fact]
    public void LesJoursSontConsecutifsSansTrou()
    {
        var jours = JoursAffiches(new DateTime(2026, 9, 1));

        for (var i = 1; i < jours.Count; i++)
            Assert.Equal(jours[i - 1].AddDays(1), jours[i]);
    }

    [Fact]
    public void LeChangementDHeure_NeDecalePasLaGrille()
    {
        // octobre 2026 contient le passage à l'heure d'hiver
        var jours = JoursAffiches(new DateTime(2026, 10, 1));

        Assert.Equal(42, jours.Count);
        for (var i = 1; i < jours.Count; i++)
            Assert.Equal(jours[i - 1].AddDays(1), jours[i]);
    }
}
