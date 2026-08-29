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

        // Règles spéciales : peuplées AUSSI sur une base existante.
        // Le bloc ci-dessus ne s'exécute que sur une base vierge ; sans cet
        // appel, toute instance déjà déployée (le VPS de l'association) aurait
        // un catalogue vide après mise à jour, et la fonctionnalité arriverait
        // inutilisable. La méthode est idempotente : elle ne fait rien si la
        // version a déjà des règles, donc une saisie manuelle n'est jamais
        // écrasée.
        await SeedReglesSpecialesToutesVersionsAsync(db, logger);

        // Les règles seedées AVANT l'ajout des comportements automatiques
        // existent déjà en base sans Code ni mot-clé : elles resteraient
        // descriptives pour toujours. On les complète ici, une seule fois.
        await ActiverComportementsAutomatiquesAsync(db, logger);
        await SeedLiguesThematiquesAsync(db, logger);

        await SeedAdminUserAsync(userManager, config);
    }

    /// <summary>
    /// Renseigne le <c>Code</c> et le mot-clé visé des règles déjà présentes en
    /// base, quand elles ont été créées avant que le comportement existe.
    ///
    /// Idempotent et prudent : on ne touche qu'une règle dont le Code est VIDE
    /// (jamais un choix fait en admin), et on ne remplit <c>OptionsChoix</c>
    /// que s'il est vide lui aussi. Un commissaire qui aurait déjà saisi un
    /// autre mot-clé garde sa valeur.
    /// </summary>
    private static async Task ActiverComportementsAutomatiquesAsync(
        ApplicationDbContext db, ILogger logger)
    {
        // Nom de la règle → (code à poser, mot-clé par défaut).
        var aBrancher = new Dictionary<string, (string Code, string MotCle)>
        {
            ["Trois-quarts à Vil Prix"] = (SpecialRuleCodes.CoutNulParMotCle, "Trois-quart"),
            ["Maîtres de la Non-Vie"] = (SpecialRuleCodes.RecrutementGratuitParMotCle, "Trois-quart"),
            ["Capitaine"] = (SpecialRuleCodes.CompetenceAuCapitaine, "Pro")
        };

        // ── Plafond de recrues offertes ─────────────────────────────────────
        // Traité À PART, avant le filtre sur r.Code : une migration AddColumn
        // pose 0 sur les lignes EXISTANTES, or 0 signifie « sans limite » —
        // exactement le trou qu'on corrige. Les règles DÉJÀ branchées sont
        // précisément celles concernées, et le filtre ci-dessous les exclut.
        var liensSansPlafond = await db.TeamTypeSpecialRules
            .Include(l => l.SpecialRule)
            .Where(l => l.SpecialRule.Code == SpecialRuleCodes.RecrutementGratuitParMotCle
                     && l.LimiteParApresMatch == 0)
            .ToListAsync();

        foreach (var lien in liensSansPlafond)
            lien.LimiteParApresMatch = 1;   // valeur du livre de règles

        if (liensSansPlafond.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Plafond de recrues offertes initialisé à 1 sur {Nb} rattachement(s)",
                liensSansPlafond.Count);
        }

        var regles = await db.SpecialRules
            .Where(r => r.Code == "" && aBrancher.Keys.Contains(r.Nom))
            .Include(r => r.TeamTypes)
            .ToListAsync();

        if (regles.Count == 0) return;

        foreach (var regle in regles)
        {
            var (code, motCle) = aBrancher[regle.Nom];
            regle.Code = code;

            foreach (var lien in regle.TeamTypes.Where(l => string.IsNullOrWhiteSpace(l.OptionsChoix)))
                lien.OptionsChoix = motCle;

            logger.LogInformation(
                "Règle « {Nom} » (id={Id}) branchée sur le comportement {Code}, mot-clé « {MotCle} »",
                regle.Nom, regle.Id, code, motCle);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Construit le catalogue de ligues thématiques à partir de l'ancienne
    /// saisie en texte libre (colonne « ReglesSpecialesLigue »), puis rattache
    /// chaque race aux ligues correspondantes.
    ///
    /// Sans ce backfill, la fonctionnalité arriverait INERTE sur une base en
    /// service : les 60 races déjà renseignées perdraient leurs ligues et le
    /// commissaire devrait tout ressaisir.
    ///
    /// Idempotent : ne crée que ce qui manque, ne touche pas aux rattachements
    /// faits à la main depuis l'écran d'administration.
    /// </summary>
    private static async Task SeedLiguesThematiquesAsync(
        ApplicationDbContext db, ILogger logger)
    {
        var races = await db.TeamTypes
            .Include(t => t.LiguesListe)
            .Where(t => t.LiguesTexteObsolete != "")
            .ToListAsync();

        if (races.Count == 0) return;

        var catalogue = await db.ThemedLeagues.ToListAsync();
        var creees = 0;
        var rattachees = 0;

        foreach (var race in races)
        {
            var noms = race.LiguesTexteObsolete
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var nom in noms)
            {
                var ligue = catalogue.FirstOrDefault(l =>
                    l.RulesVersionId == race.RulesVersionId &&
                    string.Equals(l.Nom, nom, StringComparison.OrdinalIgnoreCase));

                if (ligue is null)
                {
                    ligue = new ThemedLeague { RulesVersionId = race.RulesVersionId, Nom = nom };
                    db.ThemedLeagues.Add(ligue);
                    await db.SaveChangesAsync();   // besoin de l'Id pour la liaison
                    catalogue.Add(ligue);
                    creees++;
                }

                if (race.LiguesListe.All(x => x.ThemedLeagueId != ligue.Id))
                {
                    db.Set<TeamTypeThemedLeague>().Add(new TeamTypeThemedLeague
                    {
                        TeamTypeId = race.Id, ThemedLeagueId = ligue.Id
                    });
                    rattachees++;
                }
            }
        }

        if (creees > 0 || rattachees > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Ligues thématiques : {Creees} ligue(s) créée(s), {Rattachees} rattachement(s)",
                creees, rattachees);
        }
    }

    /// <summary>
    /// Applique le catalogue de règles spéciales à toute version de Blood Bowl
    /// qui n'en a pas encore. Rattachements résolus par nom d'équipe.
    /// </summary>
    private static async Task SeedReglesSpecialesToutesVersionsAsync(
        ApplicationDbContext db, ILogger logger)
    {
        var versionsBB = await db.RulesVersions
            .Include(v => v.Game)
            .Where(v => v.Game.Type == GameType.BloodBowl)
            .Select(v => v.Id)
            .ToListAsync();

        foreach (var versionId in versionsBB)
        {
            if (await db.SpecialRules.AnyAsync(r => r.RulesVersionId == versionId)) continue;

            await SeedReglesSpecialesAsync(db, versionId);
            logger.LogInformation(
                "Règles spéciales initialisées pour la version de règles id={VersionId}", versionId);
        }
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
            // Barème d'XP par défaut = LRB S3. Le touchdown vaut 5 en Dungeon Bowl.
            new RulesVersion { GameId = bb.Id, Nom = "Saison 3", EstActive = true, Ordre = 1,
                               XpParTouchdown = 3 },
            new RulesVersion { GameId = dbg.Id, Nom = "Edition 2022", EstActive = true, Ordre = 1,
                               XpParTouchdown = 5 }
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

        // Staff standard : sur une base NEUVE la migration de backfill n'a rien à
        // reprendre, il faut donc créer les définitions ici — sinon une nouvelle
        // installation n'aurait ni fans, ni relances, ni apothicaire.
        foreach (var versionId in new[] { versionBB.Id, versionDB.Id })
            await SeedStaffStandardAsync(db, versionId);

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
    /// Crée les cinq staff standard d'une version de règles. Mêmes valeurs que
    /// le backfill de la migration AddStaffConfigurable, pour qu'une base neuve
    /// et une base migrée partent du même état.
    /// Idempotent : ne fait rien si la version a déjà du staff.
    /// </summary>
    private static async Task SeedStaffStandardAsync(ApplicationDbContext db, int versionId)
    {
        if (await db.StaffTypes.AnyAsync(s => s.RulesVersionId == versionId)) return;

        db.StaffTypes.AddRange(
            new StaffDefinition
            {
                RulesVersionId = versionId, Nom = "Fans dévoués", Ordre = 1,
                Description = "Public fidèle de l'équipe. Influence l'affluence et les gains de match.",
                Cout = 10_000, MinCreation = 1, MaxCreation = 9, MaxLigue = null
            },
            new StaffDefinition
            {
                RulesVersionId = versionId, Nom = "Relances", Ordre = 2,
                Description = "Relances d'équipe disponibles au début de chaque match. Leur prix dépend de la race.",
                Cout = 0, CoutDepuisTypeEquipe = true, MinCreation = 0, MaxCreation = 8, MaxLigue = 8
            },
            new StaffDefinition
            {
                RulesVersionId = versionId, Nom = "Coachs assistants", Ordre = 3,
                Description = "Chaque coach assistant aide à récupérer l'avantage de terrain.",
                Cout = 10_000, MinCreation = 0, MaxCreation = 6, MaxLigue = null
            },
            new StaffDefinition
            {
                RulesVersionId = versionId, Nom = "Cheerleaders", Ordre = 4,
                Description = "Chaque cheerleader aide à récupérer l'avantage de terrain.",
                Cout = 10_000, MinCreation = 0, MaxCreation = 6, MaxLigue = null
            },
            new StaffDefinition
            {
                RulesVersionId = versionId, Nom = "Apothicaire", Ordre = 5,
                Description = "Permet de relancer un jet de blessure une fois par match.",
                Cout = 50_000, MinCreation = 0, MaxCreation = 1, MaxLigue = 1
            });

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

        // Appelé aussi par SeedReglesSpecialesToutesVersionsAsync pour les
        // bases déjà en service.
        await SeedReglesSpecialesAsync(db, bbVersion.Id);
    }

    /// <summary>
    /// Catalogue de règles spéciales (LRB p.93-94) et rattachement aux fiches
    /// d'équipe. Idempotent : ne fait rien si la version en a déjà.
    ///
    /// Appelé APRÈS la création des TeamTypes, dont les rattachements dépendent.
    /// </summary>
    private static async Task SeedReglesSpecialesAsync(ApplicationDbContext db, int versionId)
    {
        if (await db.SpecialRules.AnyAsync(r => r.RulesVersionId == versionId)) return;

        var regles = SpecialRuleSeedData.GetRegles(versionId).ToList();
        db.SpecialRules.AddRange(regles);
        await db.SaveChangesAsync();

        var parNom = regles.ToDictionary(r => r.Nom, r => r.Id);
        var equipes = await db.TeamTypes
            .Where(t => t.RulesVersionId == versionId)
            .ToDictionaryAsync(t => t.Nom, t => t.Id);

        foreach (var (nomRegle, nomEquipe, options) in SpecialRuleSeedData.GetRattachements())
        {
            // Un nom qui ne correspond à rien serait une faute de frappe du
            // seed : on l'ignore silencieusement plutôt que d'empêcher toute
            // l'application de démarrer, mais un test verrouille la cohérence.
            if (!parNom.TryGetValue(nomRegle, out var regleId)) continue;
            if (!equipes.TryGetValue(nomEquipe, out var equipeId)) continue;

            db.TeamTypeSpecialRules.Add(new TeamTypeSpecialRule
            {
                TeamTypeId = equipeId,
                SpecialRuleId = regleId,
                OptionsChoix = options
            });
        }
        await db.SaveChangesAsync();
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
