using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class LeagueService(
    ApplicationDbContext db,
    ILogger<LeagueService> logger,
    IAuthorizationService authService)
{
    public async Task<List<League>> GetAllLiguesAsync() =>
        await db.Leagues
            .Include(l => l.Game)
            .Include(l => l.RulesVersion)
            .Include(l => l.Commissaire)
            .Include(l => l.Equipes)
            .OrderByDescending(l => l.CreeLe)
            .ToListAsync();

    public async Task<League?> GetLigueAsync(int id) =>
        await db.Leagues
            .Include(l => l.Game)
            .Include(l => l.RulesVersion)
            .Include(l => l.Commissaire)
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes).ThenInclude(e => e.Coach)
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes).ThenInclude(e => e.TeamType)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs)
                .ThenInclude(m => m.EquipeDomicile)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs)
                .ThenInclude(m => m.EquipeExterieur)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<League> CreerLigueAsync(League ligue, string commissaireId)
    {
        ligue.CommissaireId = commissaireId;
        ligue.Statut = LeagueStatus.Creation;
        ligue.CreeLe = DateTime.UtcNow;

        db.Leagues.Add(ligue);
        await db.SaveChangesAsync();
        logger.LogInformation("Ligue créée : {NomLigue} (id={Id}) par commissaire {CommissaireId}", ligue.Nom, ligue.Id, commissaireId);
        return ligue;
    }

    public async Task DemarrerInscriptionsAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        ligue.Statut = LeagueStatus.Inscription;
        await db.SaveChangesAsync();
    }

    public async Task LancerSaisonAsync(int ligueId)
    {
        var ligue = await db.Leagues
            .Include(l => l.Equipes)
            .Include(l => l.Divisions)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Equipes.Count < 2)
            throw new InvalidOperationException("Il faut au moins 2 équipes pour lancer la saison.");

        // Créer une division par défaut si aucune
        if (!ligue.Divisions.Any())
        {
            var div = new Division { LeagueId = ligueId, Nom = "Division Unique", Ordre = 1 };
            db.Divisions.Add(div);
            await db.SaveChangesAsync();

            foreach (var equipe in ligue.Equipes)
                equipe.DivisionId = div.Id;

            await db.SaveChangesAsync();
        }

        await GenererPoolMatchsAsync(ligue);
        ligue.Statut = LeagueStatus.EnCours;
        await db.SaveChangesAsync();
        logger.LogInformation("Saison lancée pour la ligue {NomLigue} (id={Id}) avec {NbEquipes} équipes", ligue.Nom, ligue.Id, ligue.Equipes.Count);
    }

    private async Task GenererPoolMatchsAsync(League ligue)
    {
        var divisions = await db.Divisions
            .Include(d => d.Equipes)
            .Where(d => d.LeagueId == ligue.Id)
            .ToListAsync();

        foreach (var division in divisions)
        {
            var equipes = division.Equipes.ToList();
            var matchs = GenererRoundRobin(equipes, division.Id);
            db.Matches.AddRange(matchs);
        }
        await db.SaveChangesAsync();
    }

    private static List<Match> GenererRoundRobin(List<Team> equipes, int divisionId)
    {
        // Copie locale ; si nombre impair, ajouter null comme équipe fantôme (bye)
        // Cela rend n pair et garantit que chaque équipe rencontre toutes les autres.
        var liste = new List<Team?>(equipes.Cast<Team?>());
        if (liste.Count % 2 != 0) liste.Add(null);
        int n = liste.Count;

        var matchs = new List<Match>();

        for (int ronde = 1; ronde < n; ronde++)
        {
            for (int i = 0; i < n / 2; i++)
            {
                var domicile = liste[i];
                var exterieur = liste[n - 1 - i];

                // Ignorer les matches impliquant l'équipe fantôme (= bye)
                if (domicile is null || exterieur is null) continue;

                matchs.Add(new Match
                {
                    DivisionId = divisionId,
                    Ronde = ronde,
                    EquipeDomicileId = domicile.Id,
                    EquipeExterieurId = exterieur.Id,
                    Statut = MatchStatus.Programme,
                    EstPlayoff = false
                });
            }

            // Rotation : fixer la première équipe, faire tourner les autres
            var derniere = liste[n - 1];
            liste.RemoveAt(n - 1);
            liste.Insert(1, derniere);
        }

        return matchs;
    }

    public async Task LancerPhaseDeReposAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Statut != LeagueStatus.EnCours)
            throw new InvalidOperationException("La phase de repos ne peut être lancée que depuis l'état EnCours.");

        var teamIds = await db.Teams.Where(t => t.LeagueId == ligueId).Select(t => t.Id).ToListAsync();
        var joueurs = await db.TeamPlayers
            .Where(j => teamIds.Contains(j.TeamId) && j.ManqueSuivantMatch)
            .ToListAsync();

        foreach (var j in joueurs)
            j.ManqueSuivantMatch = false;

        ligue.Statut = LeagueStatus.PhaseDeRepos;
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Phase de repos lancée pour la ligue {NomLigue} (id={Id}) : {NbResetRPM} RPM reset sur {NbEquipes} équipes",
            ligue.Nom, ligue.Id, joueurs.Count, teamIds.Count);
    }

    public async Task GenererPlayoffsAsync(int ligueId)
    {
        var ligue = await db.Leagues
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Statut != LeagueStatus.PhaseDeRepos && ligue.Statut != LeagueStatus.EnCours)
            throw new InvalidOperationException("Les playoffs ne peuvent être générés qu'après la saison régulière ou la phase de repos.");

        // Trier les équipes par points de ligue
        var equipesQualifiees = ligue.Divisions
            .SelectMany(d => d.Equipes)
            .OrderByDescending(e => e.PointsLigue)
            .ThenByDescending(e => e.TouchdownsMarques - e.TouchdownsConcedes)
            .ThenByDescending(e => e.EliminationsInfligees)
            .Take(ligue.NombreEquipesPlayoff)
            .ToList();

        if (equipesQualifiees.Count < 2)
            throw new InvalidOperationException("Pas assez d'équipes qualifiées pour les playoffs.");

        // Générer les quarts (ou demi si 4 équipes)
        var matchsPlayoff = new List<Match>();
        int ronde = 100; // Ronde 100+ = playoffs
        for (int i = 0; i < equipesQualifiees.Count / 2; i++)
        {
            matchsPlayoff.Add(new Match
            {
                DivisionId = ligue.Divisions.First().Id,
                Ronde = ronde,
                EquipeDomicileId = equipesQualifiees[i].Id,
                EquipeExterieurId = equipesQualifiees[equipesQualifiees.Count - 1 - i].Id,
                Statut = MatchStatus.Programme,
                EstPlayoff = true
            });
        }

        db.Matches.AddRange(matchsPlayoff);
        ligue.Statut = LeagueStatus.PlayOffs;
        await db.SaveChangesAsync();
        logger.LogInformation("Playoffs générés pour la ligue {NomLigue} (id={Id}) : {NbMatchs} matchs, {NbEquipes} équipes qualifiées", ligue.Nom, ligue.Id, matchsPlayoff.Count, equipesQualifiees.Count);
    }

    /// <summary>
    /// Validation post-match de repos par un coach : applique améliorations, recrutements et achat de relances
    /// sans qu'un Match ne soit nécessaire. Trace via PhaseDeReposValidation.
    /// </summary>
    public async Task ValiderApresMatchReposAsync(
        int ligueId,
        int teamId,
        List<(int joueurId, int skillId, bool estPrincipale, int xpDepensee)> competences,
        List<(int positionId, string nom, int numero)> nouveauxJoueurs,
        int nouvellesRelances,
        TeamService teamService)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        if (ligue.Statut != LeagueStatus.PhaseDeRepos)
            throw new InvalidOperationException("La validation de repos n'est possible que pendant la phase de repos.");

        var dejaValide = await db.PhaseDeReposValidations
            .AnyAsync(v => v.LeagueId == ligueId && v.TeamId == teamId);
        if (dejaValide)
            throw new InvalidOperationException("Cette équipe a déjà validé sa phase de repos.");

        foreach (var (joueurId, skillId, estPrincipale, xpDepensee) in competences)
        {
            var type = estPrincipale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire;
            await teamService.AppliquerAmeliorationAsync(joueurId, type, skillId: skillId,
                matchSheetId: null, xpDepensee: xpDepensee);
        }

        foreach (var (positionId, nom, numero) in nouveauxJoueurs)
            await teamService.RecruterJoueurAsync(teamId, positionId, nom, numero);

        if (nouvellesRelances > 0)
        {
            var equipe = await db.Teams.Include(t => t.TeamType).FirstAsync(t => t.Id == teamId);
            var coutRelance = (equipe.TeamType?.CoutRelance ?? 50_000) * 2;
            var total = nouvellesRelances * coutRelance;
            if (equipe.Tresorerie < total)
                throw new InvalidOperationException("Fonds insuffisants pour acheter les relances.");
            const int maxRelances = 8;
            if (equipe.NombreRelances + nouvellesRelances > maxRelances)
                throw new InvalidOperationException($"Maximum {maxRelances} relances par équipe.");
            equipe.Tresorerie -= total;
            equipe.NombreRelances += nouvellesRelances;
        }

        db.PhaseDeReposValidations.Add(new PhaseDeReposValidation
        {
            LeagueId = ligueId,
            TeamId = teamId
        });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Phase de repos validée pour équipe id={TeamId} dans ligue id={LigueId} : {NbComp} comp., {NbNouv} recrues, {NbRel} relances",
            teamId, ligueId, competences.Count, nouveauxJoueurs.Count, nouvellesRelances);
    }

    public async Task TerminerLigueAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        ligue.Statut = LeagueStatus.Termine;
        await db.SaveChangesAsync();
    }

    public async Task<List<Match>> GetMatchsDivisionAsync(int divisionId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.Feuille)
            .Where(m => m.DivisionId == divisionId)
            .OrderBy(m => m.Ronde)
            .ToListAsync();

    public async Task<List<Game>> GetGamesAsync() =>
        await db.Games.Include(g => g.Versions).ToListAsync();

    public async Task<List<RulesVersion>> GetVersionsAsync(int gameId) =>
        await db.RulesVersions
            .Where(v => v.GameId == gameId)
            .OrderBy(v => v.Ordre)
            .ToListAsync();

    public async Task<bool> EstCommissaireAsync(int ligueId, string userId)
        => await authService.PeutGererLigueAsync(userId, ligueId);

    public async Task<List<TeamPlayer>> GetTopJoueursParPspAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Where(j => j.Team.LeagueId == ligueId && !j.EstMort && !j.EstRetraite)
            .OrderByDescending(j => j.PointsStarPlayer)
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopMarqueursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.Touchdowns))
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopElimineursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.EliminationsInfligees))
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopPasseursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.Passes + r.Interceptions))
            .Take(limit)
            .ToListAsync();

    public record CoachClassement(ApplicationUser Coach, int PointsLigue, int Victoires, int Nuls, int Defaites);

    public async Task<List<CoachClassement>> GetTopCoachsAsync(int ligueId)
    {
        var equipes = await db.Teams
            .Include(t => t.Coach)
            .Where(t => t.LeagueId == ligueId)
            .ToListAsync();

        return [.. equipes
            .GroupBy(t => t.Coach)
            .Select(g => new CoachClassement(
                g.Key,
                g.Sum(t => t.PointsLigue),
                g.Sum(t => t.NombreVictoires),
                g.Sum(t => t.NombreNuls),
                g.Sum(t => t.NombreDefaites)))
            .OrderByDescending(c => c.PointsLigue)
            .ThenByDescending(c => c.Victoires)];
    }

    public async Task AttribuerAwardAsync(
        int ligueId, AwardType type,
        int? teamPlayerId = null, int? teamId = null, string? coachId = null)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        var award = new LeagueAward
        {
            LeagueId = ligueId,
            Type = type,
            TeamPlayerId = teamPlayerId,
            TeamId = teamId,
            CoachId = coachId
        };
        db.LeagueAwards.Add(award);
        await db.SaveChangesAsync();
        logger.LogInformation("Award {AwardType} attribué dans la ligue id={LigueId}", type, ligueId);
    }

    public async Task<List<LeagueAward>> GetAwardsAsync(int ligueId) =>
        await db.LeagueAwards
            .Include(a => a.TeamPlayer).ThenInclude(j => j!.Team)
            .Include(a => a.Team)
            .Include(a => a.Coach)
            .Where(a => a.LeagueId == ligueId)
            .OrderBy(a => a.Type)
            .ToListAsync();

    public async Task SupprimerLigueAsync(int ligueId)
    {
        var divIds    = await db.Divisions.Where(d => d.LeagueId == ligueId).Select(d => d.Id).ToListAsync();
        var matchIds  = await db.Matches.Where(m => m.DivisionId.HasValue && divIds.Contains(m.DivisionId.Value)).Select(m => m.Id).ToListAsync();
        var sheetIds  = await db.MatchSheets.Where(s => matchIds.Contains(s.MatchId)).Select(s => s.Id).ToListAsync();
        var teamIds   = await db.Teams.Where(t => t.LeagueId == ligueId).Select(t => t.Id).ToListAsync();
        var playerIds = await db.TeamPlayers.Where(j => teamIds.Contains(j.TeamId)).Select(j => j.Id).ToListAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.MatchPlayerRecords.Where(r => sheetIds.Contains(r.MatchSheetId)).ExecuteDeleteAsync();
        await db.MatchSheets.Where(s => matchIds.Contains(s.MatchId)).ExecuteDeleteAsync();
        await db.PlayerInjuries.Where(b => playerIds.Contains(b.TeamPlayerId)).ExecuteDeleteAsync();
        await db.TeamPlayerSkills.Where(s => playerIds.Contains(s.TeamPlayerId)).ExecuteDeleteAsync();
        await db.Matches.Where(m => matchIds.Contains(m.Id)).ExecuteDeleteAsync();
        await db.TeamPlayers.Where(j => playerIds.Contains(j.Id)).ExecuteDeleteAsync();
        await db.Teams.Where(t => teamIds.Contains(t.Id)).ExecuteDeleteAsync();
        await db.Divisions.Where(d => divIds.Contains(d.Id)).ExecuteDeleteAsync();
        await db.Leagues.Where(l => l.Id == ligueId).ExecuteDeleteAsync();
        await tx.CommitAsync();

        logger.LogInformation("Ligue id={Id} supprimée avec toutes ses données", ligueId);
    }

    public async Task<List<LeagueCommissioner>> GetCommissairesDeLigueAsync(int ligueId)
        => await db.LeagueCommissioners
            .Include(lc => lc.User)
            .Where(lc => lc.LeagueId == ligueId)
            .OrderBy(lc => lc.AssigneLe)
            .ToListAsync();

    public async Task PromouvoirCommissaireDeLigueAsync(int ligueId, string userId, string assignePar)
    {
        var existe = await db.LeagueCommissioners.AnyAsync(lc => lc.LeagueId == ligueId && lc.UserId == userId);
        if (existe) return;
        db.LeagueCommissioners.Add(new LeagueCommissioner
        {
            LeagueId = ligueId,
            UserId = userId,
            AssignePar = assignePar
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Coach id={UserId} promu commissaire de la ligue {LigueId} par {AssignePar}", userId, ligueId, assignePar);
    }

    public async Task RetirerCommissaireDeLigueAsync(int ligueId, string userId)
    {
        var entry = await db.LeagueCommissioners
            .FirstOrDefaultAsync(lc => lc.LeagueId == ligueId && lc.UserId == userId);
        if (entry is null) return;
        db.LeagueCommissioners.Remove(entry);
        await db.SaveChangesAsync();
        logger.LogInformation("Coach id={UserId} retiré comme commissaire de la ligue {LigueId}", userId, ligueId);
    }
}
