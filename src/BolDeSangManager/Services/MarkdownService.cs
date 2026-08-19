using System.Text.RegularExpressions;
using Markdig;

namespace BolDeSangManager.Services;

/// <summary>
/// Rendu du markdown des règlements de ligue (R5).
///
/// ⚠️ Sécurité : le texte est saisi par des humains de confiance (commissaires),
/// mais il est ensuite rendu en HTML dans le navigateur des autres participants.
/// Un commissaire malveillant — ou un compte compromis — pourrait y glisser du
/// script. Deux garde-fous :
///   1. Markdig est configuré SANS HTML brut : les balises tapées dans le
///      markdown ressortent échappées (&lt;script&gt;) au lieu d'être injectées ;
///   2. un filet de sécurité retire les schémas d'URL dangereux (javascript:,
///      data:) des liens et images, que Markdig laisse passer.
/// </summary>
public partial class MarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()                 // ← les balises HTML brutes sont échappées
        .UseAutoLinks()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseListExtras()
        .Build();

    [GeneratedRegex("""(href|src)\s*=\s*["'](?:\s|&#\w+;)*(?:javascript|data|vbscript)\s*:[^"']*["']""",
        RegexOptions.IgnoreCase)]
    private static partial Regex SchemasDangereux();

    /// <summary>
    /// Convertit du markdown en HTML assaini, prêt à être injecté via MarkupString.
    /// </summary>
    public string VersHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var html = Markdown.ToHtml(markdown, Pipeline);

        // Filet : neutraliser javascript:/data: dans les liens et images.
        return SchemasDangereux().Replace(html, m => $"{m.Groups[1].Value}=\"#\"");
    }

    /// <summary>
    /// Convertit le markdown en texte brut — utilisé pour les aperçus courts.
    /// </summary>
    public string VersTexte(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToPlainText(markdown, Pipeline).Trim();
}
