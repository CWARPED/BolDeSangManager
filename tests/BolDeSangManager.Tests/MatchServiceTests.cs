using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class MatchServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static MatchService CreateService(ApplicationDbContext db)
    {
        var settings = new SettingsService(db);
        var emailSender = new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance);
        return new(db, NullLogger<MatchService>.Instance, emailSender, settings);
    }

    // ─── Contexte complet pour les tests de feuille ───────────────────────────

    private async Task<(Team domicile, Team exterieur, TeamPlayer joueurDom,
        TeamPlayer joueurExt, Match match, ApplicationUser saisiPar)>
        SetupMatchContextAsync()
    {
        await using var db = _factory.CreateContext();

        var commissaire = DataSeeder.CreateUser("mcomm");
        var coach1 = DataSeeder.CreateUser("mcoach1");
        var coach2 = DataSeeder.CreateUser("mcoach2");
        db.Users.AddRange(commissaire, coach1, coach2);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);

        var div = new Division { LeagueId = ligue.Id, Nom = "Division Unique", Ordre = 1 };
        db.Divisions.Add(div);
        await db.SaveChangesAsync();

        var domicile = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "Dom");
        var exterieur = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "Ext");
        domicile.DivisionId = div.Id;
        exterieur.DivisionId = div.Id;
        await db.SaveChangesAsync();

        var joueurDom = await DataSeeder.SeedPlayerAsync(db, domicile.Id, position.Id, "JD", 1);
        var joueurExt = await DataSeeder.SeedPlayerAsync(db, exterieur.Id, position.Id, "JE", 1);

        var match = await DataSeeder.SeedMatchAsync(db, domicile.Id, exterieur.Id, div.Id);

        return (domicile, exterieur, joueurDom, joueurExt, match, commissaire);
    }

    // ─── CalculerGains (méthode statique publique) ────────────────────────────

    [Fact]
    public void CalculerGains_SansFans_RetourneGainsBasesSeulementSurTDs()
    {
        // 0 fans : affluence=0, base=0
        var (dom, ext) = MatchService.CalculerGains(2, 1, fansDomicile: 0, fansExterieur: 0);
        Assert.Equal(20_000, dom);  // 0 + 2 TDs × 10k
        Assert.Equal(10_000, ext);  // 0 + 1 TD  × 10k
    }

    [Fact]
    public void CalculerGains_AvecFans_PrendEnCompteAffluence()
    {
        // fans=2+2=4, affluence=20, base=20*10000*0.5=100000 par équipe
        var (dom, ext) = MatchService.CalculerGains(1, 0, fansDomicile: 2, fansExterieur: 2);
        Assert.Equal(100_000 + 10_000, dom);
        Assert.Equal(100_000 + 0, ext);
    }

    [Fact]
    public void CalculerGains_TDsEgaux_ReceoiventMemeBasePlus()
    {
        var (dom, ext) = MatchService.CalculerGains(2, 2, fansDomicile: 0, fansExterieur: 0);
        Assert.Equal(dom, ext);
    }

    // ─── SaisirFeuilleMatchAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SaisirFeuille_VictoireDomicile_Donne3Points()
    {
        var (dom, ext, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 2, TouchdownsExterieur = 0,
            EliminationsDomicile = 0, EliminationsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var d = await db2.Teams.FindAsync(dom.Id);
        var e = await db2.Teams.FindAsync(ext.Id);
        Assert.Equal(3, d!.PointsLigue);
        Assert.Equal(0, e!.PointsLigue);
        Assert.Equal(1, d.NombreVictoires);
        Assert.Equal(1, e.NombreDefaites);
    }

    [Fact]
    public async Task SaisirFeuille_Nul_Donne1PointChacun()
    {
        var (dom, ext, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 1,
            EliminationsDomicile = 0, EliminationsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var d = await db2.Teams.FindAsync(dom.Id);
        var e = await db2.Teams.FindAsync(ext.Id);
        Assert.Equal(1, d!.PointsLigue);
        Assert.Equal(1, e!.PointsLigue);
        Assert.Equal(1, d.NombreNuls);
        Assert.Equal(1, e.NombreNuls);
    }

    [Fact]
    public async Task SaisirFeuille_StatsTouchdownsMisesAJour()
    {
        var (dom, ext, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 3, TouchdownsExterieur = 1,
            EliminationsDomicile = 2, EliminationsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var d = await db2.Teams.FindAsync(dom.Id);
        var e = await db2.Teams.FindAsync(ext.Id);
        Assert.Equal(3, d!.TouchdownsMarques);
        Assert.Equal(1, d.TouchdownsConcedes);
        Assert.Equal(1, e!.TouchdownsMarques);
        Assert.Equal(3, e.TouchdownsConcedes);
        Assert.Equal(2, d.EliminationsInfligees);
    }

    [Fact]
    public async Task SaisirFeuille_CalculePSP_TD3_Passe1_Inter2_Elim2_MVP4()
    {
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 0,
            EliminationsDomicile = 0, EliminationsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        var records = new List<MatchPlayerRecord>
        {
            new()
            {
                TeamPlayerId = joueurDom.Id,
                EstCoteDomicile = true,
                Touchdowns = 1,          // +3 PSP
                Passes = 2,              // +2 PSP
                Interceptions = 1,       // +2 PSP
                EliminationsInfligees = 1, // +2 PSP
                EstMVP = true            // +4 PSP
            }
        };
        // Total attendu : 3+2+2+2+4 = 13 PSP
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, records, saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FindAsync(joueurDom.Id);
        Assert.Equal(13, joueur!.PointsStarPlayer);
    }

    [Fact]
    public async Task SaisirFeuille_PspAdditionnesAuxJoueurs()
    {
        var (_, _, joueurDom, joueurExt, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        var records = new List<MatchPlayerRecord>
        {
            new() { TeamPlayerId = joueurDom.Id, EstCoteDomicile = true, Touchdowns = 1 },  // 3 PSP
            new() { TeamPlayerId = joueurExt.Id, EstCoteDomicile = false, EstMVP = true }    // 4 PSP
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, records, saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        Assert.Equal(3, (await db2.TeamPlayers.FindAsync(joueurDom.Id))!.PointsStarPlayer);
        Assert.Equal(4, (await db2.TeamPlayers.FindAsync(joueurExt.Id))!.PointsStarPlayer);
    }

    [Fact]
    public async Task SaisirFeuille_BlessureManqueSuivant_MarqueLeJoueur()
    {
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        var records = new List<MatchPlayerRecord>
        {
            new()
            {
                TeamPlayerId = joueurDom.Id,
                EstCoteDomicile = true,
                Blessure = InjuryType.ManqueSuivant
            }
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, records, saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FindAsync(joueurDom.Id);
        Assert.True(joueur!.ManqueSuivantMatch);
        Assert.True(await db2.PlayerInjuries.AnyAsync(b =>
            b.TeamPlayerId == joueurDom.Id && b.Type == InjuryType.ManqueSuivant));
    }

    [Fact]
    public async Task SaisirFeuille_PurgeLeManqueSuivantMatchDesJoueursDejaSanctionnes()
    {
        // Bug préexistant : ManqueSuivantMatch n'était levé que par la phase de
        // repos (entre saison régulière et play-offs). Sans phase de repos —
        // format Open, ou ligue qui n'en lance jamais — le joueur restait
        // indisponible pour toujours.
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();

        await using (var dbSetup = _factory.CreateContext())
        {
            var j = await dbSetup.TeamPlayers.FindAsync(joueurDom.Id);
            j!.ManqueSuivantMatch = true;          // sanctionné lors d'un match précédent
            await dbSetup.SaveChangesAsync();
        }

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.SaisirFeuilleMatchAsync(
            match.Id,
            new MatchSheet { TouchdownsDomicile = 1, TouchdownsExterieur = 0 },
            [],
            saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        Assert.False((await db2.TeamPlayers.FindAsync(joueurDom.Id))!.ManqueSuivantMatch);
    }

    [Fact]
    public async Task SaisirFeuille_NePurgePasLaSanctionInfligeeParCeMatchLa()
    {
        // La purge doit passer AVANT le traitement des blessures : sinon elle
        // effacerait aussitôt la sanction issue de la rencontre en cours.
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();

        await using (var dbSetup = _factory.CreateContext())
        {
            var j = await dbSetup.TeamPlayers.FindAsync(joueurDom.Id);
            j!.ManqueSuivantMatch = true;
            await dbSetup.SaveChangesAsync();
        }

        await using var db = _factory.CreateContext();
        var svc = CreateService(db);
        await svc.SaisirFeuilleMatchAsync(
            match.Id,
            new MatchSheet { TouchdownsDomicile = 1, TouchdownsExterieur = 0 },
            [new() { TeamPlayerId = joueurDom.Id, EstCoteDomicile = true, Blessure = InjuryType.ManqueSuivant }],
            saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        Assert.True((await db2.TeamPlayers.FindAsync(joueurDom.Id))!.ManqueSuivantMatch);
    }

    [Fact]
    public async Task ModifierFeuille_AvecPlafondDeFans_NeFaitPasDisparaitreDesFans()
    {
        // Bug d'asymétrie : la validation écrête au plafond, l'annulation
        // soustrayait la variation THÉORIQUE.
        //   plafond 12, équipe à 11, gain +3 → écrêtée à 12
        //   annulation : 12 − 3 = 9 … alors qu'elle avait 11. Deux fans perdus.
        // On doit soustraire la variation RÉELLEMENT appliquée (+1).
        var (dom, ext, _, _, match, saisiPar) = await SetupMatchContextAsync();

        await using (var dbSetup = _factory.CreateContext())
        {
            var ligueId = (await dbSetup.Teams.FindAsync(dom.Id))!.LeagueId;

            // Le seed fournit déjà « Fans dévoués » : on lui pose un plafond de 12.
            var type = await dbSetup.LeagueStaffTypes
                .FirstAsync(l => l.LeagueId == ligueId && l.Nom == StaffService.NomFans);
            type.MaxLigue = 12;
            await dbSetup.SaveChangesAsync();

            dbSetup.TeamStaffs.Add(new TeamStaff { TeamId = dom.Id, LeagueStaffTypeId = type.Id, Quantite = 11 });
            dbSetup.TeamStaffs.Add(new TeamStaff { TeamId = ext.Id, LeagueStaffTypeId = type.Id, Quantite = 5 });
            var d = await dbSetup.Teams.FindAsync(dom.Id); d!.FansDevoues = 11;
            var e = await dbSetup.Teams.FindAsync(ext.Id); e!.FansDevoues = 5;
            await dbSetup.SaveChangesAsync();
        }

        await using (var db = _factory.CreateContext())
        {
            var svc = CreateService(db);
            await svc.SaisirFeuilleMatchAsync(
                match.Id,
                new MatchSheet
                {
                    TouchdownsDomicile = 1, TouchdownsExterieur = 0,
                    VariationFansDomicile = 3,      // 11 + 3 = 14 → écrêté à 12
                    VariationFansExterieur = 2      // 5 + 2 = 7, pas d'écrêtage
                },
                [], saisiPar.Id);
        }

        await using (var db2 = _factory.CreateContext())
        {
            var feuille = await db2.MatchSheets.FirstAsync(f => f.MatchId == match.Id);
            Assert.Equal(1, feuille.VariationFansDomicileAppliquee);   // +1 seulement
            Assert.Equal(2, feuille.VariationFansExterieurAppliquee);
            Assert.Equal(12, (await db2.Teams.FindAsync(dom.Id))!.FansDevoues);
        }

        // Annulation via une modification de feuille (remet les compteurs à plat)
        await using (var db3 = _factory.CreateContext())
        {
            var svc = CreateService(db3);
            await svc.ModifierFeuilleAsync(
                match.Id,
                new MatchSheet
                {
                    TouchdownsDomicile = 0, TouchdownsExterieur = 0,
                    VariationFansDomicile = 0, VariationFansExterieur = 0
                },
                []);
        }

        await using (var db4 = _factory.CreateContext())
        {
            // 11 au départ : l'annulation doit restituer 11, pas 9.
            Assert.Equal(11, (await db4.Teams.FindAsync(dom.Id))!.FansDevoues);
            Assert.Equal(5, (await db4.Teams.FindAsync(ext.Id))!.FansDevoues);
        }
    }

    [Fact]
    public async Task SaisirFeuille_Mort_MarqueJoueurMort()
    {
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 0, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        var records = new List<MatchPlayerRecord>
        {
            new()
            {
                TeamPlayerId = joueurDom.Id,
                EstCoteDomicile = true,
                Blessure = InjuryType.Mort
            }
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, records, saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FindAsync(joueurDom.Id);
        Assert.True(joueur!.EstMort);
    }

    [Fact]
    public async Task SaisirFeuille_BlessurePersistante_ReduitCaracteristique()
    {
        var (_, _, joueurDom, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 0, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        var records = new List<MatchPlayerRecord>
        {
            new()
            {
                TeamPlayerId = joueurDom.Id,
                EstCoteDomicile = true,
                Blessure = InjuryType.BlessurePersistante,
                StatAffectee = AffectedStat.Mouvement
            }
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, records, saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var joueur = await db2.TeamPlayers.FindAsync(joueurDom.Id);
        Assert.Equal(-1, joueur!.ModMouvement);
    }

    [Fact]
    public async Task SaisirFeuille_PasseStatutEnFeuilleEnSaisie()
    {
        var (_, _, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 0, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);

        await using var db2 = _factory.CreateContext();
        var m = await db2.Matches.FindAsync(match.Id);
        Assert.Equal(MatchStatus.FeuilleEnSaisie, m!.Statut);
    }

    // ─── ValiderFeuilleAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ValiderFeuille_PasseStatutTermine()
    {
        var (_, _, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);
        await svc.ValiderFeuilleAsync(match.Id, "RAS");

        await using var db2 = _factory.CreateContext();
        var m = await db2.Matches.FindAsync(match.Id);
        Assert.Equal(MatchStatus.Termine, m!.Statut);
    }

    [Fact]
    public async Task ValiderFeuille_MarqueFeuilleValide()
    {
        var (_, _, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 0, TouchdownsExterieur = 1,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);
        await svc.ValiderFeuilleAsync(match.Id, "OK");

        await using var db2 = _factory.CreateContext();
        var f = await db2.MatchSheets.FirstAsync(s => s.MatchId == match.Id);
        Assert.True(f.ValideParCommissaire);
        Assert.Equal("OK", f.NotesCommissaire);
    }

    // ─── GetMatchsEnAttenteValidationAsync ────────────────────────────────────

    [Fact]
    public async Task GetMatchsEnAttenteValidation_FiltreLigueEtStatut()
    {
        var (dom, ext, _, _, match, saisiPar) = await SetupMatchContextAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        // Saisir la feuille pour passer en ValidationCompetences
        var feuille = new MatchSheet
        {
            TouchdownsDomicile = 1, TouchdownsExterieur = 0,
            GainsDomicile = 0, GainsExterieur = 0
        };
        await svc.SaisirFeuilleMatchAsync(match.Id, feuille, [], saisiPar.Id);

        // Récupérer la ligue via la team
        await using var db2 = _factory.CreateContext();
        var t = await db2.Teams.FindAsync(dom.Id);
        var matchsEnAttente = await svc.GetMatchsEnAttenteValidationAsync(t!.LeagueId);

        Assert.Single(matchsEnAttente);
        Assert.Equal(match.Id, matchsEnAttente[0].Id);
    }

    // ─── CalculerPSP (méthode statique publique, gameType-aware) ─────────────

    [Fact]
    public void CalculerPSP_BloodBowl_TouchdownDonne3Points()
    {
        var record = new MatchPlayerRecord { Touchdowns = 2 };
        var psp = MatchService.CalculerPSPPublic(record, GameType.BloodBowl);
        Assert.Equal(6, psp);
    }

    [Fact]
    public void CalculerPSP_DungeonBowl_TouchdownDonne5Points()
    {
        var record = new MatchPlayerRecord { Touchdowns = 2 };
        var psp = MatchService.CalculerPSPPublic(record, GameType.DungeonBowl);
        Assert.Equal(10, psp);
    }

    [Fact]
    public void CalculerPSP_TousLesEvenementsCombines_BloodBowl()
    {
        var record = new MatchPlayerRecord
        {
            Touchdowns = 1,          // 3 PSP
            Passes = 2,              // 2 PSP
            Interceptions = 1,       // 2 PSP
            EliminationsInfligees = 2, // 4 PSP
            EstMVP = true            // 4 PSP
        };
        var psp = MatchService.CalculerPSPPublic(record, GameType.BloodBowl);
        Assert.Equal(15, psp);
    }
}
