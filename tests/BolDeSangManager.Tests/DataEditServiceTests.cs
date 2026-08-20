using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BolDeSangManager.Tests;

public class DataEditServiceTests
{
    private static (int gameId, int versionId) SeedVersion(Data.ApplicationDbContext db)
    {
        var game = new Game { Nom = "Blood Bowl", Type = GameType.BloodBowl };
        db.Games.Add(game);
        db.SaveChanges();
        var v = new RulesVersion { GameId = game.Id, Nom = "Saison 3", EstActive = true, Ordre = 1 };
        db.RulesVersions.Add(v);
        db.SaveChanges();
        return (game.Id, v.Id);
    }

    [Fact]
    public async Task AjouterReserve_PersisteLePoste()
    {
        using var factory = new TestDbFactory();
        int versionId;
        using (var db = factory.CreateContext())
            (_, versionId) = SeedVersion(db);

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var data = new PoolPosition { Nom = "Ogre mercenaire", Cout = 140_000, Force = 5, Mouvement = 5 };
            await svc.AjouterReserveAsync(versionId, data, Array.Empty<int>(), DataEditService.AccesCategoriesInput.Vide);
        }

        using (var db = factory.CreateContext())
        {
            var liste = await db.PoolPositions.Where(p => p.RulesVersionId == versionId).ToListAsync();
            Assert.Single(liste);
            Assert.Equal("Ogre mercenaire", liste[0].Nom);
            Assert.Equal(5, liste[0].Force);
        }
    }

    [Fact]
    public async Task ImporterReserve_CopiePosteEtSkills_EtResteIndependant()
    {
        using var factory = new TestDbFactory();
        int versionId, teamTypeId, poolId, skillId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var catId = await DataSeeder.GetOrCreateCategorieAsync(db, versionId);
            var skill = new Skill { Nom = "Châtaigne", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = versionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;

            var tt = new TeamType { GameId = gameId, RulesVersionId = versionId, Nom = "Humains" };
            db.TeamTypes.Add(tt); db.SaveChanges(); teamTypeId = tt.Id;

            var pool = new PoolPosition { RulesVersionId = versionId, Nom = "Ogre", Cout = 140_000, Force = 5 };
            db.PoolPositions.Add(pool); db.SaveChanges(); poolId = pool.Id;
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = poolId, SkillId = skillId });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ImporterReserveVersTeamTypeAsync(teamTypeId, new[] { poolId });
        }

        using (var db = factory.CreateContext())
        {
            var poste = await db.PlayerPositions
                .Include(p => p.CompetencesDepart)
                .FirstOrDefaultAsync(p => p.TeamTypeId == teamTypeId && p.Nom == "Ogre");
            Assert.NotNull(poste);
            Assert.Equal(5, poste!.Force);
            Assert.Single(poste.CompetencesDepart);
            Assert.Equal(skillId, poste.CompetencesDepart.First().SkillId);
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.SupprimerReserveAsync(poolId);
        }
        using (var db = factory.CreateContext())
        {
            var poste = await db.PlayerPositions
                .Include(p => p.CompetencesDepart)
                .FirstOrDefaultAsync(p => p.TeamTypeId == teamTypeId && p.Nom == "Ogre");
            Assert.NotNull(poste);
            Assert.Single(poste!.CompetencesDepart);
        }
    }

    [Fact]
    public async Task ExporterPosteVersReserve_CopiePosteEtSkills()
    {
        using var factory = new TestDbFactory();
        int versionId, posteId, skillId, catId, catAgiliteId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            catId = await DataSeeder.GetOrCreateCategorieAsync(db, versionId);
            catAgiliteId = await DataSeeder.GetOrCreateCategorieAsync(db, versionId, "Agilité", "A");
            var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = versionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;

            var tt = new TeamType { GameId = gameId, RulesVersionId = versionId, Nom = "Humains" };
            db.TeamTypes.Add(tt); db.SaveChanges();

            var poste = new PlayerPosition
            {
                TeamTypeId = tt.Id, Nom = "Trois-quart", QuantiteMax = 16, Cout = 50_000,
                Mouvement = 6, Force = 3, Agilite = "3+", CapacitePasse = "4+", Armure = "9+",
                MotsCles = "Humain"
            };
            db.PlayerPositions.Add(poste); db.SaveChanges(); posteId = poste.Id;
            db.PlayerPositionSkills.Add(new PlayerPositionSkill { PlayerPositionId = posteId, SkillId = skillId });
            // accès : Générale en principal, Agilité en secondaire
            db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                { PlayerPositionId = posteId, SkillCategoryDefId = catId, EstPrincipale = true });
            db.PlayerPositionCategoryAccesses.Add(new PlayerPositionCategoryAccess
                { PlayerPositionId = posteId, SkillCategoryDefId = catAgiliteId, EstPrincipale = false });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ExporterPosteVersReserveAsync(posteId);
        }

        using (var db = factory.CreateContext())
        {
            var pool = await db.PoolPositions
                .Include(p => p.CompetencesDepart)
                .Include(p => p.AccesCategories)
                .FirstOrDefaultAsync(p => p.RulesVersionId == versionId && p.Nom == "Trois-quart");
            Assert.NotNull(pool);
            Assert.Equal(16, pool!.QuantiteMax);
            Assert.Equal(50_000, pool.Cout);
            Assert.Equal(6, pool.Mouvement);
            Assert.Equal(3, pool.Force);
            Assert.Equal("3+", pool.Agilite);
            Assert.Equal("4+", pool.CapacitePasse);
            Assert.Equal("9+", pool.Armure);
            Assert.Equal("Humain", pool.MotsCles);
            // les accès de catégorie suivent la copie, avec leur nature principal/secondaire
            Assert.Equal(catId, pool.AccesCategories.Single(a => a.EstPrincipale).SkillCategoryDefId);
            Assert.Equal(catAgiliteId, pool.AccesCategories.Single(a => !a.EstPrincipale).SkillCategoryDefId);
            Assert.Single(pool.CompetencesDepart);
            Assert.Equal(skillId, pool.CompetencesDepart.First().SkillId);
        }
    }

    [Fact]
    public async Task ExporterPosteVersReserve_RefuseSiNomDejaPresent()
    {
        using var factory = new TestDbFactory();
        int posteId, versionId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var tt = new TeamType { GameId = gameId, RulesVersionId = versionId, Nom = "Humains" };
            db.TeamTypes.Add(tt); db.SaveChanges();

            var poste = new PlayerPosition { TeamTypeId = tt.Id, Nom = "Ogre", Cout = 140_000, Force = 5 };
            db.PlayerPositions.Add(poste); db.SaveChanges(); posteId = poste.Id;

            db.PoolPositions.Add(new PoolPosition { RulesVersionId = versionId, Nom = "ogre", Force = 4 });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.ExporterPosteVersReserveAsync(posteId));
            Assert.Contains("Ogre", ex.Message);
        }

        using (var db = factory.CreateContext())
        {
            var pools = await db.PoolPositions.Where(p => p.RulesVersionId == versionId).ToListAsync();
            Assert.Single(pools);
            Assert.Equal(4, pools[0].Force); // l'existant n'a pas été écrasé
        }
    }

    [Fact]
    public async Task ExporterPosteVersReserve_LaCopieEstIndependante()
    {
        using var factory = new TestDbFactory();
        int posteId, versionId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var tt = new TeamType { GameId = gameId, RulesVersionId = versionId, Nom = "Humains" };
            db.TeamTypes.Add(tt); db.SaveChanges();
            var poste = new PlayerPosition { TeamTypeId = tt.Id, Nom = "Blitzeur", Cout = 85_000, Force = 3 };
            db.PlayerPositions.Add(poste); db.SaveChanges(); posteId = poste.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ExporterPosteVersReserveAsync(posteId);
        }

        // Le poste d'origine reste dans le TeamType (copie, pas déplacement)
        using (var db = factory.CreateContext())
            Assert.NotNull(await db.PlayerPositions.FindAsync(posteId));

        // Supprimer le poste d'origine n'affecte pas la Réserve
        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.SupprimerPosteAsync(posteId);
        }

        using (var db = factory.CreateContext())
        {
            var pool = await db.PoolPositions.FirstOrDefaultAsync(p => p.RulesVersionId == versionId && p.Nom == "Blitzeur");
            Assert.NotNull(pool);
            Assert.Equal(85_000, pool!.Cout);
        }
    }

    [Fact]
    public async Task ClonerVersion_CopieAussiLaReserve()
    {
        using var factory = new TestDbFactory();
        int gameId, srcVersionId, skillId, poolId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; srcVersionId = vId;
            var catId = await DataSeeder.GetOrCreateCategorieAsync(db, srcVersionId);
            var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = srcVersionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;
            var pool = new PoolPosition { RulesVersionId = srcVersionId, Nom = "Troll", Force = 5 };
            db.PoolPositions.Add(pool); db.SaveChanges(); poolId = pool.Id;
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = poolId, SkillId = skillId });
            db.SaveChanges();
        }

        int newVersionId;
        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var nouvelle = await svc.CreerVersionAsync(gameId, "Saison 4", 2, false, srcVersionId);
            newVersionId = nouvelle.Id;
        }

        using (var db = factory.CreateContext())
        {
            var pools = await db.PoolPositions
                .Include(p => p.CompetencesDepart)
                .Where(p => p.RulesVersionId == newVersionId).ToListAsync();
            Assert.Single(pools);
            Assert.Equal("Troll", pools[0].Nom);
            var skillCloneId = pools[0].CompetencesDepart.Single().SkillId;
            var skillClone = await db.Skills.FindAsync(skillCloneId);
            Assert.Equal(newVersionId, skillClone!.RulesVersionId);
        }
    }

    [Fact]
    public async Task SupprimerVersion_SupprimeAussiLaReserveEtSesCompetences()
    {
        using var factory = new TestDbFactory();
        int versionId, skillId, poolId;

        using (var db = factory.CreateContext())
        {
            var (_, vId) = SeedVersion(db);
            versionId = vId;
            // version non active : on ne peut pas supprimer la version active
            var v = await db.RulesVersions.FindAsync(versionId);
            v!.EstActive = false;
            var catId = await DataSeeder.GetOrCreateCategorieAsync(db, versionId);
            var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = versionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;
            var pool = new PoolPosition { RulesVersionId = versionId, Nom = "Troll", Force = 5 };
            db.PoolPositions.Add(pool); db.SaveChanges(); poolId = pool.Id;
            db.PoolPositionSkills.Add(new PoolPositionSkill { PoolPositionId = poolId, SkillId = skillId });
            db.PoolPositionCategoryAccesses.Add(new PoolPositionCategoryAccess
            {
                PoolPositionId = poolId,
                SkillCategoryDefId = catId,
                EstPrincipale = true
            });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.SupprimerVersionAsync(versionId);
        }

        using (var db = factory.CreateContext())
        {
            Assert.Null(await db.RulesVersions.FindAsync(versionId));
            Assert.Empty(await db.PoolPositions.Where(p => p.RulesVersionId == versionId).ToListAsync());
            Assert.Empty(await db.PoolPositionSkills.Where(s => s.PoolPositionId == poolId).ToListAsync());
            Assert.Empty(await db.PoolPositionCategoryAccesses.Where(a => a.PoolPositionId == poolId).ToListAsync());
            Assert.Empty(await db.Skills.Where(s => s.RulesVersionId == versionId).ToListAsync());
            Assert.Empty(await db.SkillCategories.Where(c => c.RulesVersionId == versionId).ToListAsync());
        }
    }

    [Fact]
    public async Task SupprimerVersion_RefuseSiUneLigueLUtilise()
    {
        using var factory = new TestDbFactory();
        int versionId, gameId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            versionId = vId; gameId = gId;
            var v = await db.RulesVersions.FindAsync(versionId);
            v!.EstActive = false;
            var user = new Data.ApplicationUser { UserName = "commish", Email = "c@x.fr", PseudoCoach = "Commish" };
            db.Users.Add(user);
            db.SaveChanges();
            db.Leagues.Add(new League
            {
                Nom = "Ligue test",
                GameId = gameId,
                RulesVersionId = versionId,
                CommissaireId = user.Id
            });
            db.SaveChanges();
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.SupprimerVersionAsync(versionId));
            Assert.Contains("ligue", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        using (var db = factory.CreateContext())
            Assert.NotNull(await db.RulesVersions.FindAsync(versionId));
    }

    [Fact]
    public async Task ClonerVersion_SupporteUneCompetenceRattacheeAUneCategorieEtrangere()
    {
        // Reproduit la donnée corrompue trouvée en base réelle : un Skill de la
        // version A pointe vers une catégorie appartenant à une autre version.
        using var factory = new TestDbFactory();
        int gameId, srcVersionId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; srcVersionId = vId;

            var autreVersion = new RulesVersion { GameId = gameId, Nom = "Autre", Ordre = 9 };
            db.RulesVersions.Add(autreVersion);
            db.SaveChanges();

            // catégories normales de la version source
            await DataSeeder.GetOrCreateCategorieAsync(db, srcVersionId);
            foreach (var (_, nom, code) in StandardSkillCategories.Toutes)
                if (!db.SkillCategories.Any(c => c.RulesVersionId == srcVersionId && c.Nom == nom))
                    db.SkillCategories.Add(new SkillCategoryDef { RulesVersionId = srcVersionId, Nom = nom, Code = code });

            // catégorie appartenant à l'AUTRE version
            var catEtrangere = new SkillCategoryDef { RulesVersionId = autreVersion.Id, Nom = "Agilité", Code = "A" };
            db.SkillCategories.Add(catEtrangere);
            db.SaveChanges();

            db.Skills.Add(new Skill
            {
                Nom = "Balle Collante",
                Categorie = SkillCategory.Agilite,
                SkillCategoryDefId = catEtrangere.Id,   // ← incohérent
                RulesVersionId = srcVersionId
            });
            db.SaveChanges();
        }

        int newVersionId;
        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            var nouvelle = await svc.CreerVersionAsync(gameId, "Clone repare", 2, false, srcVersionId);
            newVersionId = nouvelle.Id;
        }

        using (var db = factory.CreateContext())
        {
            var clone = await db.Skills
                .SingleAsync(s => s.RulesVersionId == newVersionId && s.Nom == "Balle Collante");
            var cat = await db.SkillCategories.FindAsync(clone.SkillCategoryDefId);
            // la copie doit pointer vers une catégorie de SA version, nommée comme l'originale
            Assert.Equal(newVersionId, cat!.RulesVersionId);
            Assert.Equal("Agilité", cat.Nom);
        }
    }

    [Fact]
    public async Task ModifierSkill_RefuseUneCategorieDUneAutreVersion()
    {
        using var factory = new TestDbFactory();
        int versionId, skillId, catEtrangereId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var catId = await DataSeeder.GetOrCreateCategorieAsync(db, versionId);

            var autreVersion = new RulesVersion { GameId = gameId, Nom = "Autre", Ordre = 9 };
            db.RulesVersions.Add(autreVersion);
            db.SaveChanges();
            var catEtrangere = new SkillCategoryDef { RulesVersionId = autreVersion.Id, Nom = "Force", Code = "F" };
            db.SkillCategories.Add(catEtrangere);
            db.SaveChanges();
            catEtrangereId = catEtrangere.Id;

            var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, SkillCategoryDefId = catId, RulesVersionId = versionId };
            db.Skills.Add(skill); db.SaveChanges(); skillId = skill.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.ModifierSkillAsync(skillId, "Blocage", catEtrangereId, "", false, false));
        }
    }

    [Fact]
    public async Task CreerVersion_SiLeClonageEchoue_AucuneVersionResiduelle()
    {
        using var factory = new TestDbFactory();
        int gameId, srcVersionId;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; srcVersionId = vId;

            // Une compétence dont la catégorie est introuvable ET dont aucune
            // catégorie standard homonyme n'existe → le clonage doit échouer.
            var autreVersion = new RulesVersion { GameId = gameId, Nom = "Autre", Ordre = 9 };
            db.RulesVersions.Add(autreVersion);
            db.SaveChanges();
            var catEtrangere = new SkillCategoryDef { RulesVersionId = autreVersion.Id, Nom = "Agilité", Code = "A" };
            db.SkillCategories.Add(catEtrangere);
            db.SaveChanges();

            db.Skills.Add(new Skill
            {
                Nom = "Cassée",
                Categorie = SkillCategory.Agilite,
                SkillCategoryDefId = catEtrangere.Id,
                RulesVersionId = srcVersionId
            });
            db.SaveChanges();
            // NOTE : la version source n'a AUCUNE catégorie → pas de repli possible
        }

        int versionsAvant;
        using (var db = factory.CreateContext())
            versionsAvant = await db.RulesVersions.CountAsync();

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.CreerVersionAsync(gameId, "Doit disparaitre", 2, false, srcVersionId));
        }

        using (var db = factory.CreateContext())
        {
            Assert.Equal(versionsAvant, await db.RulesVersions.CountAsync());
            Assert.Empty(await db.RulesVersions.Where(v => v.Nom == "Doit disparaitre").ToListAsync());
        }
    }

    [Fact]
    public async Task ActiverVersion_RendActiveEtDesactiveLautreDuMemeJeu()
    {
        using var factory = new TestDbFactory();
        int gameId, v1Id, v2Id;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);   // v1 : active
            gameId = gId; v1Id = vId;
            var v2 = new RulesVersion { GameId = gameId, Nom = "Saison 4", Ordre = 2, EstActive = false };
            db.RulesVersions.Add(v2); db.SaveChanges(); v2Id = v2.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ActiverVersionAsync(v2Id);
        }

        using (var db = factory.CreateContext())
        {
            Assert.True((await db.RulesVersions.FindAsync(v2Id))!.EstActive);
            Assert.False((await db.RulesVersions.FindAsync(v1Id))!.EstActive);
            // exactement une version active pour ce jeu
            Assert.Equal(1, await db.RulesVersions.CountAsync(v => v.GameId == gameId && v.EstActive));
        }
    }

    [Fact]
    public async Task ActiverVersion_NaffectePasLautreJeu()
    {
        using var factory = new TestDbFactory();
        int autreJeuVersionId, v2Id;

        using (var db = factory.CreateContext())
        {
            var (gameId, _) = SeedVersion(db);  // Blood Bowl, v1 active

            var v2 = new RulesVersion { GameId = gameId, Nom = "Saison 4", Ordre = 2, EstActive = false };
            db.RulesVersions.Add(v2);

            // Un autre jeu, avec sa propre version active
            var autreJeu = new Game { Nom = "Dungeon Bowl", Type = GameType.DungeonBowl };
            db.Games.Add(autreJeu); db.SaveChanges();
            var vAutre = new RulesVersion { GameId = autreJeu.Id, Nom = "Edition 2022", Ordre = 1, EstActive = true };
            db.RulesVersions.Add(vAutre);
            db.SaveChanges();
            v2Id = v2.Id; autreJeuVersionId = vAutre.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ActiverVersionAsync(v2Id);
        }

        using (var db = factory.CreateContext())
        {
            // la version active de l'AUTRE jeu doit rester active
            Assert.True((await db.RulesVersions.FindAsync(autreJeuVersionId))!.EstActive);
        }
    }

    [Fact]
    public async Task ActiverVersion_DejaActive_EstSansEffet()
    {
        using var factory = new TestDbFactory();
        int gameId, v1Id;

        using (var db = factory.CreateContext())
        {
            var (gId, vId) = SeedVersion(db);
            gameId = gId; v1Id = vId;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.ActiverVersionAsync(v1Id);   // déjà active : idempotent
        }

        using (var db = factory.CreateContext())
        {
            Assert.True((await db.RulesVersions.FindAsync(v1Id))!.EstActive);
            Assert.Equal(1, await db.RulesVersions.CountAsync(v => v.GameId == gameId && v.EstActive));
        }
    }

    [Fact]
    public async Task RenommerVersion_ChangeLeNomSansToucherAuReste()
    {
        using var factory = new TestDbFactory();
        int versionId;

        using (var db = factory.CreateContext())
        {
            var (_, vId) = SeedVersion(db);
            versionId = vId;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.RenommerVersionAsync(versionId, "  Édition 2026 — Saison 1  ");
        }

        using (var db = factory.CreateContext())
        {
            var v = await db.RulesVersions.FindAsync(versionId);
            Assert.Equal("Édition 2026 — Saison 1", v!.Nom);   // trim appliqué
            Assert.True(v.EstActive);                           // statut préservé
            Assert.Equal(1, v.Ordre);                           // ordre préservé
        }
    }

    [Fact]
    public async Task RenommerVersion_RefuseUnNomVide()
    {
        using var factory = new TestDbFactory();
        int versionId;

        using (var db = factory.CreateContext())
        {
            var (_, vId) = SeedVersion(db);
            versionId = vId;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.RenommerVersionAsync(versionId, "   "));
        }

        using (var db = factory.CreateContext())
            Assert.Equal("Saison 3", (await db.RulesVersions.FindAsync(versionId))!.Nom);
    }

    [Fact]
    public async Task RenommerVersion_RefuseUnNomDejaPrisDansLeMemeJeu()
    {
        using var factory = new TestDbFactory();
        int v2Id;

        using (var db = factory.CreateContext())
        {
            var (gameId, _) = SeedVersion(db);      // "Saison 3"
            var v2 = new RulesVersion { GameId = gameId, Nom = "Saison 4", Ordre = 2 };
            db.RulesVersions.Add(v2); db.SaveChanges(); v2Id = v2.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            // casse différente : le doublon doit quand même être détecté
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.RenommerVersionAsync(v2Id, "saison 3"));
            Assert.Contains("s'appelle déjà", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        using (var db = factory.CreateContext())
            Assert.Equal("Saison 4", (await db.RulesVersions.FindAsync(v2Id))!.Nom);
    }

    [Fact]
    public async Task RenommerVersion_AutoriseLeMemeNomDansUnAutreJeu()
    {
        using var factory = new TestDbFactory();
        int vAutreJeuId;

        using (var db = factory.CreateContext())
        {
            SeedVersion(db);   // Blood Bowl / "Saison 3"

            var autreJeu = new Game { Nom = "Dungeon Bowl", Type = GameType.DungeonBowl };
            db.Games.Add(autreJeu); db.SaveChanges();
            var v = new RulesVersion { GameId = autreJeu.Id, Nom = "Edition 2022", Ordre = 1 };
            db.RulesVersions.Add(v); db.SaveChanges(); vAutreJeuId = v.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            // "Saison 3" existe déjà, mais pour l'AUTRE jeu → autorisé
            await svc.RenommerVersionAsync(vAutreJeuId, "Saison 3");
        }

        using (var db = factory.CreateContext())
            Assert.Equal("Saison 3", (await db.RulesVersions.FindAsync(vAutreJeuId))!.Nom);
    }

    [Fact]
    public async Task RenommerVersion_NeCassePasLesLiguesQuiLUtilisent()
    {
        using var factory = new TestDbFactory();
        int versionId, leagueId;

        using (var db = factory.CreateContext())
        {
            var (gameId, vId) = SeedVersion(db);
            versionId = vId;
            var user = new Data.ApplicationUser { UserName = "c", Email = "c@x.fr", PseudoCoach = "C" };
            db.Users.Add(user); db.SaveChanges();
            var league = new League { Nom = "Ligue", GameId = gameId, RulesVersionId = versionId, CommissaireId = user.Id };
            db.Leagues.Add(league); db.SaveChanges(); leagueId = league.Id;
        }

        using (var db = factory.CreateContext())
        {
            var svc = new DataEditService(db, NullLogger<DataEditService>.Instance);
            await svc.RenommerVersionAsync(versionId, "Nouveau nom");
        }

        using (var db = factory.CreateContext())
        {
            // la ligue pointe toujours sur la même version, désormais renommée
            var league = await db.Leagues.FindAsync(leagueId);
            Assert.Equal(versionId, league!.RulesVersionId);
            Assert.Equal("Nouveau nom", (await db.RulesVersions.FindAsync(versionId))!.Nom);
        }
    }
}
