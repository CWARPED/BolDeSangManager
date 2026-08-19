using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BolDeSangManager.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await db.Database.MigrateAsync();
        await SeedRolesAsync(roleManager, userManager);

        if (!db.Games.Any())
        {
            await SeedGamesAndVersionsAsync(db);
            await SeedSkillsAsync(db);
            await SeedBloodBowlTeamsAsync(db);
            await SeedDungeonBowlTeamsAsync(db);
            await SeedPositionSkillsAsync(db, logger);
            await SeedPositionCategoryAccessAsync(db, logger);
        }

        await SeedAdminUserAsync(userManager, config);
    }

    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Créer les nouveaux rôles s'ils n'existent pas
        foreach (var role in new[] { "Admin", "GrandCommissaire", "Coach" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Migration douce : si "Commissaire" existe encore, transférer ses utilisateurs vers "Admin" puis supprimer le rôle.
        if (await roleManager.RoleExistsAsync("Commissaire"))
        {
            var anciensCommissaires = await userManager.GetUsersInRoleAsync("Commissaire");
            foreach (var user in anciensCommissaires)
            {
                if (!await userManager.IsInRoleAsync(user, "Admin"))
                    await userManager.AddToRoleAsync(user, "Admin");
                await userManager.RemoveFromRoleAsync(user, "Commissaire");
            }

            var oldRole = await roleManager.FindByNameAsync("Commissaire");
            if (oldRole is not null)
                await roleManager.DeleteAsync(oldRole);
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        var adminEmail = config["BolDeSang:AdminEmail"] ?? "commissaire@boldesang.fr";
        var adminPassword = config["BolDeSang:AdminPassword"] ?? "Commissaire123!";
        var adminPseudo = config["BolDeSang:AdminPseudo"] ?? "Grand Commissaire";

        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            PseudoCoach = adminPseudo,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedGamesAndVersionsAsync(ApplicationDbContext db)
    {
        var bb = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
        var dbg = new Game { Nom = "Dungeon Bowl", Type = GameType.DungeonBowl };
        db.Games.AddRange(bb, dbg);
        await db.SaveChangesAsync();

        db.RulesVersions.AddRange(
            new RulesVersion { GameId = bb.Id, Nom = "Saison 3", EstActive = true, Ordre = 1 },
            new RulesVersion { GameId = dbg.Id, Nom = "Edition 2022", EstActive = true, Ordre = 1 }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedSkillsAsync(ApplicationDbContext db)
    {
        var versionBB = await db.RulesVersions
            .Include(v => v.Game)
            .FirstAsync(v => v.Game.Type == GameType.BloodBowl && v.EstActive);
        var versionDB = await db.RulesVersions
            .Include(v => v.Game)
            .FirstAsync(v => v.Game.Type == GameType.DungeonBowl && v.EstActive);

        // Les catégories standard doivent exister avant les compétences qui les référencent.
        var categoriesParVersion = new Dictionary<int, Dictionary<SkillCategory, int>>();
        foreach (var versionId in new[] { versionBB.Id, versionDB.Id })
            categoriesParVersion[versionId] = await SeedCategoriesStandardAsync(db, versionId);

        foreach (var (versionId, gameType) in new[]
                 { (versionBB.Id, GameType.BloodBowl), (versionDB.Id, GameType.DungeonBowl) })
        {
            var map = categoriesParVersion[versionId];
            foreach (var skill in SkillSeedData.GetSkills(versionId, gameType))
            {
                skill.SkillCategoryDefId = map[skill.Categorie];
                db.Skills.Add(skill);
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Crée les catégories standard (LRB S3) d'une version si elles n'existent pas déjà,
    /// et renvoie la correspondance ancien enum → identifiant de catégorie.
    /// </summary>
    private static async Task<Dictionary<SkillCategory, int>> SeedCategoriesStandardAsync(
        ApplicationDbContext db, int versionId)
    {
        var existantes = await db.SkillCategories
            .Where(c => c.RulesVersionId == versionId)
            .ToListAsync();

        var map = new Dictionary<SkillCategory, int>();
        foreach (var (valeurEnum, nom, code) in StandardSkillCategories.Toutes)
        {
            var cat = existantes.FirstOrDefault(c => c.Nom == nom);
            if (cat is null)
            {
                cat = new SkillCategoryDef { RulesVersionId = versionId, Nom = nom, Code = code };
                db.SkillCategories.Add(cat);
                await db.SaveChangesAsync();
            }
            map[valeurEnum] = cat.Id;
        }
        return map;
    }

    private static async Task SeedBloodBowlTeamsAsync(ApplicationDbContext db)
    {
        var bbGame = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
        var bbVersion = await db.RulesVersions.FirstAsync(v => v.GameId == bbGame.Id && v.EstActive);
        foreach (var (type, positions, limites) in BloodBowlTeamSeedData.GetTeams(bbGame.Id, bbVersion.Id))
        {
            db.TeamTypes.Add(type);
            await db.SaveChangesAsync();
            foreach (var pos in positions)
            {
                pos.TeamTypeId = type.Id;
                db.PlayerPositions.Add(pos);
            }
            foreach (var limite in limites)
            {
                limite.TeamTypeId = type.Id;
                db.TeamTypeKeywordLimits.Add(limite);
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDungeonBowlTeamsAsync(ApplicationDbContext db)
    {
        var dbGame = await db.Games.FirstAsync(g => g.Type == GameType.DungeonBowl);
        var dbVersion = await db.RulesVersions.FirstAsync(v => v.GameId == dbGame.Id && v.EstActive);
        foreach (var (type, positions) in DungeonBowlTeamSeedData.GetColleges(dbGame.Id, dbVersion.Id))
        {
            db.TeamTypes.Add(type);
            await db.SaveChangesAsync();
            foreach (var pos in positions)
            {
                pos.TeamTypeId = type.Id;
                db.PlayerPositions.Add(pos);
            }
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Convertit les codes d'accès du seed (« GAF » / « AS », champs [NotMapped]) en
    /// lignes PlayerPositionCategoryAccess. Les codes sont résolus par le Code des
    /// catégories de la version du poste.
    /// </summary>
    private static async Task SeedPositionCategoryAccessAsync(ApplicationDbContext db, ILogger logger)
    {
        var positions = await db.PlayerPositions
            .Include(p => p.TeamType)
            .ToListAsync();

        var categoriesParVersion = (await db.SkillCategories.ToListAsync())
            .GroupBy(c => c.RulesVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var nbAcces = 0;
        foreach (var pos in positions)
        {
            if (!categoriesParVersion.TryGetValue(pos.TeamType.RulesVersionId, out var cats)) continue;

            var principales = CategoryAccessHelpers.ResoudreCodesHistoriques(pos.CompetencesPrincipales, cats);
            var secondaires = CategoryAccessHelpers.ResoudreCodesHistoriques(pos.CompetencesSecondaires, cats)
                .Where(c => principales.All(p => p.Id != c.Id))   // principal l'emporte
                .ToList();

            foreach (var cat in principales)
            {
                db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                {
                    PlayerPositionId = pos.Id, SkillCategoryDefId = cat.Id, EstPrincipale = true
                });
                nbAcces++;
            }
            foreach (var cat in secondaires)
            {
                db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                {
                    PlayerPositionId = pos.Id, SkillCategoryDefId = cat.Id, EstPrincipale = false
                });
                nbAcces++;
            }
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seed : {N} accès de catégorie créés pour {P} postes", nbAcces, positions.Count);
    }

    private static async Task SeedPositionSkillsAsync(ApplicationDbContext db, ILogger logger)
    {
        var allPositions = await db.PlayerPositions
            .Include(p => p.CompetencesDepart)
            .Include(p => p.TeamType)
            .ToListAsync();

        // Map (versionId) → (skillName → Skill)
        var allSkillsByVersion = await db.Skills.ToListAsync();
        var skillsParVersion = allSkillsByVersion
            .GroupBy(s => s.RulesVersionId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.Nom.ToLower()));

        var missing = new HashSet<string>();
        foreach (var position in allPositions)
        {
            if (string.IsNullOrEmpty(position._StartingSkillsTemp)) continue;

            var positionVersionId = position.TeamType?.RulesVersionId ?? 0;
            if (positionVersionId == 0) continue;

            if (!skillsParVersion.TryGetValue(positionVersionId, out var skillsForThisVersion))
                continue;

            var skillNames = position._StartingSkillsTemp.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawName in skillNames)
            {
                var name = rawName.Trim().ToLower();
                if (skillsForThisVersion.TryGetValue(name, out var skill))
                {
                    if (!position.CompetencesDepart.Any(pps => pps.SkillId == skill.Id))
                    {
                        db.PlayerPositionSkills.Add(new PlayerPositionSkill
                        {
                            PlayerPositionId = position.Id,
                            SkillId = skill.Id
                        });
                    }
                }
                else
                {
                    missing.Add($"{position.Nom} (v{positionVersionId}) → {rawName.Trim()}");
                }
            }
        }
        await db.SaveChangesAsync();

        if (missing.Count > 0)
            logger.LogWarning("Skills de départ non trouvés dans la base : {Missing}", string.Join(" ; ", missing));
    }
}
