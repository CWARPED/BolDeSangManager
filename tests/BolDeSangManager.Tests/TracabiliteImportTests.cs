using BolDeSangManager.Services;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Contrôle de traçabilité à l'import (F3).
///
/// L'enjeu : empêcher qu'un fichier ancien écrase silencieusement des
/// corrections récentes. Le contrôle doit avertir sans jamais bloquer.
/// </summary>
public class TracabiliteImportTests
{
    [Fact]
    public void FichierPlusRecent_EstAcceptéSansConfirmation()
    {
        var c = TracabiliteImport.Verifier(revisionFichier: 7, revisionBase: 5);

        Assert.Equal(TracabiliteImport.Verdict.PlusRecent, c.Verdict);
        Assert.False(c.DemandeConfirmation);
        Assert.Contains("plus récent", c.Message);
    }

    [Fact]
    public void FichierPlusAncien_DeclencheUnAvertissement()
    {
        // le cas dangereux : on écraserait des corrections récentes
        var c = TracabiliteImport.Verifier(revisionFichier: 3, revisionBase: 5);

        Assert.Equal(TracabiliteImport.Verdict.PlusAncien, c.Verdict);
        Assert.True(c.DemandeConfirmation);
        Assert.Contains("risque d'écraser", c.Message);
    }

    [Fact]
    public void FichierPlusAncien_NEstPasBloque_SeulementSignale()
    {
        // revenir en arrière peut être volontaire : on avertit, on n'interdit pas
        var c = TracabiliteImport.Verifier(revisionFichier: 1, revisionBase: 9);

        Assert.Equal(TracabiliteImport.Verdict.PlusAncien, c.Verdict);
        Assert.True(c.DemandeConfirmation);
    }

    [Fact]
    public void MemeRevision_SignaleUnFichierDejaIntegre()
    {
        var c = TracabiliteImport.Verifier(revisionFichier: 5, revisionBase: 5);

        Assert.Equal(TracabiliteImport.Verdict.Identique, c.Verdict);
        Assert.True(c.DemandeConfirmation);
        Assert.Contains("déjà été intégré", c.Message);
    }

    [Fact]
    public void FichierSansRevision_ResteImportable_MaisSignale()
    {
        // rétrocompatibilité : les JSON exportés avant F3 n'ont pas ces champs
        var c = TracabiliteImport.Verifier(revisionFichier: null, revisionBase: 5);

        Assert.Equal(TracabiliteImport.Verdict.Inconnue, c.Verdict);
        Assert.False(c.DemandeConfirmation);   // on ne bloque pas l'ancien format
        Assert.Contains("ne porte pas de numéro de révision", c.Message);
    }

    [Fact]
    public void BasePremierExport_AccepteNimporteQuelFichier()
    {
        // version jamais exportée : révision 0 en base
        var c = TracabiliteImport.Verifier(revisionFichier: 1, revisionBase: 0);

        Assert.Equal(TracabiliteImport.Verdict.PlusRecent, c.Verdict);
        Assert.False(c.DemandeConfirmation);
    }

    [Fact]
    public void LaDateDExport_EstReprisedDansLeMessage()
    {
        var quand = new DateTime(2026, 8, 17, 10, 4, 0, DateTimeKind.Utc);
        var c = TracabiliteImport.Verifier(7, 5, quand);

        Assert.Contains("17/08/2026", c.Message);
    }

    [Fact]
    public void SansDate_LeMessageResteLisible()
    {
        var c = TracabiliteImport.Verifier(7, 5, null);

        Assert.DoesNotContain("exporté le", c.Message);
        Assert.Contains("révision 7", c.Message);
    }

    [Theory]
    [InlineData(6, 5, TracabiliteImport.Verdict.PlusRecent)]
    [InlineData(5, 5, TracabiliteImport.Verdict.Identique)]
    [InlineData(4, 5, TracabiliteImport.Verdict.PlusAncien)]
    [InlineData(0, 0, TracabiliteImport.Verdict.Identique)]
    public void LesComparaisonsCouvrentLesTroisCas(int fichier, int baseRev,
        TracabiliteImport.Verdict attendu)
    {
        Assert.Equal(attendu, TracabiliteImport.Verifier(fichier, baseRev).Verdict);
    }
}
