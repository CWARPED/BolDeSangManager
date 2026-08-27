using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Diffusion temps réel des changements de MATCH.
///
/// Un match est collaboratif par conception : les deux coaches remplissent la
/// feuille, la confirment, puis dépensent leurs XP en après-match — et le match
/// se clôt TOUT SEUL quand les deux ont validé. Sans diffusion, celui qui a
/// validé en premier reste sur un écran figé et ne voit jamais la clôture.
/// </summary>
public class MatchNotificationTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static (MatchService svc, List<int> recus) CreateService(ApplicationDbContext db)
    {
        var settings = new SettingsService(db);
        var notif = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var recus = new List<int>();
        notif.MatchModifie += id => { recus.Add(id); return Task.CompletedTask; };
        var svc = new MatchService(db, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance), settings, notif);
        return (svc, recus);
    }

    private async Task<(int matchId, string coachDom, string coachExt)> SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var dom = DataSeeder.CreateUser("dom-notif");
        var ext = DataSeeder.CreateUser("ext-notif");
        db.Users.AddRange(dom, ext);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, dom.Id);

        var division = new Division { LeagueId = ligue.Id, Nom = "D1", Ordre = 1 };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        var eDom = await DataSeeder.SeedTeamAsync(db, ligue.Id, dom.Id, teamType.Id, "Dom");
        var eExt = await DataSeeder.SeedTeamAsync(db, ligue.Id, ext.Id, teamType.Id, "Ext");
        eDom.DivisionId = division.Id;
        eExt.DivisionId = division.Id;

        var match = new Match
        {
            DivisionId = division.Id,
            Ronde = 1,
            EquipeDomicileId = eDom.Id,
            EquipeExterieurId = eExt.Id,
            Statut = MatchStatus.Programme
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return (match.Id, dom.Id, ext.Id);
    }

    /// <summary>Fixer la date concerne l'adversaire : il doit le voir sans recharger.</summary>
    [Fact]
    public async Task ProgrammerMatch_NotifieLesEcransOuverts()
    {
        var (matchId, coachDom, _) = await SetupAsync();

        await using var db = _factory.CreateContext();
        var (svc, recus) = CreateService(db);
        await svc.ProgrammerMatchAsync(matchId,
            new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc), "Stade", coachDom,
            estCommissaire: false);

        Assert.Contains(matchId, recus);
    }

    /// <summary>
    /// La saisie de la feuille par un coach doit apparaître chez l'autre : c'est
    /// lui qui doit ensuite la confirmer.
    /// </summary>
    [Fact]
    public async Task SaisirFeuille_NotifieLesEcransOuverts()
    {
        var (matchId, coachDom, _) = await SetupAsync();

        await using var db = _factory.CreateContext();
        var (svc, recus) = CreateService(db);

        var feuille = new MatchSheet { MatchId = matchId, TouchdownsDomicile = 2, TouchdownsExterieur = 1 };
        await svc.SaisirFeuilleMatchAsync(matchId, feuille, [], coachDom);

        Assert.Contains(matchId, recus);
    }

    /// <summary>
    /// Le service doit rester utilisable sans diffusion : une notification
    /// absente ne doit jamais casser l'opération métier.
    /// </summary>
    [Fact]
    public async Task SansServiceDeNotification_LOperationMetierReussitQuandMeme()
    {
        var (matchId, coachDom, _) = await SetupAsync();

        await using var db = _factory.CreateContext();
        var settings = new SettingsService(db);
        var svc = new MatchService(db, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance), settings);

        await svc.ProgrammerMatchAsync(matchId,
            new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc), "Stade", coachDom,
            estCommissaire: false);

        await using var db2 = _factory.CreateContext();
        Assert.NotNull((await db2.Matches.FindAsync(matchId))!.DateProgrammee);
    }

    /// <summary>
    /// Même piège que sur la ligue : le DbContext vit aussi longtemps que
    /// l'onglet, donc une relecture SANS vidage du cache EF rend l'instance
    /// chargée à l'ouverture — l'écran se « rafraîchit » sur des données
    /// périmées et la notification semble ne servir à rien.
    /// </summary>
    [Fact]
    public async Task GetMatchFrais_VoitUnChangementFaitAilleurs()
    {
        var (matchId, coachDom, _) = await SetupAsync();

        await using var dbEcran = _factory.CreateContext();
        var (svcEcran, _) = CreateService(dbEcran);
        Assert.Null((await svcEcran.GetMatchAsync(matchId))!.DateProgrammee);

        await using (var dbAutre = _factory.CreateContext())
        {
            var (svcAutre, _) = CreateService(dbAutre);
            await svcAutre.ProgrammerMatchAsync(matchId,
                new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc), "Stade", coachDom,
                estCommissaire: false);
        }

        // Sans vidage : l'écran relit l'instance chargée à l'ouverture.
        Assert.Null((await svcEcran.GetMatchAsync(matchId))!.DateProgrammee);

        // Avec vidage : il voit enfin le changement.
        Assert.NotNull((await svcEcran.GetMatchFraisAsync(matchId))!.DateProgrammee);
    }
}
