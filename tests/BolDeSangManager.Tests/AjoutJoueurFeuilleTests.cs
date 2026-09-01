using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Correction d'une feuille par un commissaire : AJOUTER un joueur oublié à la
/// saisie initiale.
///
/// C'était un trou réel — l'écran d'édition ne savait qu'éditer les lignes
/// existantes. Un joueur oublié ne pouvait donc jamais recevoir son XP, et ses
/// actions (TD, éliminations, déviations, agressions) n'entraient jamais dans le
/// barème de points de la ligue.
/// </summary>
public class AjoutJoueurFeuilleTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private const string CommissaireId = "comm-ajout-test";

    private static MatchService CreateMatchService(ApplicationDbContext db)
    {
        var settings = new SettingsService(db);
        return new(db, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance), settings);
    }

    private static LeagueService CreateLeagueService(ApplicationDbContext db) =>
        new(db, NullLogger<LeagueService>.Instance, new StubAuth(),
            new StaffService(db, NullLogger<StaffService>.Instance));

    /// <summary>Ligue lancée, un match, DEUX joueurs par équipe (un saisi, un oublié).</summary>
    private async Task<(int ligueId, int matchId, int domId, int extId,
        int jDomSaisi, int jDomOublie, int jExt)> SetupAsync()
    {
        await using var db = _factory.CreateContext();

        var comm = DataSeeder.CreateUser("ajcomm");
        comm.Id = CommissaireId;
        var c1 = DataSeeder.CreateUser("ajc1");
        var c2 = DataSeeder.CreateUser("ajc2");
        db.Users.AddRange(comm, c1, c2);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (tt, pos) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, comm.Id, LeagueStatus.EnCours);

        var div = new Division { LeagueId = ligue.Id, Nom = "D1", Ordre = 1 };
        db.Divisions.Add(div);
        await db.SaveChangesAsync();

        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, c1.Id, tt.Id, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, c2.Id, tt.Id, "Ext");
        dom.DivisionId = div.Id;
        ext.DivisionId = div.Id;
        await db.SaveChangesAsync();

        var jSaisi  = await DataSeeder.SeedPlayerAsync(db, dom.Id, pos.Id, "Saisi", 1);
        var jOublie = await DataSeeder.SeedPlayerAsync(db, dom.Id, pos.Id, "Oublié", 2);
        var jExt    = await DataSeeder.SeedPlayerAsync(db, ext.Id, pos.Id, "Ext", 1);
        var match   = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id, div.Id);

        return (ligue.Id, match.Id, dom.Id, ext.Id, jSaisi.Id, jOublie.Id, jExt.Id);
    }

    /// <summary>Feuille initiale : 3-1 domicile, mais UN SEUL joueur domicile déclaré.</summary>
    private async Task SaisirFeuilleIncompleteAsync(int matchId, int jDomSaisi, int jExt)
    {
        await using var db = _factory.CreateContext();
        await CreateMatchService(db).SaisirFeuilleMatchAsync(matchId,
            new MatchSheet
            {
                MatchId = matchId, SaisiParId = CommissaireId,
                TouchdownsDomicile = 3, TouchdownsExterieur = 1, NombreDeTours = 11
            },
            [
                new MatchPlayerRecord
                {
                    TeamPlayerId = jDomSaisi, EstCoteDomicile = true,
                    Touchdowns = 3, EliminationsInfligees = 1
                },
                new MatchPlayerRecord
                {
                    TeamPlayerId = jExt, EstCoteDomicile = false, Touchdowns = 1
                }
            ],
            CommissaireId);
    }

    /// <summary>Le barème de l'association : les actions valent des points de classement.</summary>
    private async Task AppliquerBaremeAvecBonusAsync(int ligueId)
    {
        await using var db = _factory.CreateContext();
        await CreateLeagueService(db).ModifierBaremePointsAsync(ligueId,
            new BaremePoints
            {
                Victoire = 2000, Nul = 1500, Defaite = 1000,
                ParTouchdown = 5, ParElimination = 2, ParInterception = 1,
                ParPasse = 1, ParDeviation = 1, ParAgression = 1
            },
            [new PalierPointsLigue { JusquAuTour = 12, PointsVictoire = 3000, PointsNul = 1500, PointsDefaite = 0 }],
            CommissaireId);
    }

    /// <summary>Rejoue la feuille en AJOUTANT le joueur oublié, comme le fait l'écran.</summary>
    private async Task AjouterLeJoueurOublieAsync(
        int matchId, int jDomSaisi, int jDomOublie, int jExt, int deviations, int agressions)
    {
        await using var db = _factory.CreateContext();
        var match = await db.Matches.Include(m => m.Feuille).FirstAsync(m => m.Id == matchId);

        await CreateMatchService(db).ModifierFeuilleAsync(matchId,
            new MatchSheet
            {
                TouchdownsDomicile = 3, TouchdownsExterieur = 1,
                EliminationsDomicile = 1, EliminationsExterieur = 0,
                NombreDeTours = match.Feuille!.NombreDeTours
            },
            [
                new MatchPlayerRecord
                {
                    TeamPlayerId = jDomSaisi, EstCoteDomicile = true,
                    Touchdowns = 3, EliminationsInfligees = 1
                },
                // ← la ligne ajoutée par le commissaire
                new MatchPlayerRecord
                {
                    TeamPlayerId = jDomOublie, EstCoteDomicile = true,
                    Deviations = deviations, Agressions = agressions
                },
                new MatchPlayerRecord
                {
                    TeamPlayerId = jExt, EstCoteDomicile = false, Touchdowns = 1
                }
            ]);
    }

    // ── Le trou comblé ────────────────────────────────────────────────────────

    [Fact]
    public async Task AjouterUnJoueurOublie_CreeSaLigneSurLaFeuille()
    {
        var (_, matchId, _, _, jSaisi, jOublie, jExt) = await SetupAsync();
        await SaisirFeuilleIncompleteAsync(matchId, jSaisi, jExt);

        await using (var db = _factory.CreateContext())
        {
            var f = await db.MatchSheets.Include(x => x.RecordsJoueurs).FirstAsync(x => x.MatchId == matchId);
            Assert.Equal(2, f.RecordsJoueurs.Count);
            Assert.DoesNotContain(f.RecordsJoueurs, r => r.TeamPlayerId == jOublie);
        }

        await AjouterLeJoueurOublieAsync(matchId, jSaisi, jOublie, jExt, deviations: 2, agressions: 3);

        await using (var db = _factory.CreateContext())
        {
            var f = await db.MatchSheets.Include(x => x.RecordsJoueurs).FirstAsync(x => x.MatchId == matchId);
            Assert.Equal(3, f.RecordsJoueurs.Count);

            var ligne = f.RecordsJoueurs.Single(r => r.TeamPlayerId == jOublie);
            Assert.Equal(2, ligne.Deviations);
            Assert.Equal(3, ligne.Agressions);
            Assert.True(ligne.EstCoteDomicile);
        }
    }

    [Fact]
    public async Task AjouterUnJoueur_LuiCrediteSonXp()
    {
        // Décision produit explicite : l'XP est recréditée pour que tout reste
        // cohérent. Un joueur ajouté avec 2 TD gagne bien ses 6 PSP.
        var (_, matchId, _, _, jSaisi, jOublie, jExt) = await SetupAsync();
        await SaisirFeuilleIncompleteAsync(matchId, jSaisi, jExt);

        await using (var db = _factory.CreateContext())
            Assert.Equal(0, (await db.TeamPlayers.FindAsync(jOublie))!.PointsStarPlayer);

        await using (var db = _factory.CreateContext())
        {
            var match = await db.Matches.Include(m => m.Feuille).FirstAsync(m => m.Id == matchId);
            await CreateMatchService(db).ModifierFeuilleAsync(matchId,
                new MatchSheet
                {
                    TouchdownsDomicile = 5, TouchdownsExterieur = 1,
                    NombreDeTours = match.Feuille!.NombreDeTours
                },
                [
                    new MatchPlayerRecord { TeamPlayerId = jSaisi,  EstCoteDomicile = true, Touchdowns = 3 },
                    new MatchPlayerRecord { TeamPlayerId = jOublie, EstCoteDomicile = true, Touchdowns = 2 },
                    new MatchPlayerRecord { TeamPlayerId = jExt,    EstCoteDomicile = false, Touchdowns = 1 }
                ]);
        }

        await using (var db = _factory.CreateContext())
        {
            // Barème LRB par défaut : 3 XP par touchdown.
            Assert.Equal(6, (await db.TeamPlayers.FindAsync(jOublie))!.PointsStarPlayer);
            // Le joueur déjà présent n'est pas crédité DEUX fois : l'ancienne
            // feuille a été inversée avant réécriture.
            Assert.Equal(9, (await db.TeamPlayers.FindAsync(jSaisi))!.PointsStarPlayer);
        }
    }

    [Fact]
    public async Task AjouterUnJoueur_SesActionsEntrentDansLeClassement()
    {
        // La raison d'être du correctif : sans la ligne du joueur oublié, ses
        // déviations et agressions ne rapportaient aucun point de classement.
        var (ligueId, matchId, domId, _, jSaisi, jOublie, jExt) = await SetupAsync();
        await SaisirFeuilleIncompleteAsync(matchId, jSaisi, jExt);
        await AppliquerBaremeAvecBonusAsync(ligueId);

        int avant;
        await using (var db = _factory.CreateContext())
        {
            avant = (await db.Teams.FindAsync(domId))!.PointsLigue;
            // 3000 (victoire avant le 13e) + 3 TD×5 + 1 élim×2 = 3017
            Assert.Equal(3017, avant);
        }

        await AjouterLeJoueurOublieAsync(matchId, jSaisi, jOublie, jExt, deviations: 2, agressions: 3);

        await using (var db = _factory.CreateContext())
        {
            // + 2 déviations + 3 agressions = +5
            Assert.Equal(avant + 5, (await db.Teams.FindAsync(domId))!.PointsLigue);
        }
    }

    [Fact]
    public async Task ApresAjout_SaisieEtRecalculCompletRestentDAccord()
    {
        // Le filet du lot « barème » : quoi qu'ait fait la correction, le
        // recalcul complet doit retomber sur le même total.
        var (ligueId, matchId, domId, extId, jSaisi, jOublie, jExt) = await SetupAsync();
        await SaisirFeuilleIncompleteAsync(matchId, jSaisi, jExt);
        await AppliquerBaremeAvecBonusAsync(ligueId);
        await AjouterLeJoueurOublieAsync(matchId, jSaisi, jOublie, jExt, deviations: 2, agressions: 3);

        int filDom, filExt;
        await using (var db = _factory.CreateContext())
        {
            filDom = (await db.Teams.FindAsync(domId))!.PointsLigue;
            filExt = (await db.Teams.FindAsync(extId))!.PointsLigue;
        }

        await using (var db = _factory.CreateContext())
            await CreateLeagueService(db).RecalculerClassementAsync(ligueId);

        await using (var db = _factory.CreateContext())
        {
            Assert.Equal(filDom, (await db.Teams.FindAsync(domId))!.PointsLigue);
            Assert.Equal(filExt, (await db.Teams.FindAsync(extId))!.PointsLigue);
        }
    }

    [Fact]
    public async Task RetirerUneLigne_SupprimeLeJoueurEtSonXp()
    {
        // Symétrique de l'ajout : la croix doit vraiment retirer le joueur.
        var (_, matchId, _, _, jSaisi, jOublie, jExt) = await SetupAsync();
        await SaisirFeuilleIncompleteAsync(matchId, jSaisi, jExt);
        await AjouterLeJoueurOublieAsync(matchId, jSaisi, jOublie, jExt, deviations: 2, agressions: 3);

        await using (var db = _factory.CreateContext())
        {
            var match = await db.Matches.Include(m => m.Feuille).FirstAsync(m => m.Id == matchId);
            await CreateMatchService(db).ModifierFeuilleAsync(matchId,
                new MatchSheet
                {
                    TouchdownsDomicile = 3, TouchdownsExterieur = 1,
                    NombreDeTours = match.Feuille!.NombreDeTours
                },
                [
                    new MatchPlayerRecord { TeamPlayerId = jSaisi, EstCoteDomicile = true, Touchdowns = 3 },
                    new MatchPlayerRecord { TeamPlayerId = jExt,   EstCoteDomicile = false, Touchdowns = 1 }
                ]);
        }

        await using (var db = _factory.CreateContext())
        {
            var f = await db.MatchSheets.Include(x => x.RecordsJoueurs).FirstAsync(x => x.MatchId == matchId);
            Assert.Equal(2, f.RecordsJoueurs.Count);
            Assert.DoesNotContain(f.RecordsJoueurs, r => r.TeamPlayerId == jOublie);
            Assert.Equal(0, (await db.TeamPlayers.FindAsync(jOublie))!.PointsStarPlayer);
        }
    }

    private class StubAuth : IAuthorizationService
    {
        public Task<bool> EstAdminAsync(string userId) => Task.FromResult(true);
        public Task<bool> EstGrandCommissaireAsync(string userId) => Task.FromResult(true);
        public Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId) => Task.FromResult(true);
        public Task<bool> PeutGererLigueAsync(string userId, int ligueId) => Task.FromResult(true);
        public Task<bool> PeutEditerDonneesAsync(string userId) => Task.FromResult(true);
        public Task<bool> PeutGererSettingsAsync(string userId) => Task.FromResult(true);
    }
}
