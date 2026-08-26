// Jeu de données de démonstration pour tester les 3 devs en local.
// Idempotent : relancer le script ne duplique rien.
//
// Créé :
//   - 3 comptes coach (mot de passe commun)
//   - une ligue Open « Open Bol de Sang » avec 3 équipes complètes
//   - une ligue Round Robin « Saison Test » en Inscription (dev 3 : dater les
//     rondes et promouvoir un commissaire avant le lancement)
//   - un staff maison « Chef de bande » dans les règles (dev 1)

using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string MotDePasse = "Coach123!";

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlite("DataSource=../../src/BolDeSangManager/Data/boldesang.db;Cache=Shared"));
services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
services.AddScoped<StaffService>();

var sp = services.BuildServiceProvider();
using var scope = sp.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var staffSvc = scope.ServiceProvider.GetRequiredService<StaffService>();

// ── Comptes coach ─────────────────────────────────────────────────────────────
async Task<ApplicationUser> CoachAsync(string email, string pseudo)
{
    var u = await users.FindByEmailAsync(email);
    if (u is not null) return u;

    u = new ApplicationUser
    {
        UserName = email, Email = email, EmailConfirmed = true,
        PseudoCoach = pseudo, CreeLe = DateTime.UtcNow
    };
    var res = await users.CreateAsync(u, MotDePasse);
    if (!res.Succeeded)
        throw new Exception($"Création {email} : {string.Join(", ", res.Errors.Select(e => e.Description))}");
    await users.AddToRoleAsync(u, "Coach");
    Console.WriteLine($"  + coach {email}");
    return u;
}

var c1 = await CoachAsync("coach1@test.fr", "Ragnar");
var c2 = await CoachAsync("coach2@test.fr", "Silvara");
var c3 = await CoachAsync("coach3@test.fr", "Grim");

var commissaire = await users.FindByEmailAsync("commissaire@boldesang.fr")
    ?? throw new Exception("Compte commissaire introuvable");

var game = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
var rv = await db.RulesVersions.FirstAsync(v => v.GameId == game.Id && v.EstActive);

// ── Dev 1 : un staff maison, pour montrer la liste ouverte ────────────────────
if (!await db.StaffTypes.AnyAsync(s => s.RulesVersionId == rv.Id && s.Nom == "Chef de bande"))
{
    await staffSvc.AjouterStaffTypeAsync(new StaffDefinition
    {
        RulesVersionId = rv.Id,
        Nom = "Chef de bande",
        Description = "Relance un jet d'intimidation par mi-temps.",
        Ordre = 6, Cout = 25_000, MinCreation = 0, MaxCreation = 2, MaxLigue = 3
    });
    Console.WriteLine("  + staff « Chef de bande » (règles)");
}

// ── Ligues ────────────────────────────────────────────────────────────────────
async Task<League> LigueAsync(string nom, LeagueFormat format, LeagueStatus statut, string description)
{
    var l = await db.Leagues.FirstOrDefaultAsync(x => x.Nom == nom);
    if (l is not null) return l;

    l = new League
    {
        Nom = nom, Description = description,
        CommissaireId = commissaire.Id,
        GameId = game.Id, RulesVersionId = rv.Id,
        Format = format, Statut = statut,
        BudgetDepart = 1_000_000, NombreEquipesPlayoff = 4,
        CreeLe = DateTime.UtcNow
    };
    db.Leagues.Add(l);
    await db.SaveChangesAsync();
    await staffSvc.CopierVersLigueAsync(l.Id, rv.Id);
    Console.WriteLine($"  + ligue « {nom} » ({format}, {statut})");
    return l;
}

var open = await LigueAsync("Open Bol de Sang", LeagueFormat.Open, LeagueStatus.EnCours,
    "Ligue sans fin : inscrivez-vous quand vous voulez, jouez quand vous voulez.");

var saison = await LigueAsync("Saison Test", LeagueFormat.RoundRobin, LeagueStatus.Inscription,
    "Round Robin en phase d'inscription : préparez les dates avant de lancer.");

// Division technique de la ligue Open (sinon la suppression laisse des orphelins)
if (!await db.Divisions.AnyAsync(d => d.LeagueId == open.Id))
{
    db.Divisions.Add(new Division { LeagueId = open.Id, Nom = "Division Unique", Ordre = 1 });
    await db.SaveChangesAsync();
}
var divOpen = await db.Divisions.FirstAsync(d => d.LeagueId == open.Id);

