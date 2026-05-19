using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// CRUD validé pour les données de jeu (TeamType, PlayerPosition, Skill, RulesVersion, KeywordLimit).
/// Réservé Admin/GrandCommissaire (auth gate au layer UI).
/// </summary>
public class DataEditService(ApplicationDbContext db, ILogger<DataEditService> logger)
{
    // ═══════════════════ RulesVersion ═══════════════════

    public async Task<List<RulesVersion>> GetVersionsAsync(int gameId) =>
        await db.RulesVersions
            .Where(v => v.GameId == gameId)
            .OrderBy(v => v.Ordre)
            .ToListAsync();

    public async Task<RulesVersion> CreerVersionAsync(int gameId, string nom, int ordre, bool estActive, int? cloneFromVersionId)
    {
        // Si estActive, désactiver les autres versions actives du même jeu
        if (estActive)
        {
            var actives = await db.RulesVersions.Where(v => v.GameId == gameId && v.EstActive).ToListAsync();
            foreach (var a in actives) a.EstActive = false;
        }

        var nouvelle = new RulesVersion { GameId = gameId, Nom = nom, Ordre = ordre, EstActive = estActive };
        db.RulesVersions.Add(nouvelle);
        await db.SaveChangesAsync();

        if (cloneFromVersionId is int srcId)
            await ClonerVersionAsync(srcId, nouvelle.Id);

        logger.LogInformation("Version créée : {Nom} (id={Id}) sur Game={GameId} (cloneFrom={Clone})", nom, nouvelle.Id, gameId, cloneFromVersionId);
        return nouvelle;
    }

    private async Task ClonerVersionAsync(int sourceVersionId, int destVersionId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Cloner les Skills + map oldId → newSkill
        var sourceSkills = await db.Skills.Where(s => s.RulesVersionId == sourceVersionId).ToListAsync();
        var skillMap = new Dictionary<int, Skill>();
        foreach (var src in sourceSkills)
        {
            var copie = new Skill
            {
                Nom = src.Nom,
                Categorie = src.Categorie,
                Description = src.Description,
                EstElite = src.EstElite,
                EstTrait = src.EstTrait,
                RulesVersionId = destVersionId
            };
            db.Skills.Add(copie);
            skillMap[src.Id] = copie;
        }
        await db.SaveChangesAsync();

        // 2. Cloner les TeamTypes + map
        var sourceTypes = await db.TeamTypes
            .Include(t => t.Postes).ThenInclude(p => p.CompetencesDepart)
            .Include(t => t.LimitesMotsCles)
            .Where(t => t.RulesVersionId == sourceVersionId)
            .ToListAsync();

        var teamTypeMap = new Dictionary<int, TeamType>();
        foreach (var src in sourceTypes)
        {
            var copie = new TeamType
            {
                GameId = src.GameId,
                RulesVersionId = destVersionId,
                Nom = src.Nom,
                CoutRelance = src.CoutRelance,
                Categorie = src.Categorie,
                ReglesSpeciales = src.ReglesSpeciales,
                ReglesSpecialesLigue = src.ReglesSpecialesLigue
            };
            db.TeamTypes.Add(copie);
            teamTypeMap[src.Id] = copie;
        }
        await db.SaveChangesAsync();

        // 3. Cloner les PlayerPositions + leurs CompetencesDepart (avec mapping skill)
        foreach (var src in sourceTypes)
        {
            var destType = teamTypeMap[src.Id];
            foreach (var pos in src.Postes)
            {
                var copie = new PlayerPosition
                {
                    TeamTypeId = destType.Id,
                    Nom = pos.Nom,
                    QuantiteMax = pos.QuantiteMax,
                    Cout = pos.Cout,
                    Mouvement = pos.Mouvement,
                    Force = pos.Force,
                    Agilite = pos.Agilite,
                    CapacitePasse = pos.CapacitePasse,
                    Armure = pos.Armure,
                    CompetencesPrincipales = pos.CompetencesPrincipales,
                    CompetencesSecondaires = pos.CompetencesSecondaires,
                    MotsCles = pos.MotsCles
                };
                db.PlayerPositions.Add(copie);
                await db.SaveChangesAsync();

                foreach (var pps in pos.CompetencesDepart)
                {
                    if (skillMap.TryGetValue(pps.SkillId, out var newSkill))
                    {
                        db.PlayerPositionSkills.Add(new PlayerPositionSkill
                        {
                            PlayerPositionId = copie.Id,
                            SkillId = newSkill.Id
                        });
                    }
                }
            }

            // Limites mot-clé
            foreach (var lim in src.LimitesMotsCles)
            {
                db.TeamTypeKeywordLimits.Add(new TeamTypeKeywordLimit
                {
                    TeamTypeId = destType.Id,
                    MotCle = lim.MotCle,
                    Max = lim.Max
                });
            }
        }
        await db.SaveChangesAsync();

        await tx.CommitAsync();
        logger.LogInformation("Clonage : v{Src} → v{Dest} ({NbSkills} skills, {NbTypes} types)", sourceVersionId, destVersionId, sourceSkills.Count, sourceTypes.Count);
    }

