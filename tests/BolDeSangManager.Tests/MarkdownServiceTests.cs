using BolDeSangManager.Services;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Rendu markdown des règlements de ligue (R5), avec l'accent sur l'assainissement :
/// le texte est écrit par un commissaire mais lu par tous les participants.
/// </summary>
public class MarkdownServiceTests
{
    private readonly MarkdownService _svc = new();

    // ── Rendu nominal ─────────────────────────────────────────────────────────

    [Fact]
    public void VersHtml_TitresEtParagraphes()
    {
        var html = _svc.VersHtml("# Règlement\n\nLes matchs se jouent le samedi.");

        Assert.Contains("<h1", html);
        Assert.Contains("Règlement", html);
        Assert.Contains("<p>", html);
    }

    [Fact]
    public void VersHtml_ListesEtGras()
    {
        var html = _svc.VersHtml("- premier\n- **second**");

        Assert.Contains("<ul>", html);
        Assert.Contains("<li>", html);
        Assert.Contains("<strong>second</strong>", html);
    }

    [Fact]
    public void VersHtml_Tableaux()
    {
        var html = _svc.VersHtml("| Poste | Coût |\n|---|---|\n| Blitzeur | 85k |");

        Assert.Contains("<table>", html);
        Assert.Contains("Blitzeur", html);
    }

    [Fact]
    public void VersHtml_TexteVide_RetourneChaineVide()
    {
        Assert.Equal(string.Empty, _svc.VersHtml(null));
        Assert.Equal(string.Empty, _svc.VersHtml("   "));
    }

    // ── Sécurité : le règlement est rendu en HTML chez les autres joueurs ──────

    [Fact]
    public void VersHtml_BaliseScript_EstEchappeeEtNonInjectee()
    {
        var html = _svc.VersHtml("Bonjour <script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void VersHtml_HtmlBrut_EstNeutralise()
    {
        var html = _svc.VersHtml("<div onclick=\"voler()\">clique ici</div>");

        Assert.DoesNotContain("<div", html);
        Assert.Contains("&lt;div", html);
    }

    [Fact]
    public void VersHtml_LienJavascript_EstNeutralise()
    {
        // Markdig laisse passer ce schéma : c'est notre filet qui doit agir
        var html = _svc.VersHtml("[clique](javascript:alert('xss'))");

        Assert.DoesNotContain("javascript:", html);
    }

    [Fact]
    public void VersHtml_ImageAvecDataUri_EstNeutralisee()
    {
        var html = _svc.VersHtml("![img](data:text/html;base64,PHNjcmlwdD4=)");

        Assert.DoesNotContain("data:text/html", html);
    }

    [Fact]
    public void VersHtml_LienNormal_EstPreserve()
    {
        // l'assainissement ne doit pas casser les liens légitimes
        var html = _svc.VersHtml("[le site](https://boldesang.fr/regles)");

        Assert.Contains("https://boldesang.fr/regles", html);
    }

    // ── Texte brut (aperçus) ──────────────────────────────────────────────────

    [Fact]
    public void VersTexte_RetireLeBalisage()
    {
        var texte = _svc.VersTexte("# Titre\n\nUn **mot** important.");

        Assert.DoesNotContain("#", texte);
        Assert.DoesNotContain("**", texte);
        Assert.Contains("important", texte);
    }
}
