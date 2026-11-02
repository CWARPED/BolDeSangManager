using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Tests;

/// <summary>
/// Ticket #26 — « une BP n'est pas une blessure avec Séquelle ».
///
/// Avant le correctif, <c>InjuryType.BlessurePersistante</c> appliquait en
/// réalité une SÉQUELLE (−1 sur une caractéristique) et ne posait aucun RPM.
/// La migration <c>SeparerBlessurePersistanteEtSequelle</c> reclasse les lignes
/// concernées ; son SQL est rejoué ici mot pour mot.
///
/// ⚠️ Argument de sûreté du backfill : avant #26, le SEUL effet qu'une ligne
/// « BlessurePersistante » portant une caractéristique ait jamais eu est la
/// réduction de cette caractéristique — c'est la définition d'une Séquelle. Une
/// ligne sans caractéristique n'a eu aucun effet : on ne la devine pas, elle
/// reste une BP. Ce raisonnement ne vaut QUE pour les données antérieures à
/// #26 : ne pas rejouer ce SQL plus tard, une BP saisie depuis n'a jamais de
/// caractéristique (le service l'efface).
/// </summary>
public class BlessuresTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    /// <summary>SQL identique à celui de la migration.</summary>
    internal const string SqlBackfill = @"
UPDATE PlayerInjuries
SET Type = 4,
    Description = REPLACE(Description, 'Blessure persistante : ', 'Séquelle : ')
