using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class GameDataExportService(ApplicationDbContext db, ILogger<GameDataExportService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── Export ───────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportAsync(int rulesVersionId)
    {
        var version = await db.RulesVersions
            .Include(v => v.Game)
            .FirstOrDefaultAsync(v => v.Id == rulesVersionId)
            ?? throw new InvalidOperationException("Version de règles introuvable");

        var skills = await db.Skills
            .Include(s => s.SkillCategoryDef)
            .Where(s => s.RulesVersionId == rulesVersionId)
            .OrderBy(s => s.SkillCategoryDef.Nom).ThenBy(s => s.Nom)
            .ToListAsync();

        var teamTypes = await db.TeamTypes
            .Include(tt => tt.Postes).ThenInclude(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Include(tt => tt.LimitesMotsCles)
            .Where(tt => tt.RulesVersionId == rulesVersionId)
            .OrderBy(tt => tt.Nom)
            .ToListAsync();

        var reserve = await db.PoolPositions
            .Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Where(p => p.RulesVersionId == rulesVersionId)
            .OrderBy(p => p.Nom)
            .ToListAsync();

        var categories = await db.SkillCategories
            .Where(c => c.RulesVersionId == rulesVersionId)
            .OrderBy(c => c.Nom)
            .ToListAsync();

        var dto = new GameDataExportDto(
            Jeu: version.Game.Nom,
            Version: version.Nom,
            Ordre: version.Ordre,
            EstActive: version.EstActive,
            Skills: skills.Select(s => new SkillGdDto(
                s.Nom, s.Categorie, s.Description, s.EstElite, s.EstTrait,
                CategorieNom: s.SkillCategoryDef?.Nom)).ToList(),
            TypesEquipes: teamTypes.Select(tt => new TeamTypeGdDto(
                tt.Nom,
                tt.Categorie,
                tt.CoutRelance,
                tt.ReglesSpeciales,
                tt.ReglesSpecialesLigue,
                tt.Postes.OrderBy(p => p.Cout).Select(p => new PlayerPositionGdDto(
                    p.Nom,
                    p.QuantiteMax,
                    p.Cout,
                    p.Mouvement,
                    p.Force,
                    p.Agilite,
                    p.CapacitePasse,
                    p.Armure,
                    p.CompetencesPrincipales,
                    p.CompetencesSecondaires,
                    p.MotsCles,
                    p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList()
                )).ToList(),
                tt.LimitesMotsCles.Select(l => new KeywordLimitGdDto(l.MotCle, l.Max)).ToList()
            )).ToList(),
            Reserve: reserve.Select(p => new PlayerPositionGdDto(
                p.Nom, p.QuantiteMax, p.Cout, p.Mouvement, p.Force, p.Agilite,
                p.CapacitePasse, p.Armure, p.CompetencesPrincipales, p.CompetencesSecondaires,
                p.MotsCles,
                p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList()
            )).ToList(),
            Categories: categories.Select(c => new SkillCategoryGdDto(c.Nom, c.Code)).ToList()
        );

        var json = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOpts);
        logger.LogInformation("Export game data : version '{V}' ({NbTT} types, {NbS} compétences)",
            version.Nom, teamTypes.Count, skills.Count);
        return json;
    }

    // ── Import ───────────────────────────────────────────────────────────────

    public async Task<(bool Success, List<string> Errors)> ImportAsync(
        Stream stream, int gameId, string versionNom)
    {
        var errors = new List<string>();

        GameDataExportDto dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<GameDataExportDto>(stream, JsonOpts)
                ?? throw new InvalidOperationException("Fichier JSON invalide");
        }
        catch (Exception ex)
        {
            return (false, [$"Impossible de lire le fichier : {ex.Message}"]);
        }

        var game = await db.Games.FindAsync(gameId);
        if (game is null)
        {
            errors.Add($"Jeu id={gameId} introuvable");
            return (false, errors);
        }

        var dejaExistante = await db.RulesVersions
            .AnyAsync(v => v.GameId == gameId && v.Nom == versionNom);
        if (dejaExistante)
        {
            errors.Add($"Une version nommée « {versionNom} » existe déjà pour ce jeu.");
            return (false, errors);
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Créer la version
            var nextOrdre = await db.RulesVersions
                .Where(v => v.GameId == gameId)
                .MaxAsync(v => (int?)v.Ordre) ?? 0;

            var version = new RulesVersion
            {
                GameId = gameId,
                Nom = versionNom,
                Ordre = nextOrdre + 1,
                EstActive = false
            };
            db.RulesVersions.Add(version);
            await db.SaveChangesAsync();

            // 2. Catégories de compétence
            // Fichier récent → on reprend ses catégories. Fichier antérieur à R2
            // (Categories absent) → on matérialise les 6 catégories standard, et les
            // compétences sont rattachées via leur ancien enum.
            var categorieMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var categoriesDto = dto.Categories is { Count: > 0 }
                ? dto.Categories
                : StandardSkillCategories.Toutes
                    .Select(t => new SkillCategoryGdDto(t.Nom, t.Code)).ToList();

            foreach (var c in categoriesDto)
            {
                var cat = new SkillCategoryDef
                {
                    RulesVersionId = version.Id,
                    Nom = c.Nom,
                    Code = c.Code
                };
                db.SkillCategories.Add(cat);
                await db.SaveChangesAsync();
                categorieMap[c.Nom] = cat.Id;
            }

            // 3. Compétences
            var skillMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in dto.Skills)
            {
                // Résolution par nom de catégorie ; repli sur l'ancien enum pour les
                // fichiers exportés avant R2.
                var nomCategorie = s.CategorieNom ?? StandardSkillCategories.Nom(s.Categorie);
                if (!categorieMap.TryGetValue(nomCategorie, out var categorieId))
                {
                    errors.Add($"Catégorie « {nomCategorie} » introuvable pour la compétence « {s.Nom} ».");
                    continue;
                }

                var skill = new Skill
                {
                    RulesVersionId = version.Id,
                    Nom = s.Nom,
                    Categorie = s.Categorie,
                    SkillCategoryDefId = categorieId,
                    Description = s.Description,
                    EstElite = s.EstElite,
                    EstTrait = s.EstTrait
                };
                db.Skills.Add(skill);
                await db.SaveChangesAsync();
                skillMap[s.Nom] = skill.Id;
            }

            // 4. TypesEquipes + Postes + Limites
            foreach (var ttDto in dto.TypesEquipes)
            {
                var tt = new TeamType
                {
                    GameId = gameId,
                    RulesVersionId = version.Id,
                    Nom = ttDto.Nom,
                    Categorie = ttDto.Categorie,
                    CoutRelance = ttDto.CoutRelance,
                    ReglesSpeciales = ttDto.ReglesSpeciales,
                    ReglesSpecialesLigue = ttDto.ReglesSpecialesLigue
                };
                db.TeamTypes.Add(tt);
                await db.SaveChangesAsync();

                foreach (var pDto in ttDto.Postes)
                {
                    var pos = new PlayerPosition
                    {
                        TeamTypeId = tt.Id,
                        Nom = pDto.Nom,
                        QuantiteMax = pDto.QuantiteMax,
                        Cout = pDto.Cout,
                        Mouvement = pDto.Mouvement,
                        Force = pDto.Force,
                        Agilite = pDto.Agilite,
                        CapacitePasse = pDto.CapacitePasse,
                        Armure = pDto.Armure,
                        CompetencesPrincipales = pDto.CompetencesPrincipales,
                        CompetencesSecondaires = pDto.CompetencesSecondaires,
                        MotsCles = pDto.MotsCles
                    };
                    db.PlayerPositions.Add(pos);
                    await db.SaveChangesAsync();

                    foreach (var nomSkill in pDto.CompetencesDepart)
                    {
                        if (!skillMap.TryGetValue(nomSkill, out var skillId))
                        {
                            errors.Add($"Compétence de départ introuvable : « {nomSkill} » (poste : {pDto.Nom})");
                            continue;
                        }
                        db.PlayerPositionSkills.Add(new PlayerPositionSkill
                        {
                            PlayerPositionId = pos.Id,
                            SkillId = skillId
                        });
                    }
                    await db.SaveChangesAsync();
                }

                foreach (var lim in ttDto.Limites)
                {
                    db.TeamTypeKeywordLimits.Add(new TeamTypeKeywordLimit
                    {
                        TeamTypeId = tt.Id,
                        MotCle = lim.MotCle,
                        Max = lim.Max
                    });
                }
                await db.SaveChangesAsync();
            }

            // Réserve (PoolPosition) — skills de départ résolus par nom
            foreach (var pDto in dto.Reserve ?? [])
            {
                var pool = new PoolPosition
                {
                    RulesVersionId = version.Id,
                    Nom = pDto.Nom, QuantiteMax = pDto.QuantiteMax, Cout = pDto.Cout,
                    Mouvement = pDto.Mouvement, Force = pDto.Force, Agilite = pDto.Agilite,
                    CapacitePasse = pDto.CapacitePasse, Armure = pDto.Armure,
                    CompetencesPrincipales = pDto.CompetencesPrincipales,
                    CompetencesSecondaires = pDto.CompetencesSecondaires, MotsCles = pDto.MotsCles
                };
                db.PoolPositions.Add(pool);
                await db.SaveChangesAsync();

                foreach (var nomSkill in pDto.CompetencesDepart)
                {
                    if (!skillMap.TryGetValue(nomSkill, out var skillId))
                    {
                        errors.Add($"Compétence de départ (réserve) introuvable : « {nomSkill} » (poste : {pDto.Nom})");
                        continue;
                    }
                    db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = pool.Id, SkillId = skillId });
                }
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            logger.LogInformation("Import game data '{V}' : {NbTT} types, {NbS} skills, {NbErr} avertissements",
                versionNom, dto.TypesEquipes.Count, dto.Skills.Count, errors.Count);
            return (true, errors);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Import game data échoué pour version '{V}'", versionNom);
            return (false, [$"Erreur lors de l'import : {ex.Message}"]);
        }
    }

    // ── Réserve seule ─────────────────────────────────────────────────────────

    public async Task<byte[]> ExportReserveAsync(int rulesVersionId)
    {
        var version = await db.RulesVersions
            .Include(v => v.Game)
            .FirstOrDefaultAsync(v => v.Id == rulesVersionId)
            ?? throw new InvalidOperationException("Version de règles introuvable");

        var reserve = await db.PoolPositions
            .Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Where(p => p.RulesVersionId == rulesVersionId)
            .OrderBy(p => p.Nom)
            .ToListAsync();

        var dto = new ReserveExportDto(
            Jeu: version.Game.Nom,
            Version: version.Nom,
            Reserve: reserve.Select(p => new PlayerPositionGdDto(
                p.Nom, p.QuantiteMax, p.Cout, p.Mouvement, p.Force, p.Agilite,
                p.CapacitePasse, p.Armure, p.CompetencesPrincipales, p.CompetencesSecondaires,
                p.MotsCles,
                p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList()
            )).ToList()
        );

        logger.LogInformation("Export réserve : version '{V}' ({N} postes)", version.Nom, reserve.Count);
        return JsonSerializer.SerializeToUtf8Bytes(dto, JsonOpts);
    }

    /// <summary>
    /// Importe une réserve dans une version CIBLE. Mode AJOUT (append, pas de remplacement).
    /// Les compétences de départ sont résolues par nom parmi les skills de la version cible ;
    /// un nom introuvable est signalé mais n'interrompt pas l'import.
    /// </summary>
    public async Task<(bool Success, int Imported, List<string> Errors)> ImportReserveAsync(
        Stream stream, int rulesVersionId)
    {
        var errors = new List<string>();

        ReserveExportDto dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<ReserveExportDto>(stream, JsonOpts)
                ?? throw new InvalidOperationException("Fichier JSON invalide");
        }
        catch (Exception ex)
        {
            return (false, 0, [$"Impossible de lire le fichier : {ex.Message}"]);
        }

        var version = await db.RulesVersions.FindAsync(rulesVersionId);
        if (version is null)
            return (false, 0, [$"Version id={rulesVersionId} introuvable"]);

        // skills de la version cible, indexés par nom
        var skillMap = await db.Skills
            .Where(s => s.RulesVersionId == rulesVersionId)
            .ToDictionaryAsync(s => s.Nom, s => s.Id, StringComparer.OrdinalIgnoreCase);

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            int imported = 0;
            foreach (var pDto in dto.Reserve)
            {
                var pool = new PoolPosition
                {
                    RulesVersionId = rulesVersionId,
                    Nom = pDto.Nom, QuantiteMax = pDto.QuantiteMax, Cout = pDto.Cout,
                    Mouvement = pDto.Mouvement, Force = pDto.Force, Agilite = pDto.Agilite,
                    CapacitePasse = pDto.CapacitePasse, Armure = pDto.Armure,
                    CompetencesPrincipales = pDto.CompetencesPrincipales,
                    CompetencesSecondaires = pDto.CompetencesSecondaires, MotsCles = pDto.MotsCles
                };
                db.PoolPositions.Add(pool);
                await db.SaveChangesAsync();

                foreach (var nomSkill in pDto.CompetencesDepart)
                {
                    if (!skillMap.TryGetValue(nomSkill, out var skillId))
                    {
                        errors.Add($"Compétence introuvable dans la version cible : « {nomSkill} » (poste : {pDto.Nom})");
                        continue;
                    }
                    db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = pool.Id, SkillId = skillId });
                }
                await db.SaveChangesAsync();
                imported++;
            }

            await tx.CommitAsync();
            logger.LogInformation("Import réserve : {N} postes importés dans version {V} ({E} avertissements)",
                imported, rulesVersionId, errors.Count);
            return (true, imported, errors);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, 0, [$"Erreur lors de l'import : {ex.Message}"]);
        }
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

