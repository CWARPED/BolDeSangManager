// Jeu de données QA : 6 coaches, une ligue par format, équipes complètes.
// Idempotent. Les ligues sont créées via LeagueService pour passer par le vrai
// chemin de code (copie du staff comprise).

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
        throw new Exception($"{email} : {string.Join(", ", res.Errors.Select(e => e.Description))}");
    await users.AddToRoleAsync(u, "Coach");
    Console.WriteLine($"  + coach {email}");
    return u;
}

var coaches = new List<ApplicationUser>();
foreach (var (mail, pseudo) in new[]
         {
             ("qa1@test.fr", "Ragnar"), ("qa2@test.fr", "Silvara"), ("qa3@test.fr", "Grim"),
             ("qa4@test.fr", "Ulrik"), ("qa5@test.fr", "Nyx"), ("qa6@test.fr", "Bran")
         })
    coaches.Add(await CoachAsync(mail, pseudo));

var commissaire = await users.FindByEmailAsync("commissaire@boldesang.fr")!
    ?? throw new Exception("commissaire introuvable");
var game = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
var rv = await db.RulesVersions.FirstAsync(v => v.GameId == game.Id && v.EstActive);

async Task<League> LigueAsync(string nom, LeagueFormat format, LeagueStatus statut, bool brouillard = false)
{
    var l = await db.Leagues.FirstOrDefaultAsync(x => x.Nom == nom);
    if (l is not null) return l;

    l = new League
    {
        Nom = nom, Description = $"QA — format {format}",
        CommissaireId = commissaire.Id, GameId = game.Id, RulesVersionId = rv.Id,
        Format = format, Statut = statut, ModeBrouillard = brouillard,
        BudgetDepart = 1_000_000, NombreEquipesPlayoff = 4, CreeLe = DateTime.UtcNow
    };
    db.Leagues.Add(l);
    await db.SaveChangesAsync();
    await staffSvc.CopierVersLigueAsync(l.Id, rv.Id);
    Console.WriteLine($"  + ligue « {nom} » ({format}, {statut})");
    return l;
}

async Task<Team> EquipeAsync(League ligue, ApplicationUser coach, string nom, string race, int? divisionId)
{
    var existante = await db.Teams.FirstOrDefaultAsync(t => t.Nom == nom);
    if (existante is not null) return existante;

    var tt = await db.TeamTypes.Include(t => t.Postes)
        .FirstAsync(t => t.RulesVersionId == rv.Id && t.Nom == race);

    var equipe = new Team
    {
        Nom = nom, CoachId = coach.Id, LeagueId = ligue.Id, TeamTypeId = tt.Id,
        DivisionId = divisionId, CreeLe = DateTime.UtcNow
    };
    db.Teams.Add(equipe);
    await db.SaveChangesAsync();

    var poste = tt.Postes.Where(p => p.QuantiteMax >= 11).OrderBy(p => p.Cout).First();
    var depense = 0;
    for (var i = 1; i <= 11; i++)
    {
        db.TeamPlayers.Add(new TeamPlayer
        {
            TeamId = equipe.Id, PlayerPositionId = poste.Id,
            Nom = $"J{i}", Numero = i, ValeurActuelle = poste.Cout, RecruteLe = DateTime.UtcNow
        });
        depense += poste.Cout;
    }

    var types = await db.LeagueStaffTypes.Where(l => l.LeagueId == ligue.Id).ToListAsync();
    foreach (var (nomStaff, voulu) in new[]
             {
                 (StaffService.NomFans, 3), (StaffService.NomRelances, 2),
                 (StaffService.NomApothicaire, 1)
             })
    {
        var type = types.FirstOrDefault(t => t.Nom == nomStaff);
        if (type is null) continue;
        var unitaire = StaffService.CoutUnitaire(type, tt);
        var qte = unitaire <= 0 ? voulu : Math.Min(voulu, (ligue.BudgetDepart - depense) / unitaire);
        if (qte <= 0) continue;

        db.TeamStaffs.Add(new TeamStaff { TeamId = equipe.Id, LeagueStaffTypeId = type.Id, Quantite = qte });
        depense += qte * unitaire;

        if (nomStaff == StaffService.NomFans) equipe.FansDevoues = qte;
        if (nomStaff == StaffService.NomRelances) equipe.NombreRelances = qte;
        if (nomStaff == StaffService.NomApothicaire) equipe.Apothicaire = qte > 0;
    }

    equipe.Tresorerie = ligue.BudgetDepart - depense;
    await db.SaveChangesAsync();
    Console.WriteLine($"    · {nom} ({race}) — {equipe.Tresorerie:N0} po");
    return equipe;
}

// ── Round Robin + playoffs, prête à lancer (4 équipes) ────────────────────────
var rr = await LigueAsync("QA RoundRobin", LeagueFormat.RoundRobinAvecPlayoffs, LeagueStatus.Inscription);
await EquipeAsync(rr, coaches[0], "RR Nains", "Nains", null);
await EquipeAsync(rr, coaches[1], "RR Elfes", "Elfes Sylvains", null);
await EquipeAsync(rr, coaches[2], "RR Orques", "Orques", null);
await EquipeAsync(rr, coaches[3], "RR Humains", "Humains", null);

// ── Libre, prête à lancer (4 équipes) ─────────────────────────────────────────
var libre = await LigueAsync("QA Libre", LeagueFormat.Libre, LeagueStatus.Inscription);
await EquipeAsync(libre, coaches[0], "LB Nains", "Nains", null);
await EquipeAsync(libre, coaches[1], "LB Elfes", "Elfes Sylvains", null);
await EquipeAsync(libre, coaches[4], "LB Amazones", "Amazones", null);
await EquipeAsync(libre, coaches[5], "LB Bretonniens", "Bretonniens", null);

// ── Mode brouillard, pour vérifier le masquage du calendrier ──────────────────
var brouillard = await LigueAsync("QA Brouillard", LeagueFormat.RoundRobin, LeagueStatus.Inscription, brouillard: true);
await EquipeAsync(brouillard, coaches[2], "BR Orques", "Orques", null);
await EquipeAsync(brouillard, coaches[3], "BR Humains", "Humains", null);

Console.WriteLine();
Console.WriteLine("Jeu QA prêt.");
