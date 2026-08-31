using System.Text;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BolDeSangManager.Tests;

/// <summary>
/// Abonnement iCalendar par URL secrète (option A).
///
/// Deux propriétés critiques y sont vérifiées :
///  • le jeton est un VRAI secret (aléatoire cryptographique, jamais réémis
///    par hasard, invalidé à la régénération et à la suppression de compte) ;
///  • le flux public applique le MODE BROUILLARD, comme tous les autres chemins
///    de lecture. Un export qui le contournerait suffirait à faire tomber le
///    brouillard : il suffirait de s'abonner pour voir tout le calendrier.
/// </summary>
public class AbonnementCalendrierServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static AbonnementCalendrierService Svc(ApplicationDbContext db) =>
        new(db, new CalendrierService());

    // ── Jeton ────────────────────────────────────────────────────────────────

    [Fact]
    public void NouveauJeton_EstUrlSafe_EtAssezLong()
    {
        var jeton = AbonnementCalendrierService.NouveauJeton();

        // 32 octets en base64 ⇒ 43 caractères une fois le padding retiré.
        Assert.Equal(43, jeton.Length);
        Assert.DoesNotContain('+', jeton);
        Assert.DoesNotContain('/', jeton);
        Assert.DoesNotContain('=', jeton);
        Assert.Equal(jeton, Uri.EscapeDataString(jeton));   // utilisable tel quel dans une URL
    }

    [Fact]
    public void NouveauJeton_NeSeRepeteJamais()
    {
        var jetons = Enumerable.Range(0, 200)
            .Select(_ => AbonnementCalendrierService.NouveauJeton())
            .ToHashSet();

        Assert.Equal(200, jetons.Count);
    }

    [Fact]
    public async Task ObtenirOuCreer_CreeUneSeuleFois_PuisRenvoieLeMeme()
    {
        var db = _factory.CreateContext();
        var user = DataSeeder.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var premier = await Svc(db).ObtenirOuCreerJetonAsync(user.Id);
        var second = await Svc(db).ObtenirOuCreerJetonAsync(user.Id);

        Assert.False(string.IsNullOrEmpty(premier));
        // L'URL doit être STABLE : la régénérer à chaque affichage casserait
        // tous les abonnements déjà collés dans les agendas.
        Assert.Equal(premier, second);
    }

    [Fact]
    public async Task Regenerer_InvalideLAncienLien()
    {
        var db = _factory.CreateContext();
        var user = DataSeeder.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ancien = await Svc(db).ObtenirOuCreerJetonAsync(user.Id);
        var nouveau = await Svc(db).RegenererJetonAsync(user.Id);

        Assert.NotEqual(ancien, nouveau);
        Assert.Null(await Svc(db).TrouverParJetonAsync(ancien));
        Assert.NotNull(await Svc(db).TrouverParJetonAsync(nouveau));
    }

    [Fact]
    public async Task JetonInconnuOuVide_NeCorrespondAAucunCompte()
    {
        var db = _factory.CreateContext();
        var user = DataSeeder.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Assert.Null(await Svc(db).TrouverParJetonAsync("nimportequoi"));

        // Un jeton vide ne doit JAMAIS matcher : sinon tout compte n'ayant
        // jamais demandé d'abonnement serait exposé par « /calendrier/.ics ».
        Assert.Null(await Svc(db).TrouverParJetonAsync(""));
        Assert.Null(await Svc(db).TrouverParJetonAsync(null));
    }

    [Fact]
    public async Task CompteAnonymise_NeRepondPlus()
    {
        var db = _factory.CreateContext();
        var user = DataSeeder.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jeton = await Svc(db).ObtenirOuCreerJetonAsync(user.Id);
        Assert.NotNull(await Svc(db).TrouverParJetonAsync(jeton));

        user.EstSupprime = true;
        await db.SaveChangesAsync();

        Assert.Null(await Svc(db).TrouverParJetonAsync(jeton));
    }

    [Fact]
    public async Task Flux_JetonInconnu_NeProduitAucunContenu()
    {
        var db = _factory.CreateContext();

        // Null ⇒ l'endpoint répond 404. Surtout, PAS un calendrier vide :
        // un 200 confirmerait au curieux que la route existe et fonctionne.
        Assert.Null(await Svc(db).GenererFluxAsync("inconnu"));
    }

    // ── Contenu du flux : mode brouillard ────────────────────────────────────

    /// <summary>
    /// Ligue à 4 équipes : le coach testé possède l'équipe A.
    /// Ronde 1 jouée, rondes 2 et 3 à venir. Tous les matchs sont datés.
    /// </summary>
    private async Task<(ApplicationUser coach, League ligue, ApplicationDbContext db)>
        SeedLigueAsync(bool brouillard)
    {
        var db = _factory.CreateContext();
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);

        var coach = DataSeeder.CreateUser("coach");
        var commissaire = DataSeeder.CreateUser("commis");
        db.Users.AddRange(coach, commissaire);
        await db.SaveChangesAsync();

        var ligue = await DataSeeder.SeedLeagueAsync(
            db, game.Id, version.Id, commissaire.Id, LeagueStatus.EnCours);
        ligue.ModeBrouillard = brouillard;
        await db.SaveChangesAsync();

        var division = new Division { LeagueId = ligue.Id, Nom = "Division unique" };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        var a = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Les Marteaux");
        var b = await DataSeeder.SeedTeamAsync(db, ligue.Id, commissaire.Id, teamType.Id, "Les Charognards");
        var c = await DataSeeder.SeedTeamAsync(db, ligue.Id, commissaire.Id, teamType.Id, "Les Rats");
        var d = await DataSeeder.SeedTeamAsync(db, ligue.Id, commissaire.Id, teamType.Id, "Les Ogres");

        var baseDate = new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc);

        void Ajouter(int ronde, int dom, int ext, MatchStatus statut, int? score = null)
            => db.Matches.Add(new Match
            {
                DivisionId = division.Id,
                Ronde = ronde,
                EquipeDomicileId = dom,
                EquipeExterieurId = ext,
                Statut = statut,
                ScoreDomicile = score,
                ScoreExterieur = score is null ? null : 0,
                DateProgrammee = baseDate.AddDays(ronde * 7),
            });

        Ajouter(1, a.Id, b.Id, MatchStatus.Termine, 2);   // mon match joué
        Ajouter(2, a.Id, c.Id, MatchStatus.Programme);     // MON prochain match
        Ajouter(3, a.Id, d.Id, MatchStatus.Programme);     // mon match ULTÉRIEUR
        await db.SaveChangesAsync();

        return (coach, ligue, db);
    }

    [Fact]
    public async Task SansBrouillard_LeFluxContientTousLesMatchsDuCoach()
    {
        var (coach, _, db) = await SeedLigueAsync(brouillard: false);

        var matchs = await Svc(db).MatchsVisiblesAsync(coach.Id);

        Assert.Equal(3, matchs.Count);
    }

    [Fact]
    public async Task AvecBrouillard_LeFluxMasqueLesMatchsUlterieurs()
    {
        var (coach, _, db) = await SeedLigueAsync(brouillard: true);

        var matchs = await Svc(db).MatchsVisiblesAsync(coach.Id);

        // Ronde 1 (jouée) + ronde 2 (prochain match). La ronde 3 est masquée :
        // le coach ne doit pas préparer une rencontre qu'il ne voit pas encore
        // dans l'application — s'abonner ne doit pas être un contournement.
        Assert.Equal([1, 2], matchs.Select(m => m.Ronde).OrderBy(r => r).ToList());
    }

    [Fact]
    public async Task AvecBrouillard_LeFichierIcsLuiMemeNeContientPasLeMatchMasque()
    {
        var (coach, _, db) = await SeedLigueAsync(brouillard: true);

        var jeton = await Svc(db).ObtenirOuCreerJetonAsync(coach.Id);
        var ics = Encoding.UTF8.GetString((await Svc(db).GenererFluxAsync(jeton))!);

        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.Equal(2, ics.Split("BEGIN:VEVENT").Length - 1);

        // L'adversaire de la ronde 3 ne doit apparaître nulle part dans le flux.
        Assert.DoesNotContain("Les Ogres", ics);
        Assert.Contains("Les Rats", ics);        // ronde 2, visible
        Assert.Contains("Les Charognards", ics); // ronde 1, jouée
    }

    [Fact]
    public async Task LeFlux_NeContientQueLesMatchsDuCoach_PasCeuxDesAutres()
    {
        var (coach, ligue, db) = await SeedLigueAsync(brouillard: false);

        // Un match entre deux équipes tierces, auquel le coach ne participe pas.
        var division = await db.Divisions.FirstAsync(d => d.LeagueId == ligue.Id);
        var autres = await db.Teams.Where(t => t.CoachId != coach.Id).Take(2).ToListAsync();
        db.Matches.Add(new Match
        {
            DivisionId = division.Id,
            Ronde = 4,
            EquipeDomicileId = autres[0].Id,
            EquipeExterieurId = autres[1].Id,
            Statut = MatchStatus.Programme,
            DateProgrammee = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var matchs = await Svc(db).MatchsVisiblesAsync(coach.Id);

        Assert.DoesNotContain(4, matchs.Select(m => m.Ronde));
        Assert.Equal(3, matchs.Count);
    }

    [Fact]
    public async Task FluxParLigue_NeSortQueLesMatchsDeCetteLigue()
    {
        var (coach, ligue, db) = await SeedLigueAsync(brouillard: false);

        // Seconde ligue, même coach : elle ne doit pas polluer le flux ciblé.
        var game = await db.Games.FirstAsync();
        var version = await db.RulesVersions.FirstAsync();
        var teamType = await db.TeamTypes.FirstAsync();
        var commissaireId = ligue.CommissaireId!;

        var ligue2 = await DataSeeder.SeedLeagueAsync(
            db, game.Id, version.Id, commissaireId, LeagueStatus.EnCours);
        var div2 = new Division { LeagueId = ligue2.Id, Nom = "D2" };
        db.Divisions.Add(div2);
        await db.SaveChangesAsync();

        var e1 = await DataSeeder.SeedTeamAsync(db, ligue2.Id, coach.Id, teamType.Id, "Seconde equipe");
        var e2 = await DataSeeder.SeedTeamAsync(db, ligue2.Id, commissaireId, teamType.Id, "Adverse L2");
        db.Matches.Add(new Match
        {
            DivisionId = div2.Id,
            Ronde = 1,
            EquipeDomicileId = e1.Id,
            EquipeExterieurId = e2.Id,
            Statut = MatchStatus.Programme,
            DateProgrammee = new DateTime(2026, 11, 1, 18, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        var toutes = await Svc(db).MatchsVisiblesAsync(coach.Id);
        var ligue1Seule = await Svc(db).MatchsVisiblesAsync(coach.Id, ligue.Id);
        var ligue2Seule = await Svc(db).MatchsVisiblesAsync(coach.Id, ligue2.Id);

        Assert.Equal(4, toutes.Count);
        Assert.Equal(3, ligue1Seule.Count);
        Assert.Single(ligue2Seule);
    }

    [Fact]
    public async Task FluxParLigue_LigueInexistante_NeProduitAucunContenu()
    {
        var (coach, _, db) = await SeedLigueAsync(brouillard: false);
        var jeton = await Svc(db).ObtenirOuCreerJetonAsync(coach.Id);

        Assert.Null(await Svc(db).GenererFluxAsync(jeton, ligueId: 99_999));
    }

    [Fact]
    public async Task CoachSansEquipe_ObtientUnCalendrierVideMaisValide()
    {
        var db = _factory.CreateContext();
        var user = DataSeeder.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jeton = await Svc(db).ObtenirOuCreerJetonAsync(user.Id);
        var ics = Encoding.UTF8.GetString((await Svc(db).GenererFluxAsync(jeton))!);

        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.DoesNotContain("BEGIN:VEVENT", ics);
    }
}
