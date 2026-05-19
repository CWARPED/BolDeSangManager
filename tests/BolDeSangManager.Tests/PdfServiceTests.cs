using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;

namespace BolDeSangManager.Tests;

public class PdfServiceTests
{
    private static Team MinimalTeam(string nom = "Les Testeurs") => new()
    {
        Nom = nom,
        Tresorerie = 500_000,
        NombreRelances = 2,
        FansDevoues = 1,
        NombreCoachsAssistants = 0,
        NombreCheerleaders = 0,
        Apothicaire = false
    };

    private static Team TeamAvecIcones()
    {
        var equipe = MinimalTeam("Team Icônes Test");
        equipe.Joueurs =
        [
            new TeamPlayer
            {
                Numero = 1,
                Nom = "Joueur ManqueSuivant",
                ManqueSuivantMatch = true,      // → ⚕
                PointsStarPlayer = 0,
                Blessures = []
            },
            new TeamPlayer
            {
                Numero = 2,
                Nom = "Joueur Sequel",
                ManqueSuivantMatch = false,
                PointsStarPlayer = 0,
                Blessures = [new PlayerInjury { Type = InjuryType.BlessurePersistante }] // → ✦
            },
            new TeamPlayer
            {
                Numero = 3,
                Nom = "Joueur PSP",
                ManqueSuivantMatch = false,
                PointsStarPlayer = 6,           // → ★
                Blessures = []
            },
            new TeamPlayer
            {
                Numero = 4,
                Nom = "Joueur Toutes",
                ManqueSuivantMatch = true,      // → ⚕
                PointsStarPlayer = 8,           // → ★
                Blessures = [new PlayerInjury { Type = InjuryType.BlessurePersistante }] // → ✦
            },
        ];
        return equipe;
    }

    [Fact]
    public void GenererFeuilleEquipe_SansMatch_RetournePdfNonVide()
    {
        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(MinimalTeam(), inclureDescriptionsCompetences: false);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000, "Le PDF devrait peser plus de 1 Ko");
    }

    [Fact]
    public void GenererFeuilleEquipe_AvecDescriptionsCompetences_RetournePdfNonVide()
    {
        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(MinimalTeam(), inclureDescriptionsCompetences: true);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000);
    }

    [Fact]
    public void GenererFeuilleEquipe_AvecMatchEtUrlExterne_InclutQrCode()
    {
        var equipe = MinimalTeam();
        var matchProchain = new Match
        {
            Id = 42,
            Ronde = 1,
            EquipeDomicileId = equipe.Id,
            EquipeExterieurId = 99
        };

        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(
            equipe,
            inclureDescriptionsCompetences: false,
            matchProchain: matchProchain,
            urlExterne: "https://boldesang.example.com");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000);
    }

    [Fact]
    public void GenererFeuilleEquipe_MatchSansUrl_NePasPlanterSansQrCode()
    {
        var equipe = MinimalTeam();
        var matchProchain = new Match { Id = 1, Ronde = 2, EquipeDomicileId = equipe.Id };

        var svc = new PdfService();
        // urlExterne null → pas de QR code, mais ne doit pas lever d'exception
        var bytes = svc.GenererFeuilleEquipe(equipe, false, matchProchain, urlExterne: null);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000);
    }

    [Fact]
    public void GenererFeuilleEquipe_AvecIcones_NeLevePasException()
    {
        // Vérifie que les 3 icônes (⚕ ✦ ★) ne causent pas de crash PDF
        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(TeamAvecIcones(), inclureDescriptionsCompetences: false);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 5_000, "PDF avec joueurs doit peser > 5 Ko");
    }

    [Fact]
    public void GenererFeuilleEquipe_AvecIcones_FontesNotoReferenceesDansPdf()
    {
        // QuestPDF embarque les fontes dans le PDF — leurs noms apparaissent dans le binaire.
        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(TeamAvecIcones(), inclureDescriptionsCompetences: false);

        var pdfText = System.Text.Encoding.Latin1.GetString(bytes);
        // Les deux fontes Noto doivent toutes deux être référencées
        Assert.Contains("NotoSansSymbols", pdfText);
        // Le PDF doit référencer au moins 2 familles Noto différentes (Symbols et Symbols2)
        var countNoto = System.Text.RegularExpressions.Regex
            .Matches(pdfText, @"NotoSansSymbols\d*-Regular").Count;
        Assert.True(countNoto >= 2, $"Attendu ≥2 références Noto dans le PDF, trouvé {countNoto}");
    }
}
