using BolDeSangManager.Services;

namespace BolDeSangManager.Tests;

/// <summary>
/// L'étoile JPV sur une PSP tapée à la main (feuille de match et correction).
/// Bug de la ligue DB GBLR : le TD y est tapé à 5 (barème : 3), donc presque
/// toutes les lignes sont figées ; déplacer le JPV laissait alors le bonus sur
/// l'ancien joueur et ne le donnait pas au nouveau.
/// </summary>
public class BaremePspJpvTests
{
    private static readonly BaremePsp Bareme = new() { BonusMvp = 4 };

    [Fact]
    public void LigneNonSaisie_ResteNulle_ElleSuitDejaLeBareme()
    {
        Assert.Null(Bareme.AjusterSaisiePourJpv(null, devientJpv: true));
        Assert.Null(Bareme.AjusterSaisiePourJpv(null, devientJpv: false));
    }

    [Fact]
    public void LigneSaisie_DevientJpv_RecoitLeBonus()
        => Assert.Equal(9, Bareme.AjusterSaisiePourJpv(5, devientJpv: true));   // TD tapé 5 + JPV

    [Fact]
    public void LigneSaisie_PerdLeJpv_RendLeBonus()
        => Assert.Equal(5, Bareme.AjusterSaisiePourJpv(9, devientJpv: false));

    [Fact]
    public void LigneSaisie_PerdLeJpv_NeDescendPasSousZero()
        => Assert.Equal(0, Bareme.AjusterSaisiePourJpv(2, devientJpv: false));

    [Fact]
    public void LeBonusEstCeluiDuBaremeDeLaLigue()
        => Assert.Equal(8, new BaremePsp { BonusMvp = 3 }.AjusterSaisiePourJpv(5, devientJpv: true));
}