WHERE Type = 1 AND StatAffectee IS NOT NULL;
UPDATE MatchPlayerRecords
SET Blessure = 4
WHERE Blessure = 1 AND StatAffectee IS NOT NULL;";

    // ─── Enum persisté en int ET exporté par nom ─────────────────────────────

    [Fact]
    public void InjuryType_ValeursPersisteesFigees()
    {
        // Réordonner l'enum réaffecterait en silence les blessures en base.
        Assert.Equal(0, (int)InjuryType.ManqueSuivant);
        Assert.Equal(1, (int)InjuryType.BlessurePersistante);
        Assert.Equal(2, (int)InjuryType.RetraiteTemporaire);
        Assert.Equal(3, (int)InjuryType.Mort);
        Assert.Equal(4, (int)InjuryType.Sequelle);
    }

    // ─── Libellés (vocabulaire du LRB) ───────────────────────────────────────

    [Fact]
    public void Libelles_SuiventLeTableauDElimination()
    {
        Assert.Equal("Amoché", BlessureHelpers.Libelle(InjuryType.ManqueSuivant));
        Assert.Equal("Blessure sérieuse", BlessureHelpers.Libelle(InjuryType.BlessurePersistante));
        Assert.Equal("Séquelle", BlessureHelpers.Libelle(InjuryType.Sequelle));
        Assert.Equal("Séquelle : −1 F", BlessureHelpers.Resume(InjuryType.Sequelle, AffectedStat.Force));
        Assert.Equal("Blessure sérieuse : BP", BlessureHelpers.Resume(InjuryType.BlessurePersistante, null));
    }

    [Fact]
    public void NombreBP_NeCompteQueLesBlessuresPersistantes()
    {
        var j = new TeamPlayer
        {
            Blessures =
            [
                new PlayerInjury { Type = InjuryType.BlessurePersistante },
                new PlayerInjury { Type = InjuryType.BlessurePersistante },
                new PlayerInjury { Type = InjuryType.Sequelle, StatAffectee = AffectedStat.Armure },
                new PlayerInjury { Type = InjuryType.ManqueSuivant },
            ]
        };
        Assert.Equal(2, BlessureHelpers.NombreBP(j));
        Assert.True(BlessureHelpers.ASequelle(j));
    }

    // ─── Import d'un export antérieur à #26 ──────────────────────────────────

    [Theory]
    [InlineData(InjuryType.BlessurePersistante, AffectedStat.Agilite, InjuryType.Sequelle, AffectedStat.Agilite)]
    [InlineData(InjuryType.BlessurePersistante, null, InjuryType.BlessurePersistante, null)]
    [InlineData(InjuryType.Sequelle, AffectedStat.Force, InjuryType.Sequelle, AffectedStat.Force)]
    [InlineData(InjuryType.ManqueSuivant, AffectedStat.Force, InjuryType.ManqueSuivant, null)]
    public void NormaliserImport_RelitUneAncienneBPAvecCaracCommeSequelle(
        InjuryType type, AffectedStat? stat, InjuryType typeAttendu, AffectedStat? statAttendue)
    {
        var (t, s) = BlessureHelpers.NormaliserImport(type, stat);
        Assert.Equal(typeAttendu, t);
        Assert.Equal(statAttendue, s);
    }

    // ─── Backfill de la migration ────────────────────────────────────────────

    [Fact]
    public async Task Backfill_ReclasseEnSequelleLesBPPortantUneCaracteristique()
    {
        var (joueurId, recordAvecStat, recordSansStat) = await SeedAnciennesBlessuresAsync();

        await using (var db = _factory.CreateContext())
            await db.Database.ExecuteSqlRawAsync(SqlBackfill);

        await using var relecture = _factory.CreateContext();
        var blessures = await relecture.PlayerInjuries
            .Where(b => b.TeamPlayerId == joueurId).OrderBy(b => b.Id).ToListAsync();

        // Le cas à réparer.
        Assert.Equal(InjuryType.Sequelle, blessures[0].Type);
        Assert.Equal(AffectedStat.Mouvement, blessures[0].StatAffectee);
        Assert.Equal("Séquelle : Mouvement", blessures[0].Description);

        // CONTRE-ÉPREUVE : une BP sans caractéristique n'est pas devinée, et
        // les autres types ne bougent pas.
        Assert.Equal(InjuryType.BlessurePersistante, blessures[1].Type);
        Assert.Equal(InjuryType.ManqueSuivant, blessures[2].Type);

        Assert.Equal(InjuryType.Sequelle,
            (await relecture.MatchPlayerRecords.FindAsync(recordAvecStat))!.Blessure);
        Assert.Equal(InjuryType.BlessurePersistante,
            (await relecture.MatchPlayerRecords.FindAsync(recordSansStat))!.Blessure);

        // Le profil du joueur n'est PAS touché : la réduction avait déjà été
        // appliquée à l'époque, le backfill ne change que le nom du type.
        var j = await relecture.TeamPlayers.FindAsync(joueurId);
        Assert.Equal(-1, j!.ModMouvement);
        Assert.False(j.ManqueSuivantMatch);
    }

    [Fact]
    public async Task Backfill_EstIdempotent()
    {
        await SeedAnciennesBlessuresAsync();
        await using var db = _factory.CreateContext();

        Assert.Equal(2, await db.Database.ExecuteSqlRawAsync(SqlBackfill));
        Assert.Equal(0, await db.Database.ExecuteSqlRawAsync(SqlBackfill));
    }

    private async Task<(int joueurId, int recordAvecStat, int recordSansStat)> SeedAnciennesBlessuresAsync()
    {
        await using var db = _factory.CreateContext();
        var comm = DataSeeder.CreateUser("bcomm");
        var c1 = DataSeeder.CreateUser("bc1");
        var c2 = DataSeeder.CreateUser("bc2");
        db.Users.AddRange(comm, c1, c2);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (tt, pos) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, comm.Id);
        var div = new Division { LeagueId = ligue.Id, Nom = "D", Ordre = 1 };
        db.Divisions.Add(div);
        await db.SaveChangesAsync();
        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, c1.Id, tt.Id, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, c2.Id, tt.Id, "Ext");
        var joueur = await DataSeeder.SeedPlayerAsync(db, dom.Id, pos.Id, "Vétéran", 1);
        var match = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id, div.Id);

        // État laissé par l'ancien code : la « BP » avec caractéristique a
        // réduit le profil et n'a posé aucun RPM.
        joueur.ModMouvement = -1;
        var feuille = new MatchSheet { MatchId = match.Id, SaisiParId = c1.Id };
        db.MatchSheets.Add(feuille);
        await db.SaveChangesAsync();

        var rAvec = new MatchPlayerRecord { MatchSheetId = feuille.Id, TeamPlayerId = joueur.Id,
            Blessure = InjuryType.BlessurePersistante, StatAffectee = AffectedStat.Mouvement };
        var rSans = new MatchPlayerRecord { MatchSheetId = feuille.Id, TeamPlayerId = joueur.Id,
            Blessure = InjuryType.BlessurePersistante };
        db.MatchPlayerRecords.AddRange(rAvec, rSans);
        db.PlayerInjuries.AddRange(
            new PlayerInjury { TeamPlayerId = joueur.Id, MatchId = match.Id,
                Type = InjuryType.BlessurePersistante, StatAffectee = AffectedStat.Mouvement,
                Description = "Blessure persistante : Mouvement" },
            new PlayerInjury { TeamPlayerId = joueur.Id, MatchId = match.Id,
                Type = InjuryType.BlessurePersistante, Description = "Blessure persistante : " },
            new PlayerInjury { TeamPlayerId = joueur.Id, MatchId = match.Id,
                Type = InjuryType.ManqueSuivant, Description = "Rate le prochain match" });
        await db.SaveChangesAsync();
        return (joueur.Id, rAvec.Id, rSans.Id);
    }
}
