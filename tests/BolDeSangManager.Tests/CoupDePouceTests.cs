using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Tests;

/// <summary>
/// Coups de pouce et star players : deux catalogues INFORMATIFS rattachés à une
/// version de règles. Aucune mécanique — ils s'affichent pour que les coaches
/// comparent leurs VEA et décident à la table.
///
/// Le point réellement porteur de logique est l'accès aux star players : on
/// recoupe les ligues du star player avec le champ « Règles ligues thématiques »
/// DÉJÀ présent sur la fiche de race (ReglesSpecialesLigue), renseigné pour 60
/// des 77 races. Aucun nouveau champ n'a donc été ajouté à l'équipe.
/// </summary>
public class CoupDePouceTests
{
    private static StarPlayer Star(string ligues) =>
        new() { Nom = "Griff Oberwald", Cout = 280_000, Ligues = ligues };

    // ── Accès des star players ───────────────────────────────────────────────

    /// <summary>
    /// Choix produit : sans règle renseignée, le star player est ouvert à
    /// TOUTES les équipes. Un oubli de saisie le rend visible plutôt
    /// qu'introuvable sans explication.
    /// </summary>
    [Fact]
    public void SansLigue_LeStarPlayerEstOuvertATous()
    {
        Assert.True(Star("").EstAccessible(["BadlandsBrawl"]));
        Assert.True(Star("").EstAccessible([]));
    }

    [Fact]
    public void AvecUneLigue_SeulesLesEquipesDeCetteLigueYAccedent()
    {
        var star = Star("BadlandsBrawl");

        Assert.True(star.EstAccessible(["BadlandsBrawl", "OldWorldClassic"]));
        Assert.False(star.EstAccessible(["OldWorldClassic"]));
        Assert.False(star.EstAccessible([]));
    }

    /// <summary>Plusieurs règles : une seule suffit (le LRB dit « ou »).</summary>
    [Fact]
    public void AvecPlusieursLigues_UneSeuleSuffit()
    {
        var star = Star("UnderworldChallenge, BadlandsBrawl");

        Assert.True(star.EstAccessible(["BadlandsBrawl"]));
        Assert.True(star.EstAccessible(["UnderworldChallenge"]));
        Assert.False(star.EstAccessible(["OldWorldClassic"]));
    }

    /// <summary>
    /// La saisie est manuelle des DEUX côtés : une différence de casse ou
    /// d'espaces ne doit pas rendre un star player indisponible sans raison
    /// visible pour le commissaire.
    /// </summary>
    [Fact]
    public void LaComparaisonIgnoreLaCasseEtLesEspaces()
    {
        var star = Star("  badlandsBRAWL  ");

        Assert.True(star.EstAccessible(["BadlandsBrawl"]));
        Assert.True(star.EstAccessible(["  BADLANDSBRAWL  "]));
    }

    [Fact]
    public void LesListesSontDecoupeesProprement()
    {
        var star = new StarPlayer
        {
            Competences = "Blocage, Esquive ,  , Tacle",
            Ligues = "A, B ,,C"
        };

        Assert.Equal(["Blocage", "Esquive", "Tacle"], star.CompetencesListe);
        Assert.Equal(["A", "B", "C"], star.LiguesListe);
    }

    // ── Coups de pouce ───────────────────────────────────────────────────────

    /// <summary>
    /// Un coup de pouce est purement descriptif : nom, effet, coût. Rien qui
    /// déclenche une mécanique, contrairement aux règles spéciales à code.
    /// </summary>
    [Fact]
    public void UnCoupDePouceEstPurementDescriptif()
    {
        var cp = new Inducement
        {
            Nom = "Apothicaire itinérant",
            Description = "Soigne une blessure après le match.",
            Cout = 100_000
        };

        Assert.Equal("Apothicaire itinérant", cp.Nom);
        Assert.Equal(100_000, cp.Cout);
    }
}
