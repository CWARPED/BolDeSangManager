using BolDeSangManager.Services;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Normalisation des descriptions de compétences pour le PDF.
///
/// Bug signalé : le « Rappel des Compétences » se coupait vers le tiers de la
/// page en laissant les deux tiers droits vides. La cause n'était PAS la mise
/// en page mais la DONNÉE : les descriptions importées du livre de règles
/// portent les sauts de ligne de sa maquette en colonne étroite, et QuestPDF
/// les respecte à la lettre.
/// </summary>
public class PdfTexteFluideTests
{
    /// <summary>Le cas exact du bug : « Esquive » porte 4 sauts de ligne.</summary>
    [Fact]
    public void SautsDeLigneSimples_DeviennentDesEspaces()
    {
        var source = "Une fois par Tour, ce joueur peut relancer un unique Test\n"
                   + "d’Agilité quand il tente d’Esquiver. De plus, cette\n"
                   + "Compétence affecte le résultat Bousculé quand un joueur\n"
                   + "adverse effectue une Action de Blocage contre ce joueur,\n"
                   + "comme décrit en page 38 .";

        var resultat = PdfService.TexteFluide(source);

        Assert.DoesNotContain("\n", resultat);
        Assert.Contains("unique Test d’Agilité quand", resultat);
        Assert.Contains("cette Compétence affecte", resultat);
    }

    /// <summary>
    /// Un saut DOUBLE sépare de vrais paragraphes : tout aplatir collerait les
    /// alinéas des compétences longues.
    /// </summary>
    [Fact]
    public void SautsDoubles_SontPreservesCommeParagraphes()
    {
        var source = "Premier paragraphe sur\ndeux lignes de maquette.\n\n"
                   + "Second paragraphe, lui aussi\nsur deux lignes.";

        var resultat = PdfService.TexteFluide(source);

        Assert.Equal(
            "Premier paragraphe sur deux lignes de maquette.\nSecond paragraphe, lui aussi sur deux lignes.",
            resultat);
    }

    [Fact]
    public void RetoursWindows_SontTraitesCommeLesAutres()
    {
        var resultat = PdfService.TexteFluide("Ligne une\r\nligne deux\r\nligne trois.");

        Assert.Equal("Ligne une ligne deux ligne trois.", resultat);
    }

    [Fact]
    public void EspacesMultiples_SontReduits()
    {
        var resultat = PdfService.TexteFluide("Mot   suivi\n   de   blancs.");

        Assert.Equal("Mot suivi de blancs.", resultat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DescriptionVide_NeLevePas(string? source)
    {
        Assert.Equal(string.Empty, PdfService.TexteFluide(source));
    }

    /// <summary>Une description déjà propre ne doit pas être altérée.</summary>
    [Fact]
    public void TexteDejaFluide_ResteIdentique()
    {
        const string source = "Ce joueur peut relancer un jet d'Esquive raté une fois par activation.";

        Assert.Equal(source, PdfService.TexteFluide(source));
    }
}