record GameDataExportDto(
    string Jeu,
    string Version,
    int Ordre,
    bool EstActive,
    List<SkillGdDto> Skills,
    List<TeamTypeGdDto> TypesEquipes,
    List<PlayerPositionGdDto>? Reserve = null,  // ← AJOUT optionnel (rétrocompat)
    List<SkillCategoryGdDto>? Categories = null // ← AJOUT optionnel (rétrocompat, R2)
);

record ReserveExportDto(
    string Jeu,
    string Version,
    List<PlayerPositionGdDto> Reserve
);

record SkillGdDto(
    string Nom,
    SkillCategory Categorie,
    string Description,
    bool EstElite,
    bool EstTrait,
    // Nom de la catégorie (catégories devenues éditables). Null sur les exports
    // antérieurs : on retombe alors sur le champ Categorie (ancien enum).
    string? CategorieNom = null
);

/// <summary>Catégorie de compétence exportée. Absent des fichiers antérieurs à R2.</summary>
record SkillCategoryGdDto(
    string Nom,
    string Code
);

record TeamTypeGdDto(
    string Nom,
    TeamCategory Categorie,
    int CoutRelance,
    string ReglesSpeciales,
    string ReglesSpecialesLigue,
    List<PlayerPositionGdDto> Postes,
    List<KeywordLimitGdDto> Limites
);

record PlayerPositionGdDto(
    string Nom,
    int QuantiteMax,
    int Cout,
    int Mouvement,
    int Force,
    string Agilite,
    string CapacitePasse,
    string Armure,
    string CompetencesPrincipales,
    string CompetencesSecondaires,
    string MotsCles,
    List<string> CompetencesDepart
);

record KeywordLimitGdDto(string MotCle, int Max);
