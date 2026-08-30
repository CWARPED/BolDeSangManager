using BolDeSangManager.Components.Pages;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;

namespace BolDeSangManager.Tests;

/// <summary>
/// Sélection des matchs affichés sur l'ACCUEIL.
///
/// Règle produit : le prochain match de chaque ligue, PLUS tout match qui
/// attend encore une action de ce coach. Sans cette seconde moitié, une feuille
/// à confirmer ou un après-match à remplir disparaîtrait de l'accueil et le
/// coach n'aurait plus aucun rappel de ce qu'on attend de lui.
///
/// Tests purs : aucune base, aucun rendu Razor — la sélection est une fonction
/// statique sur des listes en mémoire, exactement pour permettre ceci.
/// </summary>
public class AccueilSelectionTests
{
    private const string MoiId = "coach-moi";
    private const string AutreId = "coach-autre";

    private static int _prochainId = 1;

    /// <summary>Fabrique un match rattaché à une ligue, avec mon équipe à domicile.</summary>
    private static Match Match(
        int ligueId,
        int ronde,
        MatchStatus statut,
        int equipeDomicileId = 10,
        int equipeExterieurId = 20,
        MatchSheet? feuille = null)
    {
        var m = new Match
        {
            Id = _prochainId++,
            Ronde = ronde,
            Statut = statut,
            EquipeDomicileId = equipeDomicileId,
            EquipeExterieurId = equipeExterieurId,
            Feuille = feuille,
            Division = new Division { Id = ligueId * 100, LeagueId = ligueId, Nom = "D1" }
        };
        return m;
    }

    private static HashSet<int> MesEquipes => [10];

    private static List<Match> Selection(IEnumerable<Match> matchs) =>
        Home.SelectionnerPourAccueil(matchs, MoiId, MesEquipes);

    // ── Le cœur de la règle ──────────────────────────────────────────────────

    [Fact]
    public void DeuxLigues_UneSeuleCarteParLigue()
    {
        var matchs = new List<Match>
        {
            Match(1, 1, MatchStatus.Programme),
            Match(1, 2, MatchStatus.Programme),
            Match(1, 3, MatchStatus.AJouer),
            Match(2, 1, MatchStatus.Programme),
            Match(2, 2, MatchStatus.Programme),
        };

        var res = Selection(matchs);

        Assert.Equal(2, res.Count);
        Assert.Single(res, m => m.Division!.LeagueId == 1);
        Assert.Single(res, m => m.Division!.LeagueId == 2);
    }

    [Fact]
    public void ProchainMatch_EstLaRondeLaPlusBasse()
    {
        // volontairement dans le désordre : on doit trier, pas prendre le premier
        var matchs = new List<Match>
        {
            Match(1, 5, MatchStatus.Programme),
            Match(1, 2, MatchStatus.Programme),
            Match(1, 9, MatchStatus.AJouer),
        };

        var res = Selection(matchs);

        Assert.Single(res);
        Assert.Equal(2, res[0].Ronde);
    }

    [Fact]
    public void LigueSansMatchAVenir_NeProduitAucuneCarte()
    {
        var matchs = new List<Match>
        {
            Match(1, 1, MatchStatus.Termine),
            Match(1, 2, MatchStatus.Concede),
        };

        Assert.Empty(Selection(matchs));
    }

    [Fact]
    public void AucunMatch_NePlantePas()
    {
        Assert.Empty(Selection([]));
    }

    // ── L'exception « une action est attendue de moi » ───────────────────────

    [Fact]
    public void FeuilleSaisieParLAdversaire_ResteAffichee_EnPlusDuProchain()
    {
        var aConfirmer = Match(1, 1, MatchStatus.FeuilleEnSaisie,
            feuille: new MatchSheet { SaisiParId = AutreId });
        var prochain = Match(1, 2, MatchStatus.Programme);

        var res = Selection([aConfirmer, prochain]);

        Assert.Equal(2, res.Count);
        Assert.Contains(res, m => m.Id == aConfirmer.Id);
        Assert.Contains(res, m => m.Id == prochain.Id);
    }

