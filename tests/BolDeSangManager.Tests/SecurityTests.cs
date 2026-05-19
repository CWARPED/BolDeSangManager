using System.Text;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Tests de sécurité :
///   1. Validation des données à l'import JSON (ValiderImport via ImportAsync)
///   2. Vérifications que GetMatchAsync expose les données nécessaires aux gardes composant
///      (Feuille.razor = CoachId, Validation.razor = CommissaireId)
/// </summary>
public class SecurityTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private LeagueExportService CreateSvc(ApplicationDbContext db) =>
        new(db, NullLogger<LeagueExportService>.Instance);

    private static Stream Json(string json) =>
        new MemoryStream(Encoding.UTF8.GetBytes(json));

    // ── JSON de base valides ────────────────────────────────────────────────────

    private const string BaseJson = """
        {
          "version": "1.0",
          "exporteeLe": "2024-01-01T00:00:00Z",
          "nomLigue": "Ligue Test",
          "description": "",
          "gameNom": "Blood Bowl",
          "rulesVersionNom": null,
          "format": "RoundRobin",
          "budgetDepart": 1000000,
          "nombreEquipesPlayoff": 4,
          "equipes": [],
          "matchs": []
        }
        """;

    private const string BaseJsonAvecEquipe = """
        {
          "version": "1.0",
          "exporteeLe": "2024-01-01T00:00:00Z",
          "nomLigue": "Ligue Test",
          "description": "",
          "gameNom": "Blood Bowl",
          "rulesVersionNom": null,
          "format": "RoundRobin",
          "budgetDepart": 1000000,
          "nombreEquipesPlayoff": 4,
          "equipes": [
            {
              "nom": "Les Brutes",
              "typeEquipeNom": "Humains",
              "coachEmail": null,
              "tresorerie": 500000,
              "nombreRelances": 3,
              "fansDevoues": 1,
              "nombreCoachsAssistants": 0,
              "nombreCheerleaders": 0,
              "apothicaire": false,
              "nombreMatchsJoues": 0,
              "nombreVictoires": 0,
              "nombreNuls": 0,
              "nombreDefaites": 0,
              "touchdownsMarques": 0,
              "touchdownsConcedes": 0,
              "eliminationsInfligees": 0,
              "pointsLigue": 0,
              "joueurs": []
            }
          ],
          "matchs": []
        }
        """;

    // ── 1. Nom de ligue ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Import_NomLigueVide_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJson.Replace("\"nomLigue\": \"Ligue Test\"", "\"nomLigue\": \"\"");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_NomLigueWhitespace_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJson.Replace("\"nomLigue\": \"Ligue Test\"", "\"nomLigue\": \"   \"");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_NomLigueTropLong_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJson.Replace("Ligue Test", new string('X', 201));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    // ── 2. Budget / playoff ─────────────────────────────────────────────────────

    [Fact]
    public async Task Import_BudgetNegatif_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJson.Replace("\"budgetDepart\": 1000000", "\"budgetDepart\": -1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_NombreEquipesPlayoffHorsLimite_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJson.Replace("\"nombreEquipesPlayoff\": 4", "\"nombreEquipesPlayoff\": 65");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    // ── 3. Trop d'équipes ───────────────────────────────────────────────────────

    [Fact]
    public async Task Import_TropDEquipes_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var e = "{\"nom\":\"E\",\"typeEquipeNom\":\"H\",\"coachEmail\":null,\"tresorerie\":0,\"nombreRelances\":0,\"fansDevoues\":0,\"nombreCoachsAssistants\":0,\"nombreCheerleaders\":0,\"apothicaire\":false,\"nombreMatchsJoues\":0,\"nombreVictoires\":0,\"nombreNuls\":0,\"nombreDefaites\":0,\"touchdownsMarques\":0,\"touchdownsConcedes\":0,\"eliminationsInfligees\":0,\"pointsLigue\":0,\"joueurs\":[]}";
        var equipes = "[" + string.Join(",", Enumerable.Repeat(e, 65)) + "]";
        var json = BaseJson.Replace("\"equipes\": []", $"\"equipes\": {equipes}");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    // ── 4. Champs d'équipe ──────────────────────────────────────────────────────

    [Fact]
    public async Task Import_NomEquipeVide_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJsonAvecEquipe.Replace("\"nom\": \"Les Brutes\"", "\"nom\": \"\"");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_TresoreNegative_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJsonAvecEquipe.Replace("\"tresorerie\": 500000", "\"tresorerie\": -1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_RelancesNegatives_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJsonAvecEquipe.Replace("\"nombreRelances\": 3", "\"nombreRelances\": -1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_StatsMatchNegatives_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJsonAvecEquipe.Replace("\"nombreMatchsJoues\": 0", "\"nombreMatchsJoues\": -1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_PointsLigueNegatifs_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = BaseJsonAvecEquipe.Replace("\"pointsLigue\": 0", "\"pointsLigue\": -10");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    // ── 5. Joueurs ──────────────────────────────────────────────────────────────

    private const string JoueurValide =
        "{\"nom\":\"Gromag\",\"numero\":1,\"positionNom\":\"Lineman\"," +
        "\"valeurActuelle\":50000,\"pointsStarPlayer\":0,\"nombreAmeliorations\":0," +
        "\"modMouvement\":0,\"modForce\":0,\"modAgilite\":0,\"modCapacitePasse\":0,\"modArmure\":0," +
        "\"estMort\":false,\"estRetraite\":false,\"manqueSuivantMatch\":false," +
        "\"competencesAcquises\":[],\"blessures\":[]}";

    private static string JsonAvecJoueur(string joueur) =>
        BaseJsonAvecEquipe.Replace("\"joueurs\": []", $"\"joueurs\": [{joueur}]");

    [Fact]
    public async Task Import_NomJoueurVide_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"nom\":\"Gromag\"", "\"nom\":\"\""));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_NumeroJoueurZero_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"numero\":1", "\"numero\":0"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_NumeroJoueur100_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"numero\":1", "\"numero\":100"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_ValeurJoueurNegative_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"valeurActuelle\":50000", "\"valeurActuelle\":-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_PSPNegatifs_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"pointsStarPlayer\":0", "\"pointsStarPlayer\":-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_ModStatPositif_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"modMouvement\":0", "\"modMouvement\":1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_ModStatHorsLimite_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecJoueur(JoueurValide.Replace("\"modArmure\":0", "\"modArmure\":-6"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_TropDeJoueurs_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var joueurs = "[" + string.Join(",", Enumerable.Range(1, 33)
            .Select(i => JoueurValide.Replace("\"numero\":1", $"\"numero\":{(i <= 16 ? i : 1)}")
                                     .Replace("\"nom\":\"Gromag\"", $"\"nom\":\"J{i}\""))) + "]";
        var json = BaseJsonAvecEquipe.Replace("\"joueurs\": []", $"\"joueurs\": {joueurs}");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    // ── 6. Matchs ───────────────────────────────────────────────────────────────

    private const string MatchValide =
        "{\"ronde\":1,\"estPlayoff\":false,\"equipeDomicileNom\":\"A\",\"equipeExterieurNom\":\"B\"," +
        "\"statut\":\"Programme\",\"scoreDomicile\":null,\"scoreExterieur\":null,\"dateJouee\":null,\"feuille\":null}";

    private static string JsonAvecMatch(string match) =>
        BaseJson.Replace("\"matchs\": []", $"\"matchs\": [{match}]");

    [Fact]
    public async Task Import_RondeZero_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var json = JsonAvecMatch(MatchValide.Replace("\"ronde\":1", "\"ronde\":0"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_ScoreNegatif_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var m = MatchValide.Replace("\"scoreDomicile\":null", "\"scoreDomicile\":-1")
                           .Replace("\"scoreExterieur\":null", "\"scoreExterieur\":2");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(JsonAvecMatch(m)), "u"));
    }

    [Fact]
    public async Task Import_TropDeMatchs_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var matchs = "[" + string.Join(",", Enumerable.Repeat(MatchValide, 2001)) + "]";
        var json = BaseJson.Replace("\"matchs\": []", $"\"matchs\": {matchs}");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(json), "u"));
    }

    [Fact]
    public async Task Import_GainsNegatifsDansFeuille_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var feuille = "{\"touchdownsDomicile\":1,\"touchdownsExterieur\":0," +
                      "\"eliminationsDomicile\":0,\"eliminationsExterieur\":0," +
                      "\"gainsDomicile\":-1,\"gainsExterieur\":0," +
                      "\"variationFansDomicile\":0,\"variationFansExterieur\":0," +
                      "\"valideParCommissaire\":false,\"notesCommissaire\":\"\",\"records\":[]}";
        var m = MatchValide.Replace("\"feuille\":null", $"\"feuille\":{feuille}")
                           .Replace("\"scoreDomicile\":null", "\"scoreDomicile\":1")
                           .Replace("\"scoreExterieur\":null", "\"scoreExterieur\":0")
                           .Replace("\"statut\":\"Programme\"", "\"statut\":\"Termine\"");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(JsonAvecMatch(m)), "u"));
    }

    [Fact]
    public async Task Import_TropDeRecordsDansFeuille_ThrowsInvalidOperation()
    {
        await using var db = _factory.CreateContext();
        var r = "{\"joueurNom\":\"J\",\"equipeNom\":\"E\",\"touchdowns\":0,\"completions\":0,\"interceptions\":0,\"eliminationsInfligees\":0,\"estMvp\":false,\"pspGagnes\":0,\"blessure\":null,\"statAffectee\":null}";
        var records = "[" + string.Join(",", Enumerable.Repeat(r, 65)) + "]";
        var feuille = "{\"touchdownsDomicile\":0,\"touchdownsExterieur\":0,\"eliminationsDomicile\":0,\"eliminationsExterieur\":0,\"gainsDomicile\":0,\"gainsExterieur\":0,\"variationFansDomicile\":0,\"variationFansExterieur\":0,\"valideParCommissaire\":false,\"notesCommissaire\":\"\",\"records\":" + records + "}";
        var m = MatchValide.Replace("\"feuille\":null", $"\"feuille\":{feuille}");
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSvc(db).ImportAsync(Json(JsonAvecMatch(m)), "u"));
    }

    // ── 7. GetMatchAsync expose les données pour les gardes composant ──────────

    [Fact]
    public async Task GetMatchAsync_ExposeCommissaireId_PourGardeValidation()
    {
        // Vérifie que CommissaireId est chargé — utilisé par Validation.razor
        // pour rejeter tout commissaire qui n'est pas celui de la ligue.
        await using var setupDb = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("secv-comm");
        var autreComm   = DataSeeder.CreateUser("secv-autre");
        var coach1 = DataSeeder.CreateUser("secv-c1");
        var coach2 = DataSeeder.CreateUser("secv-c2");
        setupDb.Users.AddRange(commissaire, autreComm, coach1, coach2);
        await setupDb.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(setupDb);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(setupDb, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(setupDb, game.Id, rv.Id, commissaire.Id);

        var div = new Division { LeagueId = ligue.Id, Nom = "Div", Ordre = 1 };
        setupDb.Divisions.Add(div);
        await setupDb.SaveChangesAsync();

        var t1 = await DataSeeder.SeedTeamAsync(setupDb, ligue.Id, coach1.Id, teamType.Id, "Dom");
        var t2 = await DataSeeder.SeedTeamAsync(setupDb, ligue.Id, coach2.Id, teamType.Id, "Ext");
        var match = await DataSeeder.SeedMatchAsync(setupDb, t1.Id, t2.Id, div.Id);

        await using var readDb = _factory.CreateContext();
        var settings1 = new SettingsService(readDb);
        var svc = new MatchService(readDb, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings1, NullLogger<GmailEmailSender>.Instance), settings1);
        var loaded = await svc.GetMatchAsync(match.Id);

        Assert.NotNull(loaded);
        // Le commissaire légitime passe
        Assert.Equal(commissaire.Id, loaded.Division?.League?.CommissaireId);
        // Un autre commissaire est rejeté par le garde du composant
        Assert.NotEqual(autreComm.Id, loaded.Division?.League?.CommissaireId);
    }

    [Fact]
    public async Task GetMatchAsync_ExposeCoachIds_PourGardeFeuille()
    {
        // Vérifie que CoachId domicile/extérieur est chargé — utilisé par Feuille.razor
        // pour n'autoriser que les coachs des deux équipes du match.
        await using var setupDb = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("secf-comm");
        var coach1 = DataSeeder.CreateUser("secf-c1");
        var coach2 = DataSeeder.CreateUser("secf-c2");
        setupDb.Users.AddRange(commissaire, coach1, coach2);
        await setupDb.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(setupDb);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(setupDb, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(setupDb, game.Id, rv.Id, commissaire.Id);

        var div = new Division { LeagueId = ligue.Id, Nom = "Div", Ordre = 1 };
        setupDb.Divisions.Add(div);
        await setupDb.SaveChangesAsync();

        var t1 = await DataSeeder.SeedTeamAsync(setupDb, ligue.Id, coach1.Id, teamType.Id, "Dom");
        var t2 = await DataSeeder.SeedTeamAsync(setupDb, ligue.Id, coach2.Id, teamType.Id, "Ext");
        var match = await DataSeeder.SeedMatchAsync(setupDb, t1.Id, t2.Id, div.Id);

        await using var readDb = _factory.CreateContext();
        var settings2 = new SettingsService(readDb);
        var svc = new MatchService(readDb, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings2, NullLogger<GmailEmailSender>.Instance), settings2);
        var loaded = await svc.GetMatchAsync(match.Id);

        Assert.NotNull(loaded);
        Assert.Equal(coach1.Id, loaded.EquipeDomicile?.CoachId);
        Assert.Equal(coach2.Id, loaded.EquipeExterieur?.CoachId);

        // Un tiers ne passe pas le filtre (logique copiée du composant)
        var tiersId = Guid.NewGuid().ToString();
        var estCoach = loaded.EquipeDomicile?.CoachId == tiersId
                    || loaded.EquipeExterieur?.CoachId == tiersId;
        Assert.False(estCoach);
    }
}
