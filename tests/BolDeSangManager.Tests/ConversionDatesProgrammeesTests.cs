using BolDeSangManager.Data.Models;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Tests;

/// <summary>
/// Backfill de la migration <c>ConvertirDatesProgrammeesEnUtc</c> (#19).
///
/// L'image Docker n'embarquait pas de base de fuseaux : le serveur tournait en
/// UTC, et <c>ProgrammationDialog</c> (« heure locale → UTC ») stockait donc
/// l'heure de PARIS telle quelle. L'appli l'affichait juste (aucune conversion
/// dans un sens ni dans l'autre) mais le calendrier .ics / Google la lisait
/// comme de l'UTC : +1 h en hiver, +2 h en été. Depuis que l'image tourne en
/// Europe/Paris, les valeurs existantes doivent devenir de vrais instants UTC.
///
/// Le SQL est rejoué ici mot pour mot et comparé à <see cref="TimeZoneInfo"/>
/// (Europe/Paris), règles d'heure d'été comprises.
///
/// ⚠️ Conversion NON idempotente par nature : sa sûreté repose sur
/// __EFMigrationsHistory (une seule exécution). Ne jamais la rejouer à la main.
/// </summary>
public class ConversionDatesProgrammeesTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    /// <summary>SQL identique à celui de la migration.</summary>
    internal const string SqlConversion = @"
UPDATE Matches
SET DateProgrammee = strftime('%Y-%m-%d %H:%M:%S', DateProgrammee,
    CASE
        WHEN DateProgrammee >= date(strftime('%Y', DateProgrammee) || '-04-01', 'weekday 0', '-7 days') || ' 02:00:00'
         AND DateProgrammee <  date(strftime('%Y', DateProgrammee) || '-11-01', 'weekday 0', '-7 days') || ' 03:00:00'
        THEN '-2 hours'
        ELSE '-1 hours'
    END)
WHERE DateProgrammee IS NOT NULL;";

    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public static TheoryData<string> HeuresDeParis => new()
    {
        "2026-01-15 20:00:00",   // hiver
        "2026-07-14 21:00:00",   // été
        "2026-09-12 13:00:00",   // la vraie date de la sauvegarde du 31/08
        "2026-03-28 23:30:00",   // veille du passage à l'heure d'été (dim. 29/03)
        "2026-03-29 01:30:00",   // jour J, avant 02:00 → encore l'hiver
        "2026-03-29 03:30:00",   // jour J, après le saut → été
        "2026-10-24 20:00:00",   // veille du retour à l'heure d'hiver (dim. 25/10)
        "2026-10-25 03:30:00",   // jour J, après le retour → hiver
        "2026-11-07 14:00:00",   // novembre
        "2027-03-28 03:00:00",   // 2027 : dernier dimanche de mars = 28
        "2027-10-31 01:00:00",   // 2027 : dernier dimanche d'octobre = 31, encore l'été
        "2025-12-31 23:00:00",   // passage d'année
    };

    [Theory]
    [MemberData(nameof(HeuresDeParis))]
    public async Task Conversion_DonneLeMemeInstantQueLeFuseauEuropeParis(string heureParis)
    {
        var local = DateTime.Parse(heureParis);
        var attendu = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Paris);

        var matchId = await SeedMatchAvecDateAsync(local);
        await using (var db = _factory.CreateContext())
            await db.Database.ExecuteSqlRawAsync(SqlConversion);

        await using var relecture = _factory.CreateContext();
        var m = await relecture.Matches.FindAsync(matchId);
        Assert.Equal(attendu, m!.DateProgrammee);
    }

    [Fact]
    public async Task Conversion_NeTouchePasAuxMatchsSansDate()
    {
        var sansDate = await SeedMatchAvecDateAsync(null);
        var avecDate = await SeedMatchAvecDateAsync(new DateTime(2026, 1, 15, 20, 0, 0));

        await using var db = _factory.CreateContext();
        Assert.Equal(1, await db.Database.ExecuteSqlRawAsync(SqlConversion));

        await using var relecture = _factory.CreateContext();
        Assert.Null((await relecture.Matches.FindAsync(sansDate))!.DateProgrammee);
        Assert.Equal(new DateTime(2026, 1, 15, 19, 0, 0),
            (await relecture.Matches.FindAsync(avecDate))!.DateProgrammee);
    }

    private async Task<int> SeedMatchAvecDateAsync(DateTime? date)
    {
        await using var db = _factory.CreateContext();
        var comm = DataSeeder.CreateUser("tz" + Guid.NewGuid().ToString("N")[..6]);
        db.Users.Add(comm);
        await db.SaveChangesAsync();
        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (tt, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, comm.Id);
        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, comm.Id, tt.Id, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, comm.Id, tt.Id, "Ext");
        var match = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id);
        match.DateProgrammee = date;
        await db.SaveChangesAsync();
        return match.Id;
    }
}
