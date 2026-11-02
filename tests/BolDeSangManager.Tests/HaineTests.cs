using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// #28 — « Prendre sa revanche » (LRB S3 p.42) : après un Amoché, une Blessure
/// sérieuse ou une Séquelle, le joueur peut gagner Haine (X). La Haine est un
/// TEXTE sur le joueur (« Orque,Nain »), indépendant du catalogue de règles :
/// elle marche en Dungeon Bowl, où aucune version n'a de Trait « Haine (…) ».
/// Saisie sur la feuille de match ET sur la page de l'équipe.
/// </summary>
public class HaineTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private sealed record Ctx(int JoueurDom, int JoueurExt, int MatchId, int TueurId, int VersionId);

    // Identifiants Identity (chaînes), renseignés par SetupAsync.
    private string _coachDom = "", _coachExt = "", _commissaire = "";

    /// <summary>
    /// Une ligue SANS aucun Trait « Haine (…) » au catalogue, sauf la Haine (Troll)
    /// du profil du Tueur de Trolls — c'est le cas réel de Dungeon Bowl 2022.
    /// </summary>
    private async Task<Ctx> SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("hcomm");
        var coach1 = DataSeeder.CreateUser("hcoach1");
        var coach2 = DataSeeder.CreateUser("hcoach2");
        db.Users.AddRange(commissaire, coach1, coach2);
        await db.SaveChangesAsync();
        (_coachDom, _coachExt, _commissaire) = (coach1.Id, coach2.Id, commissaire.Id);

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        position.MotsCles = "Humain,Trois-quart";
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        var div = new Division { LeagueId = ligue.Id, Nom = "Division Unique", Ordre = 1 };
        db.Divisions.Add(div);
        await db.SaveChangesAsync();

        var dom = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "Dom");
        var ext = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "Ext");
        dom.DivisionId = div.Id;
        ext.DivisionId = div.Id;
        await db.SaveChangesAsync();

        // Tueur de Trolls : Haine (Troll) dans le PROFIL de son poste.
        var cat = await DataSeeder.GetOrCreateCategorieAsync(db, rv.Id);
        var troll = new Skill { RulesVersionId = rv.Id, Nom = "Haine (Troll)", Categorie = SkillCategory.Generale,
            SkillCategoryDefId = cat, EstTrait = true };
        var postTueur = new PlayerPosition { TeamTypeId = teamType.Id, Nom = "Tueur de Troll", QuantiteMax = 2,
            Cout = 95_000, MotsCles = "Nain,Spécial" };
        db.AddRange(troll, postTueur);
        await db.SaveChangesAsync();
        db.PlayerPositionSkills.Add(new PlayerPositionSkill { PlayerPositionId = postTueur.Id, SkillId = troll.Id });
        await db.SaveChangesAsync();

        var joueurDom = await DataSeeder.SeedPlayerAsync(db, dom.Id, position.Id, "JD", 1);
        var joueurExt = await DataSeeder.SeedPlayerAsync(db, ext.Id, position.Id, "JE", 1);
        var tueur = await DataSeeder.SeedPlayerAsync(db, dom.Id, postTueur.Id, "Tueur", 2);

        var match = await DataSeeder.SeedMatchAsync(db, dom.Id, ext.Id, div.Id);
        return new Ctx(joueurDom.Id, joueurExt.Id, match.Id, tueur.Id, rv.Id);
    }

    private static MatchService Match(ApplicationDbContext db)
    {
        var settings = new SettingsService(db);
        return new(db, NullLogger<MatchService>.Instance,
            new GmailEmailSender(settings, NullLogger<GmailEmailSender>.Instance), settings);
    }

    private static HaineService Haine(ApplicationDbContext db, bool estCommissaire = false) =>
        new(db, new StubAuth(estCommissaire), NullLogger<HaineService>.Instance);

    private static MatchSheet FeuilleNulle() => new() { TouchdownsDomicile = 0, TouchdownsExterieur = 0 };

    private async Task<string> HainesAsync(int joueurId)
    {
        await using var db = _factory.CreateContext();
        return (await db.TeamPlayers.FindAsync(joueurId))!.Haines;
    }

    private static MatchPlayerRecord Blesse(int joueurId, InjuryType? type, string? haine) => new()
    {
        TeamPlayerId = joueurId, EstCoteDomicile = true, Blessure = type,
        StatAffectee = type == InjuryType.Sequelle ? AffectedStat.Force : null, HaineCible = haine
    };

    // ─── Règles de texte ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("  Orque ", "Orque")]
    [InlineData("Haine (Mort-Vivant)", "Mort-Vivant")]
    [InlineData("Homme   Lézard", "Homme Lézard")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normaliser(string? saisie, string? attendu) =>
        Assert.Equal(attendu, HaineHelpers.Normaliser(saisie));

    [Fact]
    public void Normaliser_RefuseVirguleEtTexteTropLong()
    {
        Assert.Throws<InvalidOperationException>(() => HaineHelpers.Normaliser("Orque, Nain"));
        Assert.Throws<InvalidOperationException>(() => HaineHelpers.Normaliser(new string('x', 41)));
    }

    [Fact]
    public void RetirerUneSeuleOccurrence_InsensibleALaCasse() =>
        Assert.Equal("Nain,Orque", HaineHelpers.Retirer("Orque,Nain,Orque", "orque"));

    // ─── Suggestions : indépendantes des Traits du catalogue ──────────────────

    [Fact]
    public async Task Suggestions_MotsClesDeLaVersion_SansMotsClesDePoste()
    {
        var c = await SetupAsync();
        await using var db = _factory.CreateContext();
        var suggestions = await Haine(db).GetSuggestionsAsync(c.VersionId);
        // Humain, Nain : mots-clés des postes ; Troll : Trait du catalogue.
        // Trois-quart et Spécial sont exclus par le livre.
        Assert.Equal(["Humain", "Nain", "Troll"], suggestions);
    }

    [Fact]
    public async Task Suggestions_DungeonBowlSansAucunTrait()
    {
        await using var db = _factory.CreateContext();
        var game = new Game { Nom = "Dungeon Bowl", Type = GameType.DungeonBowl };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var v = new RulesVersion { GameId = game.Id, Nom = "Edition 2022", Ordre = 1 };
        db.RulesVersions.Add(v);
        await db.SaveChangesAsync();
        var college = new TeamType { GameId = game.Id, RulesVersionId = v.Id, Nom = "Collège du Feu" };
        db.TeamTypes.Add(college);
        await db.SaveChangesAsync();
        db.PlayerPositions.AddRange(
            new PlayerPosition { TeamTypeId = college.Id, Nom = "Gros Bras", MotsCles = "Orque,Gros Bras" },
            new PlayerPosition { TeamTypeId = college.Id, Nom = "Lanceur", MotsCles = "Skaven,Lanceur" });
        await db.SaveChangesAsync();

        Assert.Equal(["Orque", "Skaven"], await Haine(db).GetSuggestionsAsync(v.Id));
    }

    // ─── Page de l'équipe ─────────────────────────────────────────────────────

    [Fact]
    public async Task Coach_AjouteUneHaineLibre_SansChangerLaValeur()
    {
        var c = await SetupAsync();
        int valeurAvant;
        await using (var db = _factory.CreateContext())
        {
            var j = await db.TeamPlayers.Include(p => p.PlayerPosition).Include(p => p.Improvements)
                .SingleAsync(p => p.Id == c.JoueurDom);
            valeurAvant = ValeurJoueurCalculator.Calculer(j, BaremeAmelioration.DeVersion(null));
        }

        // « Orque » n'est NI un Trait du catalogue NI un mot-clé de la version :
        // la saisie reste libre.
        await using (var db = _factory.CreateContext())
            await Haine(db).AjouterAsync(c.JoueurDom, "Orque", _coachDom);
        await using (var db = _factory.CreateContext())
            await Haine(db).AjouterAsync(c.JoueurDom, "Haine (Nain)", _coachDom);

        Assert.Equal("Orque,Nain", await HainesAsync(c.JoueurDom));
        await using (var db = _factory.CreateContext())
        {
            var j = await db.TeamPlayers.Include(p => p.PlayerPosition).Include(p => p.Improvements)
                .SingleAsync(p => p.Id == c.JoueurDom);
            Assert.Equal(valeurAvant, ValeurJoueurCalculator.Calculer(j, BaremeAmelioration.DeVersion(null)));
            Assert.Empty(j.Improvements);
            Assert.False(await db.TeamPlayerSkills.AnyAsync(s => s.TeamPlayerId == c.JoueurDom));
        }
    }

    [Fact]
    public async Task CommissaireDeLigue_PeutAjouter_UnAutreCoachNon()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Haine(db, estCommissaire: false).AjouterAsync(c.JoueurDom, "Orque", _coachExt));
            Assert.Contains("coach de l'équipe", ex.Message);
        }
        Assert.Equal("", await HainesAsync(c.JoueurDom));

        await using (var db = _factory.CreateContext())
            await Haine(db, estCommissaire: true).AjouterAsync(c.JoueurDom, "Orque", _commissaire);
        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));
    }

    [Fact]
    public async Task Ajouter_RefuseDoublon_Vide_HaineDuProfil_EtJoueurMort()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Haine(db).AjouterAsync(c.JoueurDom, "Orque", _coachDom);

        await using (var db = _factory.CreateContext())
        {
            var svc = Haine(db);
            var doublon = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AjouterAsync(c.JoueurDom, "orque", _coachDom));
            Assert.Contains("déjà", doublon.Message);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AjouterAsync(c.JoueurDom, "  ", _coachDom));
            // Haine (Troll) est déjà dans le profil du Tueur.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AjouterAsync(c.TueurId, "Troll", _coachDom));
        }

        await using (var db = _factory.CreateContext())
        {
            (await db.TeamPlayers.FindAsync(c.JoueurDom))!.EstMort = true;
            await db.SaveChangesAsync();
        }
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Haine(db).AjouterAsync(c.JoueurDom, "Nain", _coachDom));

        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));
        Assert.Equal("", await HainesAsync(c.TueurId));
    }

    [Fact]
    public async Task Retirer_EnleveLaHaineGagnee_PasCelleDuProfil()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
        {
            await Haine(db).AjouterAsync(c.JoueurDom, "Orque", _coachDom);
            await Haine(db).AjouterAsync(c.JoueurDom, "Nain", _coachDom);
        }
        await using (var db = _factory.CreateContext())
            await Haine(db).RetirerAsync(c.JoueurDom, "Orque", _coachDom);
        Assert.Equal("Nain", await HainesAsync(c.JoueurDom));

        await using (var db = _factory.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Haine(db).RetirerAsync(c.TueurId, "Troll", _coachDom));
            Assert.Contains("profil", ex.Message);
        }
    }

    // ─── Feuille de match ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(InjuryType.ManqueSuivant)]
    [InlineData(InjuryType.BlessurePersistante)]
    [InlineData(InjuryType.Sequelle)]
    public async Task Feuille_BlessureQualifiante_DonneLaHaine(InjuryType type)
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Match(db).SaisirFeuilleMatchAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, type, " Haine (Orque) ")], _coachDom);

        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));
        await using var db2 = _factory.CreateContext();
        Assert.Equal("Orque", (await db2.MatchPlayerRecords.SingleAsync()).HaineCible);
    }

    [Fact]
    public async Task Feuille_HaineSansBlessureQualifiante_EstIgnoree()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Match(db).SaisirFeuilleMatchAsync(c.MatchId, FeuilleNulle(),
                [
                    Blesse(c.JoueurDom, null, "Orque"),
                    new() { TeamPlayerId = c.JoueurExt, EstCoteDomicile = false,
                            Blessure = InjuryType.Mort, HaineCible = "Orque" }
                ],
                _coachDom);

        Assert.Equal("", await HainesAsync(c.JoueurDom));
        Assert.Equal("", await HainesAsync(c.JoueurExt));
        await using var db2 = _factory.CreateContext();
        Assert.All(await db2.MatchPlayerRecords.ToListAsync(), r => Assert.Null(r.HaineCible));
    }

    [Fact]
    public async Task Feuille_HaineInvalide_EstRefuseeSansRienEcrire()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Match(db).SaisirFeuilleMatchAsync(c.MatchId, FeuilleNulle(),
                    [Blesse(c.JoueurDom, InjuryType.ManqueSuivant, "Orque, Nain")], _coachDom));

        await using var db2 = _factory.CreateContext();
        Assert.False(await db2.MatchSheets.AnyAsync());
        Assert.Equal("", await HainesAsync(c.JoueurDom));
    }

    [Fact]
    public async Task Correction_RetireOuRemplaceLaHaineDeLaFeuille_SansDoublon()
    {
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Match(db).SaisirFeuilleMatchAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, InjuryType.ManqueSuivant, "Orque")], _coachDom);

        // Correction à l'identique : toujours UNE Haine (Orque).
        await using (var db = _factory.CreateContext())
            await Match(db).ModifierFeuilleAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, InjuryType.ManqueSuivant, "Orque")]);
        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));

        // Correction : c'était un Nain.
        await using (var db = _factory.CreateContext())
            await Match(db).ModifierFeuilleAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, InjuryType.ManqueSuivant, "Nain")]);
        Assert.Equal("Nain", await HainesAsync(c.JoueurDom));

        // Correction : plus de blessure du tout.
        await using (var db = _factory.CreateContext())
            await Match(db).ModifierFeuilleAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, null, null)]);
        Assert.Equal("", await HainesAsync(c.JoueurDom));
    }

    [Fact]
    public async Task Feuille_HaineDejaPossedee_NiDoublon_NiRetraitALaCorrection()
    {
        // Le joueur avait déjà Haine (Orque), ajoutée sur la page de l'équipe.
        // La feuille ne l'ajoute pas une 2e fois — et donc, corriger la feuille
        // ne doit pas lui retirer la Haine qu'il avait AVANT ce match.
        var c = await SetupAsync();
        await using (var db = _factory.CreateContext())
            await Haine(db).AjouterAsync(c.JoueurDom, "Orque", _coachDom);

        await using (var db = _factory.CreateContext())
            await Match(db).SaisirFeuilleMatchAsync(c.MatchId, FeuilleNulle(),
                [Blesse(c.JoueurDom, InjuryType.ManqueSuivant, "orque")], _coachDom);
        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));

        await using (var db = _factory.CreateContext())
            await Match(db).ModifierFeuilleAsync(c.MatchId, FeuilleNulle(), [Blesse(c.JoueurDom, null, null)]);
        Assert.Equal("Orque", await HainesAsync(c.JoueurDom));
    }

    private sealed class StubAuth(bool commissaire) : IAuthorizationService
    {
        public Task<bool> EstAdminAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstGrandCommissaireAsync(string userId) => Task.FromResult(false);
        public Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId) => Task.FromResult(commissaire);
        public Task<bool> PeutGererLigueAsync(string userId, int ligueId) => Task.FromResult(commissaire);
        public Task<bool> PeutEditerDonneesAsync(string userId) => Task.FromResult(false);
        public Task<bool> PeutGererSettingsAsync(string userId) => Task.FromResult(false);
    }
}
