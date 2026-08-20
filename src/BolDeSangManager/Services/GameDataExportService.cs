using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Helpers;
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
            .Include(tt => tt.Postes).ThenInclude(p => p.AccesCategories).ThenInclude(a => a.SkillCategoryDef)
            .Include(tt => tt.LimitesMotsCles)
            .Where(tt => tt.RulesVersionId == rulesVersionId)
            .OrderBy(tt => tt.Nom)
            .ToListAsync();

        var reserve = await db.PoolPositions
            .Include(p => p.CompetencesDepart).ThenInclude(pps => pps.Skill)
            .Include(p => p.AccesCategories).ThenInclude(a => a.SkillCategoryDef)
            .Where(p => p.RulesVersionId == rulesVersionId)
            .OrderBy(p => p.Nom)
            .ToListAsync();

        var categories = await db.SkillCategories
            .Where(c => c.RulesVersionId == rulesVersionId)
            .OrderBy(c => c.Nom)
            .ToListAsync();

        // F3 : chaque export produit une nouvelle révision, persistée sur la
        // version. Sans persistance le numéro repartirait à 1 à chaque fois et
        // ne prouverait rien.
        version.Revision++;
        version.DernierExportLe = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = new GameDataExportDto(
            Jeu: version.Game.Nom,
            Version: version.Nom,
            Ordre: version.Ordre,
            EstActive: version.EstActive,
            Revision: version.Revision,
            ExporteLe: version.DernierExportLe,
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
                    p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList(),
                    p.AccesCategories.Where(a => a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList(),
                    p.AccesCategories.Where(a => !a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList()
                )).ToList(),
                tt.LimitesMotsCles.Select(l => new KeywordLimitGdDto(l.MotCle, l.Max)).ToList()
            )).ToList(),
            Reserve: reserve.Select(p => new PlayerPositionGdDto(
                p.Nom, p.QuantiteMax, p.Cout, p.Mouvement, p.Force, p.Agilite,
                p.CapacitePasse, p.Armure, p.CompetencesPrincipales, p.CompetencesSecondaires,
                p.MotsCles,
                p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList(),
                p.AccesCategories.Where(a => a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList(),
                p.AccesCategories.Where(a => !a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList()
            )).ToList(),
            Categories: categories.Select(c => new SkillCategoryGdDto(c.Nom, c.Code)).ToList(),
            XpParTouchdown: version.XpParTouchdown,
            XpParPasse: version.XpParPasse,
            XpParInterception: version.XpParInterception,
            XpParElimination: version.XpParElimination,
            XpBonusMvp: version.XpBonusMvp
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
                EstActive = false,
                // F3 : la version importée hérite de la révision du fichier, pour
                // qu'on sache de quelle livraison elle provient. Sans ça la
                // traçabilité serait perdue au clonage.
                Revision = dto.Revision ?? 0,
                DernierExportLe = dto.ExporteLe,
                // Barème d'XP (R6) — un export antérieur n'a pas ces champs :
                // on retombe alors sur les valeurs par défaut du jeu.
                XpParTouchdown    = dto.XpParTouchdown    ?? XpBareme.ParDefaut(game.Type).ParTouchdown,
                XpParPasse        = dto.XpParPasse        ?? 1,
                XpParInterception = dto.XpParInterception ?? 2,
                XpParElimination  = dto.XpParElimination  ?? 2,
                XpBonusMvp        = dto.XpBonusMvp        ?? 4
            };
            db.RulesVersions.Add(version);
            await db.SaveChangesAsync();

            // 2. Catégories de compétence
            // Fichier récent → on reprend ses catégories. Fichier antérieur à R2
            // (Categories absent) → on matérialise les 6 catégories standard, et les
            // compétences sont rattachées via leur ancien enum.
            var categorieMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var categoriesCreees = new List<SkillCategoryDef>();
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
                categoriesCreees.Add(cat);
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

                    var (accP, accS) = ResoudreAccesImport(pDto, categoriesCreees);
                    foreach (var catId in accP)
                        db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                        { PlayerPositionId = pos.Id, SkillCategoryDefId = catId, EstPrincipale = true });
                    foreach (var catId in accS)
                        db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                        { PlayerPositionId = pos.Id, SkillCategoryDefId = catId, EstPrincipale = false });
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

                var (poolP, poolS) = ResoudreAccesImport(pDto, categoriesCreees);
                foreach (var catId in poolP)
                    db.PoolPositionCategoryAccesses.Add(new PoolPositionCategoryAccess
                    { PoolPositionId = pool.Id, SkillCategoryDefId = catId, EstPrincipale = true });
                foreach (var catId in poolS)
                    db.PoolPositionCategoryAccesses.Add(new PoolPositionCategoryAccess
                    { PoolPositionId = pool.Id, SkillCategoryDefId = catId, EstPrincipale = false });
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
            .Include(p => p.AccesCategories).ThenInclude(a => a.SkillCategoryDef)
            .Where(p => p.RulesVersionId == rulesVersionId)
            .OrderBy(p => p.Nom)
            .ToListAsync();

        // F3 : la Réserve fait partie des données de la version — un export
        // Réserve compte donc comme une révision de cette version.
        version.Revision++;
        version.DernierExportLe = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = new ReserveExportDto(
            Jeu: version.Game.Nom,
            Version: version.Nom,
            Reserve: reserve.Select(p => new PlayerPositionGdDto(
                p.Nom, p.QuantiteMax, p.Cout, p.Mouvement, p.Force, p.Agilite,
                p.CapacitePasse, p.Armure, p.CompetencesPrincipales, p.CompetencesSecondaires,
                p.MotsCles,
                p.CompetencesDepart.Select(pps => pps.Skill.Nom).OrderBy(n => n).ToList(),
                p.AccesCategories.Where(a => a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList(),
                p.AccesCategories.Where(a => !a.EstPrincipale).Select(a => a.SkillCategoryDef.Nom).OrderBy(n => n).ToList()
            )).ToList(),
            Revision: version.Revision,
            ExporteLe: version.DernierExportLe
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
        Stream stream, int rulesVersionId, bool confirmerMalgreRevision = false)
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

        // F3 : refuser d'appliquer un fichier plus ancien que la base sans que
        // l'utilisateur l'ait explicitement confirmé.
        var controle = TracabiliteImport.Verifier(dto.Revision, version.Revision, dto.ExporteLe);
        if (controle.DemandeConfirmation && !confirmerMalgreRevision)
            return (false, 0, [controle.Message]);
        if (controle.Verdict != TracabiliteImport.Verdict.PlusRecent)
            logger.LogWarning("Import Réserve sur version {V} : {Message}", version.Nom, controle.Message);

        // skills de la version cible, indexés par nom
        var skillMap = await db.Skills
            .Where(s => s.RulesVersionId == rulesVersionId)
            .ToDictionaryAsync(s => s.Nom, s => s.Id, StringComparer.OrdinalIgnoreCase);

        // catégories de la version cible, pour résoudre les accès des postes importés
        var categoriesCible = await db.SkillCategories
            .Where(c => c.RulesVersionId == rulesVersionId)
            .ToListAsync();

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

                var (poolP, poolS) = ResoudreAccesImport(pDto, categoriesCible);
                foreach (var catId in poolP)
                    db.PoolPositionCategoryAccesses.Add(new PoolPositionCategoryAccess
                    { PoolPositionId = pool.Id, SkillCategoryDefId = catId, EstPrincipale = true });
                foreach (var catId in poolS)
                    db.PoolPositionCategoryAccesses.Add(new PoolPositionCategoryAccess
                    { PoolPositionId = pool.Id, SkillCategoryDefId = catId, EstPrincipale = false });
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

    /// <summary>
    /// Résout les accès de catégorie d'un poste importé. Priorité aux listes par NOM
    /// (exports R2b) ; repli sur les codes historiques « GAF » pour les fichiers antérieurs.
    /// Renvoie (principales, secondaires) sous forme d'identifiants de catégorie.
    /// </summary>
    private static (List<int> principales, List<int> secondaires) ResoudreAccesImport(
        PlayerPositionGdDto dto, List<SkillCategoryDef> categories)
    {
        List<int> ParNoms(List<string> noms) => noms
            .Select(n => categories.FirstOrDefault(c => string.Equals(c.Nom, n, StringComparison.OrdinalIgnoreCase)))
            .Where(c => c is not null)
            .Select(c => c!.Id)
            .Distinct()
            .ToList();

        var principales = dto.AccesPrincipal is { Count: > 0 }
            ? ParNoms(dto.AccesPrincipal)
            : CategoryAccessHelpers.ResoudreCodesHistoriques(dto.CompetencesPrincipales, categories).Select(c => c.Id).ToList();

        var secondaires = dto.AccesSecondaire is { Count: > 0 }
            ? ParNoms(dto.AccesSecondaire)
            : CategoryAccessHelpers.ResoudreCodesHistoriques(dto.CompetencesSecondaires, categories).Select(c => c.Id).ToList();

        return (principales, secondaires.Where(s => !principales.Contains(s)).ToList());
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
    List<SkillCategoryGdDto>? Categories = null, // ← AJOUT optionnel (rétrocompat, R2)
    // Barème d'XP de la version (R6). Optionnels : un export antérieur reprend
    // les valeurs par défaut du jeu à l'import.
    // Traçabilité (F3). Optionnels : un JSON exporté avant cette version n'en a
    // pas, et doit rester importable tel quel.
    int? Revision = null,
    DateTime? ExporteLe = null,
    int? XpParTouchdown = null,
    int? XpParPasse = null,
    int? XpParInterception = null,
    int? XpParElimination = null,
    int? XpBonusMvp = null
);

record ReserveExportDto(
    string Jeu,
    string Version,
    List<PlayerPositionGdDto> Reserve,
    // Traçabilité (F3), optionnelle pour rester rétrocompatible.
    int? Revision = null,
    DateTime? ExporteLe = null
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
    // Codes historiques (« GAF »). Conservés pour relire les exports antérieurs à R2b ;
    // ignorés dès que AccesPrincipal / AccesSecondaire sont présents.
    string CompetencesPrincipales,
    string CompetencesSecondaires,
    string MotsCles,
    List<string> CompetencesDepart,
    // Accès par NOM de catégorie (R2b) : seule forme compatible avec des codes à 2 caractères.
    List<string>? AccesPrincipal = null,
    List<string>? AccesSecondaire = null
);

record KeywordLimitGdDto(string MotCle, int Max);
