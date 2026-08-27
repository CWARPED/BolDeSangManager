using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

/// <summary>
/// Diffusion temps réel des changements de ligue aux écrans ouverts.
///
/// Contexte : une page ligue ne lisait la base qu'à son ouverture. Un coach qui
/// gardait l'écran ouvert pendant que le commissaire lançait la saison restait
/// bloqué sur « Inscriptions », sans le bloc « Proposer une rencontre ».
/// </summary>
public class LeagueNotificationServiceTests
{
    [Fact]
    public async Task Notifier_PrevientLesAbonnes_AvecLIdentifiantDeLaLigue()
    {
        var svc = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var recus = new List<int>();
        svc.LigueModifiee += id => { recus.Add(id); return Task.CompletedTask; };

        await svc.NotifierAsync(42);

        Assert.Equal([42], recus);
    }

    /// <summary>
    /// Le cas d'usage réel : plusieurs coaches ont la même ligue ouverte, tous
    /// doivent être prévenus — pas seulement celui qui a agi.
    /// </summary>
    [Fact]
    public async Task Notifier_PrevientTousLesAbonnes()
    {
        var svc = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var appels = 0;
        svc.LigueModifiee += _ => { appels++; return Task.CompletedTask; };
        svc.LigueModifiee += _ => { appels++; return Task.CompletedTask; };
        svc.LigueModifiee += _ => { appels++; return Task.CompletedTask; };

        await svc.NotifierAsync(1);

        Assert.Equal(3, appels);
    }

    /// <summary>
    /// Un écran dont le circuit est déjà fermé lève au rafraîchissement. Il ne
    /// doit ni interrompre la diffusion aux autres, ni remonter jusqu'à
    /// l'action métier qui vient pourtant de réussir en base.
    /// </summary>
    [Fact]
    public async Task Notifier_UnAbonneEnErreur_NEmpechePasLesAutres()
    {
        var svc = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var sain = 0;
        svc.LigueModifiee += _ => throw new InvalidOperationException("circuit fermé");
        svc.LigueModifiee += _ => { sain++; return Task.CompletedTask; };

        await svc.NotifierAsync(1);   // ne doit pas lever

        Assert.Equal(1, sain);
    }

    /// <summary>
    /// Sans désabonnement, le singleton garderait en vie des pages fermées et
    /// tenterait de les rendre. Dispose doit réellement couper le lien.
    /// </summary>
    [Fact]
    public async Task Desabonnement_ArreteLesNotifications()
    {
        var svc = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var appels = 0;
        Func<int, Task> abonne = _ => { appels++; return Task.CompletedTask; };

        svc.LigueModifiee += abonne;
        await svc.NotifierAsync(1);
        svc.LigueModifiee -= abonne;
        await svc.NotifierAsync(1);

        Assert.Equal(1, appels);
    }

    [Fact]
    public async Task Notifier_SansAucunAbonne_NeLevePas()
    {
        var svc = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        await svc.NotifierAsync(1);
    }
}

