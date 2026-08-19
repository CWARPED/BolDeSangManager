using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Conversion des accès historiques « GAF » vers des catégories (R2b) et rendu compact.
/// </summary>
public class CategoryAccessHelpersTests
{
    private static List<SkillCategoryDef> Standard() =>
    [
        new() { Id = 1, Nom = "Agilité",   Code = "A" },
        new() { Id = 2, Nom = "Force",     Code = "F" },
        new() { Id = 3, Nom = "Générale",  Code = "G" },
        new() { Id = 4, Nom = "Mutation",  Code = "M" },
        new() { Id = 5, Nom = "Passe",     Code = "P" },
        new() { Id = 6, Nom = "Scélérate", Code = "S" },
    ];

    [Fact]
    public void ResoudreCodes_ConvertitChaqueLettre()
    {
        var res = CategoryAccessHelpers.ResoudreCodesHistoriques("GAF", Standard());
        Assert.Equal(["Générale", "Agilité", "Force"], res.Select(c => c.Nom));
    }

    [Fact]
    public void ResoudreCodes_InsensibleALaCasse()
    {
        var res = CategoryAccessHelpers.ResoudreCodesHistoriques("ga", Standard());
        Assert.Equal(["Générale", "Agilité"], res.Select(c => c.Nom));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ResoudreCodes_VideDonneListeVide(string? codes)
    {
        Assert.Empty(CategoryAccessHelpers.ResoudreCodesHistoriques(codes, Standard()));
    }

    [Fact]
    public void ResoudreCodes_IgnoreLesCodesInconnus()
    {
        // 'Z' n'existe pas : on garde les autres au lieu de tout perdre
        var res = CategoryAccessHelpers.ResoudreCodesHistoriques("GZA", Standard());
        Assert.Equal(["Générale", "Agilité"], res.Select(c => c.Nom));
    }

    [Fact]
    public void ResoudreCodes_DedoublonneEtIgnoreSeparateurs()
    {
        var res = CategoryAccessHelpers.ResoudreCodesHistoriques("G,G A", Standard());
        Assert.Equal(["Générale", "Agilité"], res.Select(c => c.Nom));
    }

    [Fact]
    public void FormatAcces_TriteParNomEtSepare()
    {
        var cats = Standard().Where(c => c.Code is "G" or "A" or "F").ToList();
        Assert.Equal("A · F · G", CategoryAccessHelpers.FormatAcces(cats));
    }

    [Fact]
    public void FormatAcces_VideDonneTiret()
    {
        Assert.Equal("—", CategoryAccessHelpers.FormatAcces([]));
    }
}
