using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Filtrage des compétences de l'après-match selon les accès du poste (R7).
/// </summary>
public class AccesCompetencesHelpersTests
{
    // Catégories : 1 = Générale, 2 = Agilité, 3 = Mutation
    private static Skill S(int id, string nom, int categorieId, bool elite = false) => new()
    {
        Id = id,
        Nom = nom,
        SkillCategoryDefId = categorieId,
        SkillCategoryDef = new SkillCategoryDef { Id = categorieId, Nom = $"Cat{categorieId}" },
        EstElite = elite
    };

    private static List<Skill> Catalogue() =>
    [
        S(1, "Blocage",    1),           // Générale
        S(2, "Plaquage",   1),           // Générale
        S(3, "Esquive",    2),           // Agilité
        S(4, "Tentacules", 3),           // Mutation
        S(5, "Coup Bas",   1, elite: true),  // Générale, mais Élite
    ];

    /// <summary>Poste avec Générale en principal et Agilité en secondaire.</summary>
    private static List<PlayerPositionCategoryAccess> AccesTroisQuart() =>
    [
        new() { SkillCategoryDefId = 1, EstPrincipale = true },
        new() { SkillCategoryDefId = 2, EstPrincipale = false },
    ];

    [Fact]
    public void Filtrer_NeProposeQueLesCategoriesAccessibles()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart());

        var noms = r.Select(c => c.Skill.Nom).ToList();
        Assert.Contains("Blocage", noms);      // Générale = accès principal
        Assert.Contains("Esquive", noms);      // Agilité = accès secondaire
        Assert.DoesNotContain("Tentacules", noms);  // Mutation : hors accès
    }

    [Fact]
    public void Filtrer_MasqueLesCompetencesElite()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart());

        Assert.DoesNotContain("Coup Bas", r.Select(c => c.Skill.Nom));
    }

    [Fact]
    public void Filtrer_DeduitPrincipalDepuisLAcces()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart());

        // Générale est un accès principal → +20k
        Assert.True(r.First(c => c.Skill.Nom == "Blocage").EstPrincipale);
        // Agilité est secondaire → +40k
        Assert.False(r.First(c => c.Skill.Nom == "Esquive").EstPrincipale);
    }

    [Fact]
    public void Filtrer_AvecHorsAcces_ReaffichhTout()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart(), inclureHorsAcces: true);

        var noms = r.Select(c => c.Skill.Nom).ToList();
        Assert.Contains("Tentacules", noms);   // Mutation redevient visible
        Assert.Contains("Coup Bas", noms);     // Élite aussi
        Assert.Equal(5, r.Count);
    }

    [Fact]
    public void Filtrer_AvecHorsAcces_MarqueCeQuiSortDesAcces()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart(), inclureHorsAcces: true);

        Assert.True(r.First(c => c.Skill.Nom == "Tentacules").HorsAcces);
        Assert.False(r.First(c => c.Skill.Nom == "Blocage").HorsAcces);
    }

    [Fact]
    public void Filtrer_HorsAcces_NeCompteJamaisCommePrincipale()
    {
        // une compétence hors accès ne peut pas ouvrir droit à la hausse réduite
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart(), inclureHorsAcces: true);

        Assert.False(r.First(c => c.Skill.Nom == "Tentacules").EstPrincipale);
    }

    [Fact]
    public void Filtrer_ClasseLesAccessiblesAvantLesHorsAcces()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), AccesTroisQuart(), inclureHorsAcces: true);

        var premierHorsAcces = r.FindIndex(c => c.HorsAcces);
        var dernierAccessible = r.FindLastIndex(c => !c.HorsAcces);

        Assert.True(dernierAccessible < premierHorsAcces);
    }

    [Fact]
    public void Filtrer_PosteSansAcces_NeProposeRien()
    {
        // cas réel constaté en base : un poste dont les deux champs étaient vides
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), []);

        Assert.Empty(r);
    }

    [Fact]
    public void Filtrer_PosteSansAcces_AvecHorsAcces_ProposeTout()
    {
        var r = AccesCompetencesHelpers.Filtrer(Catalogue(), [], inclureHorsAcces: true);

        Assert.Equal(5, r.Count);
        Assert.All(r, c => Assert.True(c.HorsAcces));
    }

    // ── Recalcul serveur de la hausse de valeur ───────────────────────────────

    [Fact]
    public void EstAccesPrincipal_VraiPourUneCategoriePrincipale()
    {
        Assert.True(AccesCompetencesHelpers.EstAccesPrincipal(S(1, "Blocage", 1), AccesTroisQuart()));
    }

    [Fact]
    public void EstAccesPrincipal_FauxPourUnAccesSecondaire()
    {
        Assert.False(AccesCompetencesHelpers.EstAccesPrincipal(S(3, "Esquive", 2), AccesTroisQuart()));
    }

    [Fact]
    public void EstAccesPrincipal_FauxHorsAcces()
    {
        // le coach a coché « hors accès » : la hausse doit rester celle d'un secondaire
        Assert.False(AccesCompetencesHelpers.EstAccesPrincipal(S(4, "Tentacules", 3), AccesTroisQuart()));
    }
}
