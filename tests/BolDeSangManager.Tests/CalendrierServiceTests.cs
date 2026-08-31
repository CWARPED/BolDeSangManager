using System.Text;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Export iCalendar des matchs programmés (#1). Le format est strict : un
/// échappement manqué ou une ligne trop longue fait rejeter le fichier par
/// Google Agenda ou Outlook.
/// </summary>
public class CalendrierServiceTests
{
    private readonly CalendrierService _svc = new();

    private static Match M(int id = 1, string dom = "Les Marteaux", string ext = "Les Charognards",
        DateTime? date = null, string lieu = "", int ronde = 1) => new()
    {
        Id = id,
        Ronde = ronde,
        DateProgrammee = date ?? new DateTime(2026, 9, 12, 18, 30, 0, DateTimeKind.Utc),
        Lieu = lieu,
        EquipeDomicile = new Team { Nom = dom },
        EquipeExterieur = new Team { Nom = ext },
        Division = new Division { League = new League { Nom = "Ligue de la Saison Sanglante" } }
    };

    private string Texte(Match m) => Encoding.UTF8.GetString(_svc.GenererIcs(m));

    // ── Structure du fichier ──────────────────────────────────────────────────

    [Fact]
    public void Ics_ContientLEnveloppeVCalendar()
    {
        var ics = Texte(M());

        Assert.StartsWith("BEGIN:VCALENDAR", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("VERSION:2.0", ics);
    }

    [Fact]
    public void Ics_ContientUnEvenementAvecTitreEtDates()
    {
        var ics = Texte(M());

        Assert.Contains("BEGIN:VEVENT", ics);
        Assert.Contains("SUMMARY:Les Marteaux vs Les Charognards", ics);
        Assert.Contains("DTSTART:20260912T183000Z", ics);
        Assert.Contains("DTEND:20260912T203000Z", ics);   // +2 h
        Assert.Contains("END:VEVENT", ics);
    }

    [Fact]
    public void Ics_UtiliseUnUidStable_PourEviterLesDoublonsAuReimport()
    {
        var premier = Texte(M(id: 42));
        var second  = Texte(M(id: 42));

        Assert.Contains("UID:match-42@boldesang-manager", premier);
        Assert.Contains("UID:match-42@boldesang-manager", second);
    }

    [Fact]
    public void Ics_LesLignesSeTerminentParCRLF()
    {
        // le format l'impose ; un simple \n est refusé par certains agendas
        var ics = Texte(M());
        var lignes = ics.Split("\r\n");

        Assert.True(lignes.Length > 5);
        Assert.DoesNotContain(ics.Replace("\r\n", ""), "\n");
    }

    // ── Lieu ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Ics_IncluteLeLieuQuandIlEstRenseigne()
    {
        Assert.Contains("LOCATION:Le Repaire du Troll", Texte(M(lieu: "Le Repaire du Troll")));
    }

    [Fact]
    public void Ics_OmetLeLieuQuandIlEstVide()
    {
        Assert.DoesNotContain("LOCATION:", Texte(M(lieu: "")));
    }

    // ── Échappement (RFC 5545 §3.3.11) ────────────────────────────────────────

    [Fact]
    public void Ics_EchappeLesVirgulesEtPointsVirgules()
    {
        var ics = Texte(M(lieu: "Bar du Coin, 12 rue des Trolls; étage 2"));

        Assert.Contains("\\,", ics);
        Assert.Contains("\\;", ics);
        // la virgule brute ne doit pas subsister dans la valeur
        Assert.DoesNotContain("LOCATION:Bar du Coin,", ics);
    }

    [Fact]
    public void Ics_EchappeLesAntislashs()
    {
        var ics = Texte(M(lieu: @"Chez Bob\Sous-sol"));

        Assert.Contains(@"\\", ics);
    }

    [Fact]
    public void Ics_EchappeLesRetoursLigne()
    {
        var ics = Texte(M(lieu: "Salle A\nPorte 3"));

        Assert.Contains("\\n", ics);
        // le retour brut ne doit pas casser la structure de la propriété
        Assert.DoesNotContain("LOCATION:Salle A\nPorte", ics);
    }

    // ── Repli des lignes longues (RFC 5545 §3.1) ──────────────────────────────

    [Fact]
    public void Ics_ReplieLesLignesDePlusDe75Octets()
    {
        var lieuLong = new string('A', 200);
        var ics = Texte(M(lieu: lieuLong));

        foreach (var ligne in ics.Split("\r\n"))
            Assert.True(Encoding.UTF8.GetByteCount(ligne) <= 75,
                $"Ligne trop longue ({Encoding.UTF8.GetByteCount(ligne)} octets) : {ligne[..Math.Min(40, ligne.Length)]}…");
    }

    [Fact]
    public void Ics_ReplieSansCouperLesCaracteresAccentues()
    {
        // découper sur les octets naïvement casserait un é en deux
        var ics = Texte(M(lieu: string.Concat(Enumerable.Repeat("é", 100))));

        Assert.Contains("é", ics);
        Assert.DoesNotContain("\uFFFD", ics);   // pas de caractère de remplacement
    }

    // ── Calendrier multi-matchs ───────────────────────────────────────────────

    [Fact]
    public void Ics_Multi_ContientUnEvenementParMatch()
    {
        var ics = Encoding.UTF8.GetString(_svc.GenererIcs(
            [M(1), M(2, dom: "Rivaux A", ext: "Rivaux B")], "Ligue test"));

        Assert.Equal(2, ics.Split("BEGIN:VEVENT").Length - 1);
    }

    [Fact]
    public void Ics_Multi_IgnoreLesMatchsSansDate()
    {
        var sansDate = M(3);
        sansDate.DateProgrammee = null;

        var ics = Encoding.UTF8.GetString(_svc.GenererIcs([M(1), sansDate], "Ligue test"));

        Assert.Equal(1, ics.Split("BEGIN:VEVENT").Length - 1);
    }

    [Fact]
    public void Ics_Multi_SansAucuneDate_ResteUnFichierValide()
    {
        var sansDate = M(1);
        sansDate.DateProgrammee = null;

        var ics = Encoding.UTF8.GetString(_svc.GenererIcs([sansDate], "Ligue vide"));

        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.Contains("END:VCALENDAR", ics);
        Assert.DoesNotContain("BEGIN:VEVENT", ics);
    }

    [Fact]
    public void Ics_MentionneLaRondeEtLaLigueDansLaDescription()
    {
        var ics = Texte(M(ronde: 3));

        Assert.Contains("Ronde 3", ics);
        Assert.Contains("Ligue de la Saison Sanglante", ics);
    }

    [Fact]
    public void Ics_NommeCorrectementLesToursDePlayoff()
    {
        var ics = Texte(M(ronde: 101));

        Assert.Contains("Play-off", ics);
        Assert.DoesNotContain("Ronde 101", ics);
    }

    // ── Lien « Ajouter à Google Agenda » (option B) ───────────────────────────
    //
    // Google impose des dates UTC au format compact YYYYMMDDTHHMMSSZ séparées
    // par une barre oblique. Tout le reste est du texte libre, qui DOIT être
    // échappé pour l'URL : un « & » dans un nom d'équipe couperait la chaîne de
    // paramètres, et Google recevrait un événement tronqué sans rien signaler.

    [Fact]
    public void Google_PointeVersLeFormulaireDeCreationDEvenement()
    {
        var url = _svc.UrlGoogleAgenda(M())!;

        Assert.StartsWith("https://calendar.google.com/calendar/render?", url);
        Assert.Contains("action=TEMPLATE", url);
    }

    [Fact]
    public void Google_UtiliseLesDatesUtcAuFormatCompact_DebutEtFinSeparesParUneBarre()
    {
        var url = _svc.UrlGoogleAgenda(M())!;

        // 18h30 UTC + 2 h de durée par défaut
        Assert.Contains("dates=20260912T183000Z/20260912T203000Z", url);
    }

    [Fact]
    public void Google_ReprendLeTitreDuMatch_EchappePourLUrl()
    {
        var url = _svc.UrlGoogleAgenda(M())!;

        Assert.Contains("text=Les%20Marteaux%20vs%20Les%20Charognards", url);
        // L'espace brut casserait l'URL dans un attribut href.
        Assert.DoesNotContain("text=Les Marteaux", url);
    }

    [Fact]
    public void Google_EchappeLesCaracteresQuiCasseraientLaChaineDeParametres()
    {
        var url = _svc.UrlGoogleAgenda(M(dom: "Tom & Jerry", ext: "A=B"))!;

        Assert.DoesNotContain("Tom & Jerry", url);
        Assert.Contains("Tom%20%26%20Jerry", url);
        Assert.Contains("A%3DB", url);

        // Un seul paramètre « text » : le & du nom n'a pas créé de paramètre fantôme.
        Assert.Equal(1, url.Split("text=").Length - 1);
    }

    [Fact]
    public void Google_TransmetLeLieuQuandIlExiste_EtLOmetSinon()
    {
        var avecLieu = _svc.UrlGoogleAgenda(M(lieu: "Taverne du Nain, Lyon"))!;
        var sansLieu = _svc.UrlGoogleAgenda(M())!;

        Assert.Contains("location=Taverne%20du%20Nain%2C%20Lyon", avecLieu);
        Assert.DoesNotContain("location=", sansLieu);
    }

    [Fact]
    public void Google_DecritLaLigueEtLaRondeDansLesDetails()
    {
        var url = _svc.UrlGoogleAgenda(M(ronde: 3))!;

        Assert.Contains("details=", url);
        Assert.Contains(Uri.EscapeDataString("Ronde 3"), url);
        Assert.Contains(Uri.EscapeDataString("Ligue de la Saison Sanglante"), url);
    }

    [Fact]
    public void Google_NommeCorrectementLesToursDePlayoff()
    {
        var url = _svc.UrlGoogleAgenda(M(ronde: 101))!;

        Assert.Contains(Uri.EscapeDataString("Play-off"), url);
        Assert.DoesNotContain(Uri.EscapeDataString("Ronde 101"), url);
    }

    [Fact]
    public void Google_SansDateProgrammee_NeProduitAucunLien()
    {
        var sansDate = M();
        sansDate.DateProgrammee = null;

        Assert.Null(_svc.UrlGoogleAgenda(sansDate));
    }
}
