using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Programmation d'un match : date et lieu (#1). Saisie libre par les deux
/// coaches et les commissaires — mais l'habilitation doit tenir côté serveur.
/// </summary>
public class ProgrammationMatchTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<(MatchService svc, ApplicationDbContext db, int matchId,
                        string coachDom, string coachExt, string intrus)> PreparerAsync()
    {
        var db = _factory.CreateContext();

        var commissaire = DataSeeder.CreateUser("commissaire");
        var coachDom = DataSeeder.CreateUser("dom");
        var coachExt = DataSeeder.CreateUser("ext");
        var intrus = DataSeeder.CreateUser("intrus");
        db.Users.AddRange(commissaire, coachDom, coachExt, intrus);
        await db.SaveChangesAsync();

        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);

        var equipeDom = await DataSeeder.SeedTeamAsync(db, ligue.Id, coachDom.Id, teamType.Id, "Domicile");
        var equipeExt = await DataSeeder.SeedTeamAsync(db, ligue.Id, coachExt.Id, teamType.Id, "Extérieur");

        var division = new Division { LeagueId = ligue.Id, Nom = "Division 1", Ordre = 1 };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        var match = new Match
        {
            DivisionId = division.Id,
            Ronde = 1,
            EquipeDomicileId = equipeDom.Id,
            EquipeExterieurId = equipeExt.Id,
            Statut = MatchStatus.Programme
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        var settings = new SettingsService(db);
        var svc = new MatchService(db, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance), settings);
        return (svc, db, match.Id, coachDom.Id, coachExt.Id, intrus.Id);
    }

    [Fact]
    public async Task CoachDomicile_PeutFixerDateEtLieu()
    {
        var (svc, db, matchId, coachDom, _, _) = await PreparerAsync();
        var quand = new DateTime(2026, 9, 12, 18, 30, 0, DateTimeKind.Utc);

        await svc.ProgrammerMatchAsync(matchId, quand, "Le Repaire du Troll", coachDom);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.Equal(quand, relu!.DateProgrammee);
        Assert.Equal("Le Repaire du Troll", relu.Lieu);
    }

    [Fact]
    public async Task CoachExterieur_PeutAussiFixerLaDate()
    {
        // saisie libre : les deux coaches, pas seulement celui qui reçoit
        var (svc, db, matchId, _, coachExt, _) = await PreparerAsync();

        await svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(3), "Chez Marc", coachExt);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.NotNull(relu!.DateProgrammee);
        Assert.Equal("Chez Marc", relu.Lieu);
    }

    [Fact]
    public async Task UnCoachEtranger_NePeutPasFixerLaDate()
    {
        var (svc, _, matchId, _, _, intrus) = await PreparerAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(1), "Ailleurs", intrus));
    }

    [Fact]
    public async Task UnCommissaire_PeutFixerLaDateSansEtreCoach()
    {
        var (svc, db, matchId, _, _, intrus) = await PreparerAsync();

        await svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(1), "Salle municipale",
            intrus, estCommissaire: true);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.NotNull(relu!.DateProgrammee);
    }

    [Fact]
    public async Task EffacerLaDate_EstPossible()
    {
        var (svc, db, matchId, coachDom, _, _) = await PreparerAsync();
        await svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(1), "Quelque part", coachDom);

        await svc.ProgrammerMatchAsync(matchId, null, "", coachDom);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.Null(relu!.DateProgrammee);
        Assert.Equal("", relu.Lieu);
    }

    [Fact]
    public async Task UnMatchDejaJoue_NePeutPlusEtreReprogramme()
    {
        var (svc, db, matchId, coachDom, _, _) = await PreparerAsync();
        var match = await db.Matches.FindAsync(matchId);
        match!.Statut = MatchStatus.Termine;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow, "Trop tard", coachDom));
    }

    [Fact]
    public async Task LeLieu_EstNettoyeDesEspacesSuperflus()
    {
        var (svc, db, matchId, coachDom, _, _) = await PreparerAsync();

        await svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(1), "   Chez Bob   ", coachDom);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.Equal("Chez Bob", relu!.Lieu);
    }

    [Fact]
    public async Task LeLieuPeutEtreVide_UneDateSeuleSuffit()
    {
        var (svc, db, matchId, coachDom, _, _) = await PreparerAsync();

        await svc.ProgrammerMatchAsync(matchId, DateTime.UtcNow.AddDays(1), "", coachDom);

        var relu = await db.Matches.FindAsync(matchId);
        Assert.NotNull(relu!.DateProgrammee);
        Assert.Equal("", relu.Lieu);
    }
}