// ── Équipes ───────────────────────────────────────────────────────────────────
async Task EquipeAsync(League ligue, ApplicationUser coach, string nom, string race,
    int fans, int relances, int coachs, int cheer, bool apo, int? divisionId)
{
    if (await db.Teams.AnyAsync(t => t.Nom == nom)) return;

    var tt = await db.TeamTypes
        .Include(t => t.Postes)
        .FirstAsync(t => t.RulesVersionId == rv.Id && t.Nom == race);

    var equipe = new Team
    {
        Nom = nom, CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = tt.Id,
        DivisionId = divisionId, Tresorerie = 0, CreeLe = DateTime.UtcNow
    };
    db.Teams.Add(equipe);
    await db.SaveChangesAsync();

    // 11 joueurs sur le poste de base le moins cher autorisé en nombre
    var poste = tt.Postes.Where(p => p.QuantiteMax >= 11).OrderBy(p => p.Cout).First();
    var depense = 0;
    for (var i = 1; i <= 11; i++)
    {
        db.TeamPlayers.Add(new TeamPlayer
        {
            TeamId = equipe.Id, PlayerPositionId = poste.Id,
            Nom = $"{poste.Nom} #{i}", Numero = i,
            ValeurActuelle = poste.Cout, RecruteLe = DateTime.UtcNow
        });
        depense += poste.Cout;
    }

    // Staff, via les copies de la ligue. On n'achète que ce que le budget permet :
    // les races chères (Nains) partiraient sinon en trésorerie négative.
    var types = await db.LeagueStaffTypes.Where(l => l.LeagueId == ligue.Id).ToListAsync();
    var achats = new List<(LeagueStaffType type, int qte)>();
    foreach (var (nomStaff, qteVoulue) in new[]
             {
                 (StaffService.NomFans, fans), (StaffService.NomRelances, relances),
                 (StaffService.NomCoachs, coachs), (StaffService.NomCheerleaders, cheer),
                 (StaffService.NomApothicaire, apo ? 1 : 0)
             })
    {
        var type = types.FirstOrDefault(t => t.Nom == nomStaff);
        if (type is null || qteVoulue <= 0) continue;

        var unitaire = StaffService.CoutUnitaire(type, tt);
        var possible = unitaire <= 0
            ? qteVoulue
            : Math.Min(qteVoulue, (ligue.BudgetDepart - depense) / unitaire);
        if (possible <= 0) continue;

        achats.Add((type, possible));
        depense += possible * unitaire;
    }

    foreach (var (type, qte) in achats)
        db.TeamStaffs.Add(new TeamStaff
        {
            TeamId = equipe.Id, LeagueStaffTypeId = type.Id, Quantite = qte
        });

    int Qte(string nomStaff) =>
        achats.FirstOrDefault(a => a.type.Nom == nomStaff).qte;

    equipe.FansDevoues = Qte(StaffService.NomFans);
    equipe.NombreRelances = Qte(StaffService.NomRelances);
    equipe.NombreCoachsAssistants = Qte(StaffService.NomCoachs);
    equipe.NombreCheerleaders = Qte(StaffService.NomCheerleaders);
    equipe.Apothicaire = Qte(StaffService.NomApothicaire) > 0;
    equipe.Tresorerie = ligue.BudgetDepart - depense;

    await db.SaveChangesAsync();
    Console.WriteLine($"  + équipe « {nom} » ({race}) — trésorerie {equipe.Tresorerie:N0} po");
}

await EquipeAsync(open, c1, "Les Crocs d'Acier", "Nains", 5, 3, 1, 1, true, divOpen.Id);
await EquipeAsync(open, c2, "Flèches Sylvestres", "Elfes Sylvains", 4, 2, 0, 2, true, divOpen.Id);
await EquipeAsync(open, c3, "Horde Verte", "Orques", 6, 3, 2, 0, false, divOpen.Id);

// Une seule équipe en Saison Test : il en faut 2 pour lancer, à toi d'en créer une.
await EquipeAsync(saison, c1, "Les Bretteurs", "Humains", 3, 2, 1, 1, false, null);

Console.WriteLine();
Console.WriteLine("Jeu de données prêt.");