/// <summary>
/// Le maillon qui compte vraiment : LeagueService doit émettre la notification
/// quand il change l'état d'une ligue. Sans ça le service de diffusion est
/// correct mais personne ne l'appelle, et la désynchronisation persiste.
/// </summary>
public class LeagueServiceNotificationTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static LeagueService CreateService(
        ApplicationDbContext db, LeagueNotificationService notifications) =>
        new(db, NullLogger<LeagueService>.Instance, new StubAuth(),
            new StaffService(db, NullLogger<StaffService>.Instance), notifications);

    private async Task<(int ligueId, int gameId, int rvId, string coachId)> SetupAsync()
    {
        await using var db = _factory.CreateContext();
        var commissaire = DataSeeder.CreateUser("comm-notif");
        var coach = DataSeeder.CreateUser("coach-notif");
        db.Users.AddRange(commissaire, coach);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);
        await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "A");
        await DataSeeder.SeedTeamAsync(db, ligue.Id, commissaire.Id, teamType.Id, "B");
        return (ligue.Id, game.Id, rv.Id, coach.Id);
    }

    /// <summary>
    /// Le scénario signalé : le commissaire lance la saison, les écrans ouverts
    /// doivent être prévenus.
    /// </summary>
    [Fact]
    public async Task LancerSaison_NotifieLesEcransOuverts()
    {
        var (ligueId, _, _, _) = await SetupAsync();
        var notifications = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var recus = new List<int>();
        notifications.LigueModifiee += id => { recus.Add(id); return Task.CompletedTask; };

        await using var db = _factory.CreateContext();
        await CreateService(db, notifications).LancerSaisonAsync(ligueId);

        Assert.Contains(ligueId, recus);
    }

    [Fact]
    public async Task DemarrerInscriptions_NotifieLesEcransOuverts()
    {
        var (ligueId, _, _, _) = await SetupAsync();
        var notifications = new LeagueNotificationService(NullLogger<LeagueNotificationService>.Instance);
        var recus = new List<int>();
        notifications.LigueModifiee += id => { recus.Add(id); return Task.CompletedTask; };

        await using var db = _factory.CreateContext();
        await CreateService(db, notifications).DemarrerInscriptionsAsync(ligueId);

        Assert.Contains(ligueId, recus);
    }

    /// <summary>
    /// Le service reste utilisable sans diffusion (tests, appels internes) :
    /// une notification absente ne doit jamais casser l'opération métier.
    /// </summary>
    [Fact]
    public async Task SansServiceDeNotification_LOperationMetierReussitQuandMeme()
    {
        var (ligueId, _, _, _) = await SetupAsync();

        await using var db = _factory.CreateContext();
        var svc = new LeagueService(db, NullLogger<LeagueService>.Instance, new StubAuth(),
            new StaffService(db, NullLogger<StaffService>.Instance));   // pas de notifications

        await svc.LancerSaisonAsync(ligueId);

        await using var db2 = _factory.CreateContext();
        Assert.Equal(LeagueStatus.EnCours, (await db2.Leagues.FindAsync(ligueId))!.Statut);
    }

    /// <summary>
    /// La vraie cause du bug, trouvée en test à deux navigateurs : la
    /// notification arrivait bien, mais l'écran relisait l'ANCIEN statut. Le
    /// DbContext vit aussi longtemps que le circuit Blazor (donc que l'onglet),
    /// et EF renvoyait l'entité chargée à l'ouverture au lieu d'interroger la
    /// base. Sans ignorerCache, la page se « rafraîchit » sans rien changer.
    /// </summary>
    [Fact]
    public async Task GetLigue_AvecIgnorerCache_VoitUnChangementFaitAilleurs()
    {
        var (ligueId, _, _, _) = await SetupAsync();

        // Le contexte de l'écran ouvert : il charge la ligue, puis la garde.
        await using var dbEcran = _factory.CreateContext();
        var svcEcran = CreateService(dbEcran, new LeagueNotificationService(
            NullLogger<LeagueNotificationService>.Instance));
        var avant = await svcEcran.GetLigueAsync(ligueId);
        Assert.Equal(LeagueStatus.Inscription, avant!.Statut);

        // Un AUTRE utilisateur lance la saison, sur son propre contexte.
        await using (var dbAutre = _factory.CreateContext())
        {
            await CreateService(dbAutre, new LeagueNotificationService(
                NullLogger<LeagueNotificationService>.Instance)).LancerSaisonAsync(ligueId);
        }

        // Sans vidage du cache, l'écran relit l'ancienne instance…
        var sansVidage = await svcEcran.GetLigueAsync(ligueId);
        Assert.Equal(LeagueStatus.Inscription, sansVidage!.Statut);

        // …avec, il voit enfin le changement.
        var avecVidage = await svcEcran.GetLigueAsync(ligueId, ignorerCache: true);
        Assert.Equal(LeagueStatus.EnCours, avecVidage!.Statut);
    }

    private class StubAuth : BolDeSangManager.Services.IAuthorizationService
    {
        public Task<bool> EstAdminAsync(string userId) => Task.FromResult(true);
        public Task<bool> EstGrandCommissaireAsync(string userId) => Task.FromResult(true);
        public Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId) => Task.FromResult(true);
        public Task<bool> PeutGererLigueAsync(string userId, int ligueId) => Task.FromResult(true);
        public Task<bool> PeutEditerDonneesAsync(string userId) => Task.FromResult(true);
        public Task<bool> PeutGererSettingsAsync(string userId) => Task.FromResult(true);
    }
}