    [Fact]
    public void FeuilleQueJAiSaisieMoiMeme_NestPasUneActionEnAttente()
    {
        // j'ai saisi, c'est l'adversaire qui doit confirmer : rien à faire de mon côté
        var enAttenteDeLautre = Match(1, 1, MatchStatus.FeuilleEnSaisie,
            feuille: new MatchSheet { SaisiParId = MoiId });
        var prochain = Match(1, 2, MatchStatus.Programme);

        var res = Selection([enAttenteDeLautre, prochain]);

        Assert.Single(res);
        Assert.Equal(prochain.Id, res[0].Id);
    }

    [Fact]
    public void ApresMatchNonValideDeMonCote_ResteAffiche()
    {
        var apresMatch = Match(1, 1, MatchStatus.ValidationCompetences,
            feuille: new MatchSheet
            {
                ApresMatchDomicileValide = false,
                ApresMatchExterieurValide = true
            });

        var res = Selection([apresMatch]);

        Assert.Single(res);
    }

    /// <summary>
    /// Le cas qui motive l'affinage : mon après-match est fait, j'attends
    /// l'adversaire. Rien ne m'est demandé, la carte ne doit pas rester collée
    /// sur l'accueil.
    /// </summary>
    [Fact]
    public void ApresMatchDejaValideDeMonCote_DisparaitDeLAccueil()
    {
        var apresMatch = Match(1, 1, MatchStatus.ValidationCompetences,
            feuille: new MatchSheet
            {
                ApresMatchDomicileValide = true,   // mon équipe (10) est à domicile
                ApresMatchExterieurValide = false
            });

        Assert.Empty(Selection([apresMatch]));
    }

    [Fact]
    public void ApresMatchDeLautreCote_QuandJeSuisALexterieur()
    {
        // mon équipe (10) joue à l'extérieur cette fois
        var apresMatch = Match(1, 1, MatchStatus.ValidationCompetences,
            equipeDomicileId: 20, equipeExterieurId: 10,
            feuille: new MatchSheet
            {
                ApresMatchDomicileValide = true,
                ApresMatchExterieurValide = false   // moi : pas encore validé
            });

        Assert.Single(Selection([apresMatch]));
    }

    // ── Ordre d'affichage ────────────────────────────────────────────────────

    [Fact]
    public void LesActionsEnAttentePassentAvantLeProchainMatch()
    {
        var prochain = Match(1, 2, MatchStatus.Programme);
        var aConfirmer = Match(1, 1, MatchStatus.FeuilleEnSaisie,
            feuille: new MatchSheet { SaisiParId = AutreId });

        // fourni dans le mauvais ordre exprès
        var res = Selection([prochain, aConfirmer]);

        Assert.Equal(2, res.Count);
        Assert.Equal(aConfirmer.Id, res[0].Id);
    }

    [Fact]
    public void AucunDoublon_QuandUnMatchEstALaFoisProchainEtEnAttente()
    {
        // FeuilleEnSaisie n'est pas « à jouer », mais on vérifie le DistinctBy
        var m = Match(1, 1, MatchStatus.FeuilleEnSaisie,
            feuille: new MatchSheet { SaisiParId = AutreId });

        var res = Selection([m, m]);

        Assert.Single(res);
    }

    // ── Prédicat AttendUneActionDe, isolé ────────────────────────────────────

    [Fact]
    public void AttendUneAction_FauxPourUnMatchTermineOuAJouer()
    {
        Assert.False(DisplayHelpers.AttendUneActionDe(
            Match(1, 1, MatchStatus.Termine), MoiId, MesEquipes));
        Assert.False(DisplayHelpers.AttendUneActionDe(
            Match(1, 1, MatchStatus.Programme), MoiId, MesEquipes));
        Assert.False(DisplayHelpers.AttendUneActionDe(
            Match(1, 1, MatchStatus.Concede), MoiId, MesEquipes));
    }
}
