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
    public void GenererReglement_AvecMarkdown_RetournePdfNonVide()
    {
        var svc = new PdfService();
        var ligue = new League
        {
            Nom = "Ligue de la Saison Sanglante",
            Reglement = """
                # Règlement officiel

                ## 1. Déroulement

                Les matchs se jouent le **samedi**, sauf accord entre coaches.

                - Chaque équipe dispose de 4 minutes par tour
                - Les dés doivent être lancés sur la table
                - Un dé sorti est **relancé**

                ## 2. Sanctions

                > Tout comportement antisportif est sanctionné par le commissaire.

                1. Premier avertissement
                2. Match perdu par forfait

                ---

                *Règlement adopté en assemblée générale.*
                """
        };

        var bytes = svc.GenererReglement(ligue);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000, "Le PDF devrait peser plus de 1 Ko");
    }

    [Fact]
    public void GenererReglement_SansReglement_RetourneQuandMemeUnPdf()
    {
        // le bouton peut être cliqué sur une ligue neuve : pas d'exception attendue
        var svc = new PdfService();
        var bytes = svc.GenererReglement(new League { Nom = "Ligue vide" });

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 500);
    }

    [Fact]
    public void GenererReglement_MarkdownExotique_NeFaitPasEchouerLeRendu()
    {
        // tableaux, images, HTML : hors du sous-ensemble supporté, doivent
        // dégrader en texte simple sans planter
        var svc = new PdfService();
        var ligue = new League
        {
            Nom = "Ligue test",
            Reglement = """
                | Poste | Coût |
                |---|---|
                | Blitzeur | 85k |

                ![image](https://exemple.fr/image.png)

                <div>html brut</div>

                Texte avec des *étoiles* non fermées ** et des _underscores_.
                """
        };

        var bytes = svc.GenererReglement(ligue);
        Assert.True(bytes.Length > 500);
    }

    [Fact]
    public void GenererFeuilleEquipe_EnPaysage_RetournePdfNonVide()
    {
        var svc = new PdfService();
        var bytes = svc.GenererFeuilleEquipe(MinimalTeam(),
            inclureDescriptionsCompetences: false, paysage: true);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1_000);
    }

    [Fact]
    public void GenererFeuilleEquipe_PortraitEtPaysage_ProduisentDesDocumentsDifferents()
    {
        var svc = new PdfService();
        var equipe = MinimalTeam();

        var portrait = svc.GenererFeuilleEquipe(equipe, false, paysage: false);
        var paysage  = svc.GenererFeuilleEquipe(equipe, false, paysage: true);

        // même contenu, mise en page différente : les octets ne peuvent pas coïncider
        Assert.NotEqual(portrait.Length, paysage.Length);
    }

    [Fact]
    public void GenererFeuilleEquipe_RappelCompetencesEnPaysage_TientSurMoinsDePagesQuEnPortrait()
    {
        // Signalé par l'utilisateur : en paysage le rappel des compétences
        // s'affichait sur une colonne unique, laissant la moitié droite vide et
        // débordant sur une page supplémentaire. En deux colonnes, le paysage ne
        // doit jamais coûter PLUS de pages que le portrait à contenu égal.
        var svc = new PdfService();

        // Assez de compétences pour que la mise en page compte vraiment : sur une
        // seule colonne ce volume déborde en page 2, sur deux colonnes il tient.
        var equipe = EquipeAvecCompetences(nbCategories: 3, parCategorie: 6);

        var paysage = svc.GenererFeuilleEquipe(equipe, inclureDescriptionsCompetences: true, paysage: true);

        Assert.Equal(1, NombreDePages(paysage));
    }

    [Fact]
    public void GenererFeuilleEquipe_PaysageAvecUneSeuleCategorie_NeCassePas()
    {
        // Cas limite : la répartition en deux colonnes découpe par catégorie.
        // Avec une seule catégorie il n'y a rien à répartir — on doit retomber
        // proprement sur le rendu une colonne, sans exception.
        var svc = new PdfService();
        var equipe = EquipeAvecCompetences(nbCategories: 1, parCategorie: 5);

        var bytes = svc.GenererFeuilleEquipe(equipe, inclureDescriptionsCompetences: true, paysage: true);

        Assert.True(bytes.Length > 1_000);
        Assert.Equal(1, NombreDePages(bytes));
    }

    /// <summary>Compte les objets /Type /Page du PDF (suffisant pour ces tests).</summary>
    private static int NombreDePages(byte[] pdf)
    {
        var texte = System.Text.Encoding.Latin1.GetString(pdf);
        return System.Text.RegularExpressions.Regex.Matches(texte, @"/Type\s*/Page[^s]").Count;
    }

    /// <summary>Équipe dont les joueurs portent des compétences réparties en catégories.</summary>
    private static Team EquipeAvecCompetences(int nbCategories, int parCategorie)
    {
        var equipe = MinimalTeam();
        var joueur = new TeamPlayer
        {
            Numero = 1,
            Nom = "Porteur de compétences",
            PointsStarPlayer = 0,
            Blessures = [],
            Competences = []
        };
        equipe.Joueurs = [joueur];

        var categories = new[] { SkillCategory.Agilite, SkillCategory.Force, SkillCategory.Generale };

        for (var c = 0; c < nbCategories; c++)
        {
            for (var i = 0; i < parCategorie; i++)
            {
                joueur.Competences.Add(new TeamPlayerSkill
                {
                    EstCompetenceDepart = false,
                    Skill = new Skill
                    {
                        Id = c * 100 + i,
                        Nom = $"Comp {c}-{i}",
                        Description = "Description courte de la compétence, une ligne.",
                        Categorie = categories[c % categories.Length]
                    }
                });
            }
        }

        return equipe;
    }

    [Fact]
    public void GenererFeuilleEquipe_ParDefaut_ResteEnPortrait()
    {
        // #4 : l'orientation est une option, le portrait reste le comportement par défaut
        var svc = new PdfService();
        var equipe = MinimalTeam();

        var implicite = svc.GenererFeuilleEquipe(equipe, false);
        var portrait  = svc.GenererFeuilleEquipe(equipe, false, paysage: false);

        Assert.Equal(portrait.Length, implicite.Length);
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
