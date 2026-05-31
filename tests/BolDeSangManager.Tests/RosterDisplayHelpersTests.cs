using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Xunit;

namespace BolDeSangManager.Tests;

public class RosterDisplayHelpersTests
{
    private static PlayerPosition Poste(int id, string nom, string motsCles, int cout = 50_000) =>
        new() { Id = id, Nom = nom, MotsCles = motsCles, Cout = cout };

    [Fact]
    public void AucuneLimite_TousLesPostesEnClassique()
    {
        var postes = new[]
        {
            Poste(1, "Trois-quart Humain", "Trois-quart,Humain"),
            Poste(2, "Blitzer Humain", "Blitzer,Humain", 90_000),
        };

        var (cadres, classiques) = RosterDisplayHelpers.GroupePostesParMotCleLimite(postes, []);

        Assert.Empty(cadres);
        Assert.Equal(2, classiques.Count);
    }

    [Fact]
    public void UneLimite_RegroupeLesPostesConcernes_LesAutresRestentClassiques()
    {
        var postes = new[]
        {
            Poste(1, "Trois-quart Humain", "Trois-quart,Humain"),
            Poste(2, "Gros Bras Ogre", "Gros Bras,Ogre", 140_000),
            Poste(3, "Gros Bras Troll", "Gros Bras,Troll", 115_000),
        };
        var limites = new[] { new TeamTypeKeywordLimit { MotCle = "Gros Bras", Max = 3 } };

        var (cadres, classiques) = RosterDisplayHelpers.GroupePostesParMotCleLimite(postes, limites);

        var cadre = Assert.Single(cadres);
        Assert.Equal("Gros Bras", cadre.MotCle);
        Assert.Equal(3, cadre.Max);
        Assert.Equal(2, cadre.Postes.Count);
        Assert.Contains(cadre.Postes, p => p.Nom == "Gros Bras Ogre");
        Assert.Contains(cadre.Postes, p => p.Nom == "Gros Bras Troll");

        var classique = Assert.Single(classiques);
        Assert.Equal("Trois-quart Humain", classique.Nom);
    }

    [Fact]
    public void PosteAvecDeuxMotsClesLimites_NEstPlaceQueDansLePremierCadre()
    {
        var postes = new[]
        {
            Poste(1, "Momie", "Humain,Mort-Vivant,Bloqueur,Gros Bras"),
            Poste(2, "Trois-quart Squelette", "Trois-quart,Humain,Squelette,Mort-Vivant"),
        };
        var limites = new[]
        {
            new TeamTypeKeywordLimit { MotCle = "Gros Bras", Max = 1 },
            new TeamTypeKeywordLimit { MotCle = "Mort-Vivant", Max = 12 },
        };

        var (cadres, classiques) = RosterDisplayHelpers.GroupePostesParMotCleLimite(postes, limites);

        Assert.Equal(2, cadres.Count);
        // La Momie (Gros Bras + Mort-Vivant) n'apparaît que dans le premier cadre.
        Assert.Contains(cadres[0].Postes, p => p.Nom == "Momie");
        Assert.DoesNotContain(cadres[1].Postes, p => p.Nom == "Momie");
        Assert.Contains(cadres[1].Postes, p => p.Nom == "Trois-quart Squelette");

        // Aucun doublon : chaque poste apparaît au plus une fois dans l'ensemble des cadres.
        var ids = cadres.SelectMany(c => c.Postes).Select(p => p.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
        Assert.Empty(classiques);
    }
}
