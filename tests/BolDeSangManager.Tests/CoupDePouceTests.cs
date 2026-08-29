using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Tests;

/// <summary>
/// Coups de pouce et star players : deux catalogues INFORMATIFS rattachés à une
/// version de règles. Aucune mécanique — ils s'affichent pour que les coaches
/// comparent leurs VEA et décident à la table.
///
/// Le point porteur de logique est l'accès aux star players : on recoupe les
/// ligues thématiques du star player avec celles de la race. Les deux côtés
/// pointent vers le CATALOGUE de ligues, pas vers du texte libre — une faute
/// de frappe rendait auparavant un star player introuvable sans explication.
/// </summary>
public class CoupDePouceTests
{
    private static ThemedLeague Ligue(int id, string nom) =>
        new() { Id = id, Nom = nom };

    private static StarPlayer Star(params int[] liguesIds)
    {
        var star = new StarPlayer { Nom = "Griff Oberwald", Cout = 280_000 };
        foreach (var id in liguesIds)
            star.Ligues.Add(new StarPlayerThemedLeague { StarPlayerId = 1, ThemedLeagueId = id });
        return star;
    }

    // ── Accès des star players ───────────────────────────────────────────────

    /// <summary>
    /// Choix produit : sans ligue rattachée, le star player est ouvert à
    /// TOUTES les équipes. Un oubli de saisie le rend visible plutôt
    /// qu'introuvable sans explication.
    /// </summary>
    [Fact]
    public void SansLigue_LeStarPlayerEstOuvertATous()
    {
        Assert.True(Star().EstAccessible([1, 2]));
        Assert.True(Star().EstAccessible([]));
    }

    [Fact]
    public void AvecUneLigue_SeulesLesEquipesDeCetteLigueYAccedent()
    {
        var star = Star(1);   // Badlands Brawl

        Assert.True(star.EstAccessible([1, 3]));
        Assert.False(star.EstAccessible([3]));
        Assert.False(star.EstAccessible([]));
    }

    /// <summary>Plusieurs ligues : une seule suffit (le LRB dit « ou »).</summary>
    [Fact]
    public void AvecPlusieursLigues_UneSeuleSuffit()
    {
        var star = Star(1, 2);

        Assert.True(star.EstAccessible([1]));
        Assert.True(star.EstAccessible([2]));
        Assert.False(star.EstAccessible([3]));
    }

    /// <summary>
    /// Tout l'intérêt du catalogue : la comparaison porte sur des identifiants,
    /// donc une différence de casse ou d'orthographe entre deux saisies ne peut
    /// plus rendre un star player indisponible.
    /// </summary>
    [Fact]
    public void LeCatalogueSupprimeLesDivergencesDeSaisie()
    {
        var badlands = Ligue(7, "Badlands Brawl");

        var star = new StarPlayer { Nom = "Grashnak" };
        star.Ligues.Add(new StarPlayerThemedLeague { ThemedLeagueId = badlands.Id });

        // La race pointe vers LA MÊME ligue du catalogue.
        Assert.True(star.EstAccessible([badlands.Id]));
    }

    [Fact]
    public void LesCompetencesSontDecoupeesProprement()
    {
        var star = new StarPlayer { Competences = "Blocage, Esquive ,  , Tacle" };

        Assert.Equal(["Blocage", "Esquive", "Tacle"], star.CompetencesListe);
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
