using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class LeagueExportService(ApplicationDbContext db, ILogger<LeagueExportService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── Export ───────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportAsync(int ligueId)
    {
        var ligue = await LoadFullAsync(ligueId);
        var dto = ToDto(ligue);
        logger.LogInformation("Export ligue id={Id} ({Nom})", ligue.Id, ligue.Nom);
        return JsonSerializer.SerializeToUtf8Bytes(dto, JsonOpts);
    }

    private async Task<League> LoadFullAsync(int ligueId) =>
        await db.Leagues
            .Include(l => l.Game)
            .Include(l => l.RulesVersion)
            .Include(l => l.Equipes).ThenInclude(e => e.Coach)
            .Include(l => l.Equipes).ThenInclude(e => e.TeamType)
            .Include(l => l.Equipes).ThenInclude(e => e.Joueurs).ThenInclude(j => j.PlayerPosition)
            .Include(l => l.Equipes).ThenInclude(e => e.Joueurs)
                .ThenInclude(j => j.Competences.Where(c => !c.EstCompetenceDepart)).ThenInclude(c => c.Skill)
            .Include(l => l.Equipes).ThenInclude(e => e.Joueurs).ThenInclude(j => j.Blessures)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs).ThenInclude(m => m.EquipeDomicile)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs).ThenInclude(m => m.EquipeExterieur)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs)
                .ThenInclude(m => m.Feuille).ThenInclude(f => f!.RecordsJoueurs)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
        ?? throw new InvalidOperationException($"Ligue {ligueId} introuvable");

    private static LeagueExportDto ToDto(League ligue)
    {
        var playerTeam = ligue.Equipes
            .SelectMany(e => e.Joueurs.Select(j => (j.Id, e.Nom)))
            .ToDictionary(x => x.Id, x => x.Nom);

        var equipes = ligue.Equipes.Select(e => new EquipeExportDto(
            Nom: e.Nom,
            TypeEquipeNom: e.TeamType?.Nom ?? "",
            CoachEmail: e.Coach?.Email ?? "",
            Tresorerie: e.Tresorerie,
            NombreRelances: e.NombreRelances,
            FansDevoues: e.FansDevoues,
            NombreCoachsAssistants: e.NombreCoachsAssistants,
            NombreCheerleaders: e.NombreCheerleaders,
            Apothicaire: e.Apothicaire,
            NombreMatchsJoues: e.NombreMatchsJoues,
            NombreVictoires: e.NombreVictoires,
            NombreNuls: e.NombreNuls,
            NombreDefaites: e.NombreDefaites,
            TouchdownsMarques: e.TouchdownsMarques,
            TouchdownsConcedes: e.TouchdownsConcedes,
            EliminationsInfligees: e.EliminationsInfligees,
            PointsLigue: e.PointsLigue,
            Joueurs: e.Joueurs.Select(j => new JoueurExportDto(
                Nom: j.Nom,
                Numero: j.Numero,
                PositionNom: j.PlayerPosition?.Nom ?? "",
                ValeurActuelle: j.ValeurActuelle,
                PointsStarPlayer: j.PointsStarPlayer,
                NombreAmeliorations: j.Improvements.Count,
                ModMouvement: j.ModMouvement,
                ModForce: j.ModForce,
                ModAgilite: j.ModAgilite,
                ModCapacitePasse: j.ModCapacitePasse,
                ModArmure: j.ModArmure,
                EstMort: j.EstMort,
                EstRetraite: j.EstRetraite,
                ManqueSuivantMatch: j.ManqueSuivantMatch,
                CompetencesAcquises: j.Competences
                    .Where(c => !c.EstCompetenceDepart)
                    .Select(c => c.Skill?.Nom ?? "").Where(n => n != "").ToList(),
                Blessures: j.Blessures
                    .Select(b => new BlessureExportDto(b.Type, b.StatAffectee, b.Description)).ToList()
            )).ToList()
        )).ToList();

        var matchs = ligue.Divisions
            .SelectMany(d => d.Matchs)
            .OrderBy(m => m.Ronde)
            .Select(m => new MatchExportDto(
                Ronde: m.Ronde,
                EstPlayoff: m.EstPlayoff,
                EquipeDomicileNom: m.EquipeDomicile?.Nom ?? "",
                EquipeExterieurNom: m.EquipeExterieur?.Nom ?? "",
                Statut: m.Statut,
                ScoreDomicile: m.ScoreDomicile,
                ScoreExterieur: m.ScoreExterieur,
                DateJouee: m.DateJouee,
                Feuille: m.Feuille is null ? null : new MatchSheetExportDto(
                    TouchdownsDomicile: m.Feuille.TouchdownsDomicile,
                    TouchdownsExterieur: m.Feuille.TouchdownsExterieur,
                    EliminationsDomicile: m.Feuille.EliminationsDomicile,
                    EliminationsExterieur: m.Feuille.EliminationsExterieur,
                    GainsDomicile: m.Feuille.GainsDomicile,
                    GainsExterieur: m.Feuille.GainsExterieur,
                    VariationFansDomicile: m.Feuille.VariationFansDomicile,
                    VariationFansExterieur: m.Feuille.VariationFansExterieur,
                    ValideParCommissaire: m.Feuille.ValideParCommissaire,
                    NotesCommissaire: m.Feuille.NotesCommissaire,
                    Records: m.Feuille.RecordsJoueurs.Select(r => new RecordJoueurExportDto(
                        JoueurNom: r.TeamPlayer?.Nom ?? $"Joueur#{r.TeamPlayerId}",
                        EquipeNom: playerTeam.GetValueOrDefault(r.TeamPlayerId, ""),
                        Touchdowns: r.Touchdowns,
                        Passes: r.Passes,
                        Interceptions: r.Interceptions,
                        EliminationsInfligees: r.EliminationsInfligees,
                        EstMVP: r.EstMVP,
                        PspGagnes: r.PspGagnes,
                        Blessure: r.Blessure,
                        StatAffectee: r.StatAffectee
                    )).ToList()
                )
            )).ToList();

        return new LeagueExportDto(
            Version: "1.0",
            ExporteeLe: DateTime.UtcNow,
            NomLigue: ligue.Nom,
            Description: ligue.Description,
            GameNom: ligue.Game?.Nom ?? "",
            RulesVersionNom: ligue.RulesVersion?.Nom,
            Format: ligue.Format,
            BudgetDepart: ligue.BudgetDepart,
            NombreEquipesPlayoff: ligue.NombreEquipesPlayoff,
            Equipes: equipes,
            Matchs: matchs
        );
    }

    // ── Import ───────────────────────────────────────────────────────────────

    private static void ValiderImport(LeagueExportDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomLigue) || dto.NomLigue.Length > 200)
            throw new InvalidOperationException("Nom de ligue invalide (vide ou > 200 caractères).");
        if (string.IsNullOrWhiteSpace(dto.GameNom))
            throw new InvalidOperationException("Nom de jeu manquant.");
        if (dto.BudgetDepart < 0 || dto.BudgetDepart > 10_000_000)
            throw new InvalidOperationException($"Budget de départ invalide : {dto.BudgetDepart}.");
        if (dto.NombreEquipesPlayoff < 0 || dto.NombreEquipesPlayoff > 64)
            throw new InvalidOperationException($"Nombre d'équipes playoff invalide : {dto.NombreEquipesPlayoff}.");
        if (dto.Equipes.Count > 64)
            throw new InvalidOperationException($"Trop d'équipes ({dto.Equipes.Count}, max 64).");
        if (dto.Matchs.Count > 2000)
            throw new InvalidOperationException($"Trop de matchs ({dto.Matchs.Count}, max 2000).");

        foreach (var e in dto.Equipes)
        {
            if (string.IsNullOrWhiteSpace(e.Nom) || e.Nom.Length > 200)
                throw new InvalidOperationException($"Nom d'équipe invalide : '{e.Nom}'.");
            if (e.Tresorerie < 0 || e.Tresorerie > 10_000_000)
                throw new InvalidOperationException($"Trésorerie invalide pour '{e.Nom}' : {e.Tresorerie}.");
            if (e.NombreRelances < 0 || e.NombreRelances > 20)
                throw new InvalidOperationException($"Nombre de relances invalide pour '{e.Nom}' : {e.NombreRelances}.");
            if (e.NombreMatchsJoues < 0 || e.NombreVictoires < 0 || e.NombreNuls < 0 || e.NombreDefaites < 0)
                throw new InvalidOperationException($"Stats de match négatives pour '{e.Nom}'.");
            if (e.TouchdownsMarques < 0 || e.TouchdownsConcedes < 0 || e.EliminationsInfligees < 0)
                throw new InvalidOperationException($"Stats de TDs/élims négatives pour '{e.Nom}'.");
            if (e.PointsLigue < 0)
                throw new InvalidOperationException($"Points de ligue négatifs pour '{e.Nom}'.");
            if (e.Joueurs.Count > 32)
                throw new InvalidOperationException($"Trop de joueurs pour '{e.Nom}' ({e.Joueurs.Count}, max 32).");

            foreach (var j in e.Joueurs)
            {
                if (string.IsNullOrWhiteSpace(j.Nom) || j.Nom.Length > 100)
                    throw new InvalidOperationException($"Nom de joueur invalide : '{j.Nom}'.");
                if (j.Numero < 1 || j.Numero > 99)
                    throw new InvalidOperationException($"Numéro invalide pour '{j.Nom}' : {j.Numero}.");
                if (j.ValeurActuelle < 0 || j.ValeurActuelle > 500_000)
                    throw new InvalidOperationException($"Valeur invalide pour '{j.Nom}' : {j.ValeurActuelle}.");
                if (j.PointsStarPlayer < 0 || j.PointsStarPlayer > 100)
                    throw new InvalidOperationException($"PSP invalides pour '{j.Nom}' : {j.PointsStarPlayer}.");
                if (j.ModMouvement < -5 || j.ModMouvement > 0 || j.ModForce < -5 || j.ModForce > 0
                    || j.ModAgilite < -5 || j.ModAgilite > 0 || j.ModCapacitePasse < -5 || j.ModCapacitePasse > 0
                    || j.ModArmure < -5 || j.ModArmure > 0)
                    throw new InvalidOperationException($"Modificateurs de stats invalides pour '{j.Nom}'.");
            }
        }

        foreach (var m in dto.Matchs)
        {
            if (m.Ronde < 1 || m.Ronde > 999)
                throw new InvalidOperationException($"Ronde invalide : {m.Ronde}.");
            if ((m.ScoreDomicile ?? 0) < 0 || (m.ScoreExterieur ?? 0) < 0)
                throw new InvalidOperationException("Score négatif dans les matchs.");
            if (m.Feuille is not null)
            {
                var f = m.Feuille;
                if (f.TouchdownsDomicile < 0 || f.TouchdownsExterieur < 0
                    || f.EliminationsDomicile < 0 || f.EliminationsExterieur < 0)
                    throw new InvalidOperationException("Valeurs négatives dans une feuille de match.");
                if (f.GainsDomicile < 0 || f.GainsExterieur < 0)
                    throw new InvalidOperationException("Gains négatifs dans une feuille de match.");
                if (f.Records.Count > 64)
                    throw new InvalidOperationException($"Trop de records joueurs dans un match ({f.Records.Count}).");
            }
        }
    }

    public async Task<League> ImportAsync(Stream jsonStream, string commissaireId)
    {
        var dto = await JsonSerializer.DeserializeAsync<LeagueExportDto>(jsonStream, JsonOpts)
            ?? throw new InvalidOperationException("JSON invalide ou vide");

        var game = await db.Games.FirstOrDefaultAsync(g => g.Nom == dto.GameNom)
            ?? throw new InvalidOperationException($"Jeu '{dto.GameNom}' introuvable");

        var rulesVersion = await db.RulesVersions
            .FirstOrDefaultAsync(v => v.GameId == game.Id
                && (dto.RulesVersionNom == null || v.Nom == dto.RulesVersionNom))
            ?? throw new InvalidOperationException("Version de règles introuvable");

        var ligue = new League
        {
            Nom = $"{dto.NomLigue} (importée {DateTime.Now:dd/MM/yyyy})",
            Description = dto.Description,
            CommissaireId = commissaireId,
            GameId = game.Id,
            RulesVersionId = rulesVersion.Id,
            Format = dto.Format,
            BudgetDepart = dto.BudgetDepart,
            NombreEquipesPlayoff = dto.NombreEquipesPlayoff,
            Statut = LeagueStatus.Termine,
            CreeLe = DateTime.UtcNow
        };
        db.Leagues.Add(ligue);
        await db.SaveChangesAsync();

        var division = new Division { LeagueId = ligue.Id, Nom = "Division Unique", Ordre = 1 };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        // (nomEquipe, nomJoueur) → nouveau playerId
        var playerMap = new Dictionary<(string, string), int>();

        foreach (var equipeDto in dto.Equipes)
        {
            var teamType = await db.TeamTypes
                .FirstOrDefaultAsync(t => t.Nom == equipeDto.TypeEquipeNom && t.RulesVersionId == rulesVersion.Id)
                ?? await db.TeamTypes.FirstOrDefaultAsync(t => t.RulesVersionId == rulesVersion.Id)
                ?? throw new InvalidOperationException($"Aucun type d'équipe pour la version de règles '{rulesVersion.Nom}'");

            var coachId = commissaireId;
            if (!string.IsNullOrEmpty(equipeDto.CoachEmail))
            {
                var coach = await db.Users.FirstOrDefaultAsync(u => u.Email == equipeDto.CoachEmail);
                if (coach != null) coachId = coach.Id;
            }

            var team = new Team
            {
                Nom = equipeDto.Nom,
                CoachId = coachId,
                LeagueId = ligue.Id,
                DivisionId = division.Id,
                TeamTypeId = teamType.Id,
                Tresorerie = equipeDto.Tresorerie,
                NombreRelances = equipeDto.NombreRelances,
                FansDevoues = equipeDto.FansDevoues,
                NombreCoachsAssistants = equipeDto.NombreCoachsAssistants,
                NombreCheerleaders = equipeDto.NombreCheerleaders,
                Apothicaire = equipeDto.Apothicaire,
                NombreMatchsJoues = equipeDto.NombreMatchsJoues,
                NombreVictoires = equipeDto.NombreVictoires,
                NombreNuls = equipeDto.NombreNuls,
                NombreDefaites = equipeDto.NombreDefaites,
                TouchdownsMarques = equipeDto.TouchdownsMarques,
                TouchdownsConcedes = equipeDto.TouchdownsConcedes,
                EliminationsInfligees = equipeDto.EliminationsInfligees,
                PointsLigue = equipeDto.PointsLigue,
                CreeLe = DateTime.UtcNow
            };
            db.Teams.Add(team);
            await db.SaveChangesAsync();

            foreach (var joueurDto in equipeDto.Joueurs)
            {
                var position = await db.PlayerPositions
                    .Include(p => p.CompetencesDepart)
                    .FirstOrDefaultAsync(p => p.TeamTypeId == teamType.Id && p.Nom == joueurDto.PositionNom)
                    ?? await db.PlayerPositions
                        .Include(p => p.CompetencesDepart)
                        .FirstOrDefaultAsync(p => p.TeamTypeId == teamType.Id);

                if (position is null)
                {
                    logger.LogWarning("Aucun poste pour {Equipe}/{Joueur}, joueur ignoré", equipeDto.Nom, joueurDto.Nom);
                    continue;
                }

                var joueur = new TeamPlayer
                {
                    TeamId = team.Id,
                    PlayerPositionId = position.Id,
                    Nom = joueurDto.Nom,
                    Numero = joueurDto.Numero,
                    ValeurActuelle = joueurDto.ValeurActuelle,
                    PointsStarPlayer = joueurDto.PointsStarPlayer,
                    // Données legacy d'import : NombreAmeliorations n'est plus appliqué ; les Improvements sont reconstruits lors des après-matchs.
                    ModMouvement = joueurDto.ModMouvement,
                    ModForce = joueurDto.ModForce,
                    ModAgilite = joueurDto.ModAgilite,
                    ModCapacitePasse = joueurDto.ModCapacitePasse,
                    ModArmure = joueurDto.ModArmure,
                    EstMort = joueurDto.EstMort,
                    EstRetraite = joueurDto.EstRetraite,
                    ManqueSuivantMatch = joueurDto.ManqueSuivantMatch,
                    RecruteLe = DateTime.UtcNow
                };
                db.TeamPlayers.Add(joueur);
                await db.SaveChangesAsync();

                playerMap[(equipeDto.Nom, joueurDto.Nom)] = joueur.Id;

                foreach (var comp in position.CompetencesDepart)
                    db.TeamPlayerSkills.Add(new TeamPlayerSkill { TeamPlayerId = joueur.Id, SkillId = comp.SkillId, EstCompetenceDepart = true });

                foreach (var skillNom in joueurDto.CompetencesAcquises)
                {
                    var skill = await db.Skills.FirstOrDefaultAsync(s => s.Nom == skillNom && s.RulesVersionId == rulesVersion.Id);
                    if (skill != null)
                        db.TeamPlayerSkills.Add(new TeamPlayerSkill { TeamPlayerId = joueur.Id, SkillId = skill.Id, EstCompetenceDepart = false });
                }

                foreach (var b in joueurDto.Blessures)
                    db.PlayerInjuries.Add(new PlayerInjury { TeamPlayerId = joueur.Id, Type = b.Type, StatAffectee = b.StatAffectee, Description = b.Description, Date = DateTime.UtcNow });

                await db.SaveChangesAsync();
            }
        }

        var teamIdMap = await db.Teams.Where(t => t.LeagueId == ligue.Id).ToDictionaryAsync(t => t.Nom, t => t.Id);

        foreach (var matchDto in dto.Matchs)
        {
            if (!teamIdMap.TryGetValue(matchDto.EquipeDomicileNom, out var domId) ||
                !teamIdMap.TryGetValue(matchDto.EquipeExterieurNom, out var extId))
            {
                logger.LogWarning("Match {Dom} vs {Ext} ignoré (équipe manquante)", matchDto.EquipeDomicileNom, matchDto.EquipeExterieurNom);
                continue;
            }

            var match = new Match
            {
                DivisionId = division.Id,
                Ronde = matchDto.Ronde,
                EstPlayoff = matchDto.EstPlayoff,
                EquipeDomicileId = domId,
                EquipeExterieurId = extId,
                Statut = matchDto.Statut,
                ScoreDomicile = matchDto.ScoreDomicile,
                ScoreExterieur = matchDto.ScoreExterieur,
                DateJouee = matchDto.DateJouee
            };
            db.Matches.Add(match);
            await db.SaveChangesAsync();

            if (matchDto.Feuille is not null)
            {
                var feuille = new MatchSheet
                {
                    MatchId = match.Id,
                    SaisiParId = commissaireId,
                    SaisiLe = DateTime.UtcNow,
                    TouchdownsDomicile = matchDto.Feuille.TouchdownsDomicile,
                    TouchdownsExterieur = matchDto.Feuille.TouchdownsExterieur,
                    EliminationsDomicile = matchDto.Feuille.EliminationsDomicile,
                    EliminationsExterieur = matchDto.Feuille.EliminationsExterieur,
                    GainsDomicile = matchDto.Feuille.GainsDomicile,
                    GainsExterieur = matchDto.Feuille.GainsExterieur,
                    VariationFansDomicile = matchDto.Feuille.VariationFansDomicile,
                    VariationFansExterieur = matchDto.Feuille.VariationFansExterieur,
                    ValideParCommissaire = matchDto.Feuille.ValideParCommissaire,
                    NotesCommissaire = matchDto.Feuille.NotesCommissaire
                };
                db.MatchSheets.Add(feuille);
                await db.SaveChangesAsync();

                foreach (var r in matchDto.Feuille.Records)
                {
                    if (!playerMap.TryGetValue((r.EquipeNom, r.JoueurNom), out var pid))
                    {
                        logger.LogWarning("Record ignoré : joueur '{J}' équipe '{E}' introuvable", r.JoueurNom, r.EquipeNom);
                        continue;
                    }
                    db.MatchPlayerRecords.Add(new MatchPlayerRecord
                    {
                        MatchSheetId = feuille.Id,
                        TeamPlayerId = pid,
                        Touchdowns = r.Touchdowns,
                        Passes = r.Passes,
                        Interceptions = r.Interceptions,
                        EliminationsInfligees = r.EliminationsInfligees,
                        EstMVP = r.EstMVP,
                        PspGagnes = r.PspGagnes,
                        Blessure = r.Blessure,
                        StatAffectee = r.StatAffectee
                    });
                }
                await db.SaveChangesAsync();
            }
        }

        logger.LogInformation("Import ligue '{Nom}' (id={Id}) : {NbE} équipes, {NbM} matchs", ligue.Nom, ligue.Id, dto.Equipes.Count, dto.Matchs.Count);
        return ligue;
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

record LeagueExportDto(
    string Version,
    DateTime ExporteeLe,
    string NomLigue,
    string Description,
    string GameNom,
    string? RulesVersionNom,
    LeagueFormat Format,
    int BudgetDepart,
    int NombreEquipesPlayoff,
    List<EquipeExportDto> Equipes,
    List<MatchExportDto> Matchs
);

record EquipeExportDto(
    string Nom,
    string TypeEquipeNom,
    string? CoachEmail,
    int Tresorerie,
    int NombreRelances,
    int FansDevoues,
    int NombreCoachsAssistants,
    int NombreCheerleaders,
    bool Apothicaire,
    int NombreMatchsJoues,
    int NombreVictoires,
    int NombreNuls,
    int NombreDefaites,
    int TouchdownsMarques,
    int TouchdownsConcedes,
    int EliminationsInfligees,
    int PointsLigue,
    List<JoueurExportDto> Joueurs
);

record JoueurExportDto(
    string Nom,
    int Numero,
    string PositionNom,
    int ValeurActuelle,
    int PointsStarPlayer,
    int NombreAmeliorations,
    int ModMouvement,
    int ModForce,
    int ModAgilite,
    int ModCapacitePasse,
    int ModArmure,
    bool EstMort,
    bool EstRetraite,
    bool ManqueSuivantMatch,
    List<string> CompetencesAcquises,
    List<BlessureExportDto> Blessures
);

record BlessureExportDto(InjuryType Type, AffectedStat? StatAffectee, string Description);

record MatchExportDto(
    int Ronde,
    bool EstPlayoff,
    string EquipeDomicileNom,
    string EquipeExterieurNom,
    MatchStatus Statut,
    int? ScoreDomicile,
    int? ScoreExterieur,
    DateTime? DateJouee,
    MatchSheetExportDto? Feuille
);

record MatchSheetExportDto(
    int TouchdownsDomicile,
    int TouchdownsExterieur,
    int EliminationsDomicile,
    int EliminationsExterieur,
    int GainsDomicile,
    int GainsExterieur,
    int VariationFansDomicile,
    int VariationFansExterieur,
    bool ValideParCommissaire,
    string NotesCommissaire,
    List<RecordJoueurExportDto> Records
);

record RecordJoueurExportDto(
    string JoueurNom,
    string EquipeNom,
    int Touchdowns,
    int Passes,
    int Interceptions,
    int EliminationsInfligees,
    bool EstMVP,
    int PspGagnes,
    InjuryType? Blessure,
    AffectedStat? StatAffectee
);
