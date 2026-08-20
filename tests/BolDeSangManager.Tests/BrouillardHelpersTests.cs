using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Mode brouillard (#2) : un coach ne voit que son prochain match programmé
/// et l'ensemble des matchs déjà joués.
/// </summary>
public class BrouillardHelpersTests
{
    // Équipes : 1 & 2 appartiennent au coach testé ; 3, 4, 5, 6 aux autres.
    private static readonly HashSet<int> MesEquipes = [1];

    private static Match M(int id, int ronde, int dom, int ext,
        MatchStatus statut = MatchStatus.Programme, int? score = null) =>
        new()
        {
            Id = id, Ronde = ronde,
            EquipeDomicileId = dom, EquipeExterieurId = ext,
            Statut = statut, ScoreDomicile = score
        };

    /// <summary>
    /// Calendrier type : ronde 1 jouée, rondes 2 et 3 à venir.
    /// Le coach (équipe 1) joue en ronde 1, 2 et 3.
    /// </summary>
    private static List<Match> Calendrier() =>
    [
        M(10, 1, 1, 3, MatchStatus.Termine, score: 2),   // mon match joué
        M(11, 1, 4, 5, MatchStatus.Termine, score: 1),   // match joué des autres
        M(20, 2, 1, 4),                                   // MON prochain match
        M(21, 2, 3, 5),                                   // à venir, autres
        M(30, 3, 1, 5),                                   // mon match ultérieur
        M(31, 3, 4, 6),                                   // à venir, autres
    ];

    [Fact]
    public void ModeDesactive_ToutEstVisible()
    {
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: false, estCommissaire: false);

        Assert.Equal(6, visibles.Count);
    }

    [Fact]
    public void Commissaire_VoitToutMemeEnBrouillard()
    {
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: true, estCommissaire: true);

        Assert.Equal(6, visibles.Count);
    }

    [Fact]
    public void Brouillard_MasqueLesMatchsAVenirDesAutres()
    {
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: true, estCommissaire: false);

        var ids = visibles.Select(m => m.Id).ToHashSet();
        Assert.DoesNotContain(21, ids);   // ronde 2, autres équipes
        Assert.DoesNotContain(31, ids);   // ronde 3, autres équipes
    }

    [Fact]
    public void Brouillard_LaisseVoirTousLesMatchsJoues()
    {
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: true, estCommissaire: false);

        var ids = visibles.Select(m => m.Id).ToHashSet();
        Assert.Contains(10, ids);   // le mien
        Assert.Contains(11, ids);   // celui des autres → visible aussi
    }

    [Fact]
    public void Brouillard_LaisseVoirMonProchainMatch()
    {
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.Contains(20, visibles.Select(m => m.Id));
    }

    [Fact]
    public void Brouillard_MasqueMesPropresMatchsUlterieurs()
    {
        // c'est le point clé : on ne prépare pas la ronde 3 en jouant la ronde 2
        var visibles = BrouillardHelpers.FiltrerVisibles(
            Calendrier(), MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.DoesNotContain(30, visibles.Select(m => m.Id));
    }

    [Fact]
    public void Brouillard_LeProchainMatchEstCeluiDeLaRondeLaPlusBasse()
    {
        // calendrier volontairement désordonné
        List<Match> desordre = [M(30, 3, 1, 5), M(20, 2, 1, 4), M(40, 4, 1, 6)];

        var visibles = BrouillardHelpers.FiltrerVisibles(
            desordre, MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.Single(visibles);
        Assert.Equal(20, visibles[0].Id);
    }

    [Fact]
    public void Brouillard_FeuilleEnSaisieCompteCommeJoue()
    {
        // le match est engagé : son résultat n'est plus une information à protéger
        List<Match> cal = [M(50, 2, 3, 4, MatchStatus.FeuilleEnSaisie)];

        var visibles = BrouillardHelpers.FiltrerVisibles(
            cal, MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.Single(visibles);
    }

    [Fact]
    public void Brouillard_CoachSansMatchAVenir_VoitSeulementLesMatchsJoues()
    {
        List<Match> cal =
        [
            M(10, 1, 1, 3, MatchStatus.Termine, score: 2),
            M(21, 2, 3, 5),   // à venir, sans moi
        ];

        var visibles = BrouillardHelpers.FiltrerVisibles(
            cal, MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.Single(visibles);
        Assert.Equal(10, visibles[0].Id);
    }

    [Fact]
    public void EstVisible_ProtegeLAccesDirectAUnMatchMasque()
    {
        var cal = Calendrier();
        var matchDesAutres = cal.First(m => m.Id == 31);

        Assert.False(BrouillardHelpers.EstVisible(
            matchDesAutres, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));

        Assert.True(BrouillardHelpers.EstVisible(
            matchDesAutres, cal, MesEquipes, modeBrouillard: false, estCommissaire: false));
    }

    [Fact]
    public void PremiereRonde_LeProchainMatchResteVisible()
    {
        // pas de « ronde précédente » : la règle ne doit rien bloquer d'anormal
        List<Match> cal = [M(20, 1, 1, 4), M(21, 1, 3, 5)];

        var visibles = BrouillardHelpers.FiltrerVisibles(
            cal, MesEquipes, modeBrouillard: true, estCommissaire: false);

        Assert.Single(visibles);
        Assert.Equal(20, visibles[0].Id);
    }

    [Fact]
    public void Playoffs_MemeRegle_LeProchainTourResteVisible()
    {
        // les rondes >= 100 sont les play-offs : rien de spécifique, même règle
        List<Match> cal =
        [
            M(60, 100, 1, 3, MatchStatus.Termine, score: 3),
            M(61, 101, 1, 4),   // ma demi-finale
            M(62, 101, 5, 6),   // l'autre demi-finale
        ];

        var visibles = BrouillardHelpers.FiltrerVisibles(
            cal, MesEquipes, modeBrouillard: true, estCommissaire: false);

        var ids = visibles.Select(m => m.Id).ToHashSet();
        Assert.Contains(60, ids);
        Assert.Contains(61, ids);
        Assert.DoesNotContain(62, ids);
    }

    // ─── Fiche d'équipe du prochain adversaire ────────────────────────────────
    // Les fiches sont publiques par choix : SEUL le prochain adversaire est
    // masqué, et seulement en mode brouillard.

    [Fact]
    public void FicheEquipe_ProchainAdversaire_EstMasquee()
    {
        List<Match> cal =
        [
            M(10, 1, 1, 3, MatchStatus.Termine, score: 2),   // déjà joué contre 3
            M(11, 2, 1, 4),                                   // prochain : contre 4
            M(12, 3, 1, 5),                                   // plus tard : contre 5
        ];

        Assert.False(BrouillardHelpers.PeutVoirFicheEquipe(
            4, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_AdversaireDejaAffronte_ResteVisible()
    {
        List<Match> cal =
        [
            M(10, 1, 1, 3, MatchStatus.Termine, score: 2),
            M(11, 2, 1, 4),
        ];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            3, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_AdversaireFutur_ResteVisible()
    {
        // seul le PROCHAIN est masqué : les rencontres ultérieures ne le sont pas
        List<Match> cal =
        [
            M(11, 2, 1, 4),
            M(12, 3, 1, 5),
        ];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            5, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_EquipeNonImpliquee_ResteVisible()
    {
        List<Match> cal =
        [
            M(11, 2, 1, 4),
            M(12, 2, 5, 6),   // match entre deux autres équipes
        ];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            6, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_SansBrouillard_ToutEstVisible()
    {
        List<Match> cal = [M(11, 2, 1, 4)];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            4, cal, MesEquipes, modeBrouillard: false, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_Commissaire_VoitTout()
    {
        List<Match> cal = [M(11, 2, 1, 4)];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            4, cal, MesEquipes, modeBrouillard: true, estCommissaire: true));
    }

    [Fact]
    public void FicheEquipe_SaPropreEquipe_ToujoursVisible()
    {
        // un coach engageant deux équipes qui s'affrontent doit voir les siennes
        var deuxEquipes = new HashSet<int> { 1, 2 };
        List<Match> cal = [M(11, 2, 1, 2)];

        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            2, cal, deuxEquipes, modeBrouillard: true, estCommissaire: false));
    }

    [Fact]
    public void FicheEquipe_MatchEnCoursDeSaisie_ResteMasquee()
    {
        // tant que le résultat n'est pas acquis, l'effectif adverse reste caché
        List<Match> cal = [M(11, 2, 1, 4, MatchStatus.FeuilleEnSaisie)];

        Assert.True(BrouillardHelpers.EstJoue(cal[0]));   // considéré comme joué
        Assert.True(BrouillardHelpers.PeutVoirFicheEquipe(
            4, cal, MesEquipes, modeBrouillard: true, estCommissaire: false));
    }
}