    // ═══════════════════ TeamType ═══════════════════

    public async Task<List<TeamType>> GetTeamTypesAsync(int versionId) =>
        await db.TeamTypes
            .Include(t => t.Postes)
            .Where(t => t.RulesVersionId == versionId)
            .OrderBy(t => t.Nom)
            .ToListAsync();

    public async Task<TeamType?> GetTeamTypeAsync(int id) =>
        await db.TeamTypes
            .Include(t => t.Postes).ThenInclude(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Include(t => t.LimitesMotsCles)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TeamType> CreerTeamTypeAsync(int versionId, TeamType data)
    {
        data.RulesVersionId = versionId;
        var gameId = await db.RulesVersions.Where(v => v.Id == versionId).Select(v => v.GameId).FirstAsync();
        data.GameId = gameId;
        db.TeamTypes.Add(data);
        await db.SaveChangesAsync();
        logger.LogInformation("TeamType créé : {Nom} (id={Id}) sur version {VersionId}", data.Nom, data.Id, versionId);
        return data;
    }

    public async Task ModifierTeamTypeAsync(int id, string nom, TeamCategory categorie, int coutRelance, string reglesSpeciales, string reglesSpecialesLigue)
    {
        var t = await db.TeamTypes.FindAsync(id) ?? throw new InvalidOperationException("TeamType introuvable");
        t.Nom = nom;
        t.Categorie = categorie;
        t.CoutRelance = coutRelance;
        t.ReglesSpeciales = reglesSpeciales;
        t.ReglesSpecialesLigue = reglesSpecialesLigue;
        await db.SaveChangesAsync();
    }

    public async Task SupprimerTeamTypeAsync(int id)
    {
        var nbEquipes = await db.Teams.CountAsync(e => e.TeamTypeId == id);
        if (nbEquipes > 0)
            throw new InvalidOperationException($"{nbEquipes} équipe(s) utilisent ce type. Supprimer les équipes d'abord.");

        var t = await db.TeamTypes.FindAsync(id) ?? throw new InvalidOperationException("TeamType introuvable");
        db.TeamTypes.Remove(t);
        await db.SaveChangesAsync();
        logger.LogInformation("TeamType supprimé : {Nom} (id={Id})", t.Nom, id);
    }

    // ═══════════════════ PlayerPosition ═══════════════════

    public async Task<PlayerPosition> AjouterPosteAsync(int teamTypeId, PlayerPosition data, IEnumerable<int> skillsDepart)
    {
        data.TeamTypeId = teamTypeId;
        db.PlayerPositions.Add(data);
        await db.SaveChangesAsync();
        foreach (var sid in skillsDepart)
            db.PlayerPositionSkills.Add(new PlayerPositionSkill { PlayerPositionId = data.Id, SkillId = sid });
        await db.SaveChangesAsync();
        return data;
    }

    public async Task ModifierPosteAsync(int id, PlayerPosition data, IEnumerable<int> skillsDepart)
    {
        var p = await db.PlayerPositions
            .Include(x => x.CompetencesDepart)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Poste introuvable");

        p.Nom = data.Nom;
        p.QuantiteMax = data.QuantiteMax;
        p.Cout = data.Cout;
        p.Mouvement = data.Mouvement;
        p.Force = data.Force;
        p.Agilite = data.Agilite;
        p.CapacitePasse = data.CapacitePasse;
        p.Armure = data.Armure;
        p.CompetencesPrincipales = data.CompetencesPrincipales;
        p.CompetencesSecondaires = data.CompetencesSecondaires;
        p.MotsCles = data.MotsCles;

        // Resync skills de départ
        db.PlayerPositionSkills.RemoveRange(p.CompetencesDepart);
        await db.SaveChangesAsync();
        foreach (var sid in skillsDepart)
            db.PlayerPositionSkills.Add(new PlayerPositionSkill { PlayerPositionId = p.Id, SkillId = sid });
        await db.SaveChangesAsync();
    }

    public async Task SupprimerPosteAsync(int id)
    {
        var nbJoueurs = await db.TeamPlayers.CountAsync(j => j.PlayerPositionId == id);
        if (nbJoueurs > 0)
            throw new InvalidOperationException($"{nbJoueurs} joueur(s) utilisent ce poste.");

        var p = await db.PlayerPositions.FindAsync(id) ?? throw new InvalidOperationException("Poste introuvable");
        db.PlayerPositions.Remove(p);
        await db.SaveChangesAsync();
    }

    // ═══════════════════ Skill ═══════════════════

    public async Task<List<Skill>> GetSkillsAsync(int versionId) =>
        await db.Skills
            .Where(s => s.RulesVersionId == versionId)
            .OrderBy(s => s.Categorie).ThenBy(s => s.Nom)
            .ToListAsync();

    public async Task<Skill> CreerSkillAsync(int versionId, Skill data)
    {
        data.RulesVersionId = versionId;
        db.Skills.Add(data);
        await db.SaveChangesAsync();
        return data;
    }

    public async Task ModifierSkillAsync(int id, string nom, SkillCategory categorie, string description, bool estElite, bool estTrait)
    {
        var s = await db.Skills.FindAsync(id) ?? throw new InvalidOperationException("Skill introuvable");
        s.Nom = nom;
        s.Categorie = categorie;
        s.Description = description;
        s.EstElite = estElite;
        s.EstTrait = estTrait;
        await db.SaveChangesAsync();
    }

    public async Task SupprimerSkillAsync(int id)
    {
        var nbJoueurs = await db.TeamPlayerSkills.CountAsync(t => t.SkillId == id);
        if (nbJoueurs > 0)
            throw new InvalidOperationException($"{nbJoueurs} joueur(s) ont cette compétence.");
        var nbImp = await db.PlayerImprovements.CountAsync(p => p.SkillId == id);
        if (nbImp > 0)
            throw new InvalidOperationException($"{nbImp} amélioration(s) référencent cette compétence.");
        var nbPostes = await db.PlayerPositionSkills.CountAsync(p => p.SkillId == id);
        if (nbPostes > 0)
            throw new InvalidOperationException($"{nbPostes} poste(s) ont cette compétence de départ.");

        var s = await db.Skills.FindAsync(id) ?? throw new InvalidOperationException("Skill introuvable");
        db.Skills.Remove(s);
        await db.SaveChangesAsync();
    }

    // ═══════════════════ KeywordLimit ═══════════════════

    public async Task<TeamTypeKeywordLimit> AjouterLimiteAsync(int teamTypeId, string motCle, int max)
    {
        var l = new TeamTypeKeywordLimit { TeamTypeId = teamTypeId, MotCle = motCle, Max = max };
        db.TeamTypeKeywordLimits.Add(l);
        await db.SaveChangesAsync();
        return l;
    }

    public async Task SupprimerLimiteAsync(int id)
    {
        var l = await db.TeamTypeKeywordLimits.FindAsync(id) ?? throw new InvalidOperationException("Limite introuvable");
        db.TeamTypeKeywordLimits.Remove(l);
        await db.SaveChangesAsync();
    }
}
