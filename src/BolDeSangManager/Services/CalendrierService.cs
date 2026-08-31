using System.Text;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Services;

/// <summary>
/// Génération de fichiers iCalendar (.ics) pour les matchs programmés (#1),
/// afin de les importer dans Google Agenda, Outlook, Apple Calendrier…
///
/// Format RFC 5545. Points d'attention respectés ici :
///  • les dates sont émises en UTC (suffixe Z) — c'est l'agenda du destinataire
///    qui les affiche dans son fuseau ;
///  • les caractères \ ; , et les retours à la ligne doivent être échappés dans
///    les champs texte, sinon le fichier est refusé ;
///  • les lignes de plus de 75 octets doivent être repliées ;
///  • chaque événement a un UID stable, pour qu'un ré-import mette à jour
///    l'événement existant au lieu d'en créer un doublon.
/// </summary>
public class CalendrierService
{
    /// <summary>Durée par défaut d'un match, faute de mieux.</summary>
    private static readonly TimeSpan DureeMatch = TimeSpan.FromHours(2);

    private const string Domaine = "boldesang-manager";

    /// <summary>Fichier .ics pour un match unique.</summary>
    public byte[] GenererIcs(Match match) => GenererIcs([match], "Match");

    /// <summary>
    /// Fichier .ics regroupant plusieurs matchs (calendrier d'une ligue ou d'une équipe).
    /// Les matchs sans date programmée sont ignorés : un événement sans date n'a pas de sens.
    /// </summary>
    public byte[] GenererIcs(IEnumerable<Match> matchs, string nomCalendrier)
    {
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append($"PRODID:-//{Domaine}//FR\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:PUBLISH\r\n");
        Ligne(sb, "X-WR-CALNAME", nomCalendrier);

        foreach (var m in matchs.Where(m => m.DateProgrammee.HasValue))
        {
            var debut = DateTime.SpecifyKind(m.DateProgrammee!.Value, DateTimeKind.Utc);
            var titre = $"{m.EquipeDomicile?.Nom ?? "?"} vs {m.EquipeExterieur?.Nom ?? "?"}";

            var description = new StringBuilder();
            var ligue = m.Division?.League?.Nom;
            if (!string.IsNullOrWhiteSpace(ligue)) description.Append($"Ligue : {ligue}\n");
            description.Append(m.Ronde >= 100
                ? $"Play-off — Tour {m.Ronde - 99}"
                : $"Ronde {m.Ronde}");

            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append($"UID:match-{m.Id}@{Domaine}\r\n");
            sb.Append($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}\r\n");
            sb.Append($"DTSTART:{debut:yyyyMMdd'T'HHmmss'Z'}\r\n");
            sb.Append($"DTEND:{debut.Add(DureeMatch):yyyyMMdd'T'HHmmss'Z'}\r\n");
            Ligne(sb, "SUMMARY", titre);
            Ligne(sb, "DESCRIPTION", description.ToString());
            if (!string.IsNullOrWhiteSpace(m.Lieu)) Ligne(sb, "LOCATION", m.Lieu);
            sb.Append("END:VEVENT\r\n");
        }

        sb.Append("END:VCALENDAR\r\n");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// URL « Ajouter à Google Agenda » pré-remplie pour UN match.
    ///
    /// Complément du .ics : le coach clique, Google ouvre le formulaire de
    /// création d'événement déjà rempli, il valide. Aucune mise à jour ensuite —
    /// c'est une copie ponctuelle, exactement comme un .ics importé.
    ///
    /// Format des dates imposé par Google : <c>YYYYMMDDTHHMMSSZ/YYYYMMDDTHHMMSSZ</c>
    /// en UTC. Les valeurs sont échappées pour l'URL (<see cref="Uri.EscapeDataString"/>),
    /// faute de quoi un nom d'équipe contenant « &amp; » tronquerait les paramètres suivants.
    ///
    /// Retourne <c>null</c> quand le match n'a pas de date : un événement sans
    /// date n'a pas de sens, l'appelant n'affiche alors pas le lien.
    /// </summary>
    public string? UrlGoogleAgenda(Match m)
    {
        if (!m.DateProgrammee.HasValue) return null;

        var debut = DateTime.SpecifyKind(m.DateProgrammee.Value, DateTimeKind.Utc);
        var fin = debut.Add(DureeMatch);

        var titre = $"{m.EquipeDomicile?.Nom ?? "?"} vs {m.EquipeExterieur?.Nom ?? "?"}";

        var description = new StringBuilder();
        var ligue = m.Division?.League?.Nom;
        if (!string.IsNullOrWhiteSpace(ligue)) description.Append($"Ligue : {ligue}\n");
        description.Append(m.Ronde >= 100
            ? $"Play-off — Tour {m.Ronde - 99}"
            : $"Ronde {m.Ronde}");

        var parametres = new List<string>
        {
            "action=TEMPLATE",
            $"text={Uri.EscapeDataString(titre)}",
            $"dates={debut:yyyyMMdd'T'HHmmss'Z'}/{fin:yyyyMMdd'T'HHmmss'Z'}",
            $"details={Uri.EscapeDataString(description.ToString())}",
        };

        if (!string.IsNullOrWhiteSpace(m.Lieu))
            parametres.Add($"location={Uri.EscapeDataString(m.Lieu)}");

        return "https://calendar.google.com/calendar/render?" + string.Join("&", parametres);
    }

    /// <summary>Écrit une propriété texte : échappement puis repli des lignes longues.</summary>
    private static void Ligne(StringBuilder sb, string propriete, string valeur)
        => sb.Append(Replier($"{propriete}:{Echapper(valeur)}")).Append("\r\n");

    /// <summary>
    /// Échappe les caractères réservés du format (RFC 5545 §3.3.11).
    /// L'antislash doit être traité en premier, sinon on ré-échappe les autres.
    /// </summary>
    private static string Echapper(string valeur) => valeur
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    /// <summary>
    /// Replie les lignes de plus de 75 octets (RFC 5545 §3.1) : la suite est
    /// préfixée d'une espace. Le découpage se fait sur les octets UTF-8 pour ne
    /// pas couper un caractère accentué en deux.
    /// </summary>
    private static string Replier(string ligne)
    {
        const int max = 74;
        if (Encoding.UTF8.GetByteCount(ligne) <= max) return ligne;

        var sb = new StringBuilder();
        var courant = new StringBuilder();
        var octets = 0;
        var premiere = true;

        foreach (var c in ligne)
        {
            var taille = Encoding.UTF8.GetByteCount([c]);
            if (octets + taille > (premiere ? max : max - 1))
            {
                sb.Append(courant).Append("\r\n ");
                courant.Clear();
                octets = 0;
                premiere = false;
            }
            courant.Append(c);
            octets += taille;
        }
        sb.Append(courant);
        return sb.ToString();
    }
}
