using BolDeSangManager.Services;

namespace BolDeSangManager.Tests;

/// <summary>
/// Barème de points de classement : la fonction pure qui décide combien vaut un
/// match. C'est la SEULE source de vérité — le calcul au fil de l'eau comme le
/// recalcul complet passent par elle.
/// </summary>
public class BaremePointsTests
{
    /// <summary>Le barème de la ligue de l'utilisateur (VPS), pour les cas concrets.</summary>
    private static BaremePoints BaremeReference() => new()
    {
        // Au-delà du 12e tour
        Victoire = 2000, Nul = 1500, Defaite = 1000,
        ParTouchdown = 5, ParElimination = 2, ParInterception = 1,
        ParPasse = 1, ParDeviation = 1, ParAgression = 1,
        Paliers = [new PalierPoints(12, 3000, 1500, 0)]
    };

    [Fact]
    public void ParDefaut_ReproduitLeCalculHistorique_3_1_0()
    {
        var b = BaremePoints.ParDefaut();
        Assert.Equal(3, b.PointsEquipe(2, 1, tours: null, actions: default));
        Assert.Equal(1, b.PointsEquipe(1, 1, tours: null, actions: default));
        Assert.Equal(0, b.PointsEquipe(1, 2, tours: null, actions: default));
    }

    [Fact]
    public void ParDefaut_AucunBonus_MemeAvecBeaucoupDActions()
    {
        var b = BaremePoints.ParDefaut();
        var actions = new ActionsEquipe(Touchdowns: 5, Eliminations: 4, Interceptions: 3,
                                        Passes: 7, Deviations: 6, Agressions: 9);
        Assert.Equal(3, b.PointsEquipe(5, 0, tours: 8, actions: actions));
    }

    [Fact]
    public void SansPalier_LeNombreDeToursNaAucunEffet()
    {
        var b = new BaremePoints { Victoire = 3, Nul = 1, Defaite = 0 };
        Assert.Equal(3, b.PointsEquipe(2, 1, tours: 4, actions: default));
        Assert.Equal(3, b.PointsEquipe(2, 1, tours: 16, actions: default));
    }

    [Theory]
    [InlineData(11, 3000, 0)]      // avant le seuil : palier
    [InlineData(12, 3000, 0)]      // AU seuil : palier (JusquAuTour est inclusif)
    [InlineData(13, 2000, 1000)]   // après : points de base
    [InlineData(16, 2000, 1000)]
    public void Palier_SappliqueJusquAuTourInclus(int tours, int attenduVainqueur, int attenduPerdant)
    {
        var b = BaremeReference();
        Assert.Equal(attenduVainqueur, b.PointsEquipe(2, 1, tours, actions: default));
        Assert.Equal(attenduPerdant, b.PointsEquipe(1, 2, tours, actions: default));
    }

    [Fact]
    public void Nul_IdentiqueDesDeuxCotesDuSeuil_QuandLeBaremeLeVeut()
    {
        var b = BaremeReference();
        Assert.Equal(1500, b.PointsEquipe(1, 1, tours: 5, actions: default));
        Assert.Equal(1500, b.PointsEquipe(1, 1, tours: 15, actions: default));
    }

    [Fact]
    public void ToursNonRenseigne_RetombeSurLesPointsDeBase()
    {
        // Cas des matchs déjà joués au moment du déploiement : pas de nombre de
        // tours en base, donc barème de base — jamais une exception ni un zéro.
        var b = BaremeReference();
        Assert.Equal(2000, b.PointsEquipe(2, 1, tours: null, actions: default));
        Assert.Equal(1000, b.PointsEquipe(1, 2, tours: null, actions: default));
    }

    [Fact]
    public void BonusSAdditionnentAuResultat()
    {
        var b = BaremeReference();
        var actions = new ActionsEquipe(Touchdowns: 3, Eliminations: 2, Interceptions: 1,
                                        Passes: 4, Deviations: 2, Agressions: 5);
        // 3000 (victoire avant le 13e) + 15 + 4 + 1 + 4 + 2 + 5 = 3031
        Assert.Equal(3031, b.PointsEquipe(3, 0, tours: 10, actions: actions));
    }

    [Fact]
    public void BonusSAppliquentAussiAuPerdant()
    {
        var b = BaremeReference();
        var actions = new ActionsEquipe(Touchdowns: 1, Eliminations: 3, Interceptions: 0,
                                        Passes: 2, Deviations: 1, Agressions: 4);
        // 0 (défaite avant le 13e) + 5 + 6 + 0 + 2 + 1 + 4 = 18
        Assert.Equal(18, b.PointsEquipe(1, 3, tours: 9, actions: actions));
    }

    [Fact]
    public void PlusieursPaliers_LePlusPetitSeuilAtteintGagne()
    {
        var b = new BaremePoints
        {
            Victoire = 1000, Nul = 500, Defaite = 100,
            Paliers =
            [
                new PalierPoints(12, 3000, 1500, 0),
                new PalierPoints(8,  5000, 2000, 0)
            ]
        };
        Assert.Equal(5000, b.PointsEquipe(2, 0, tours: 7, actions: default));
        Assert.Equal(5000, b.PointsEquipe(2, 0, tours: 8, actions: default));
        Assert.Equal(3000, b.PointsEquipe(2, 0, tours: 9, actions: default));
        Assert.Equal(1000, b.PointsEquipe(2, 0, tours: 13, actions: default));
    }

    [Fact]
    public void ActionsDe_AgregeLeBonCote()
    {
        var records = new List<BolDeSangManager.Data.Models.MatchPlayerRecord>
        {
            new() { EstCoteDomicile = true,  Touchdowns = 2, Passes = 1, Deviations = 3, Agressions = 1 },
            new() { EstCoteDomicile = true,  Touchdowns = 1, EliminationsInfligees = 2 },
            new() { EstCoteDomicile = false, Touchdowns = 5, Agressions = 9, Interceptions = 4 }
        };

        var dom = BaremePoints.ActionsDe(records, coteDomicile: true);
        Assert.Equal(3, dom.Touchdowns);
        Assert.Equal(2, dom.Eliminations);
        Assert.Equal(1, dom.Passes);
        Assert.Equal(3, dom.Deviations);
        Assert.Equal(1, dom.Agressions);
        Assert.Equal(0, dom.Interceptions);

        var ext = BaremePoints.ActionsDe(records, coteDomicile: false);
        Assert.Equal(5, ext.Touchdowns);
        Assert.Equal(9, ext.Agressions);
        Assert.Equal(4, ext.Interceptions);
    }
}
