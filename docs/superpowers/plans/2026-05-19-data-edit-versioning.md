# Page d'édition de données + versioning par RulesVersion — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permettre aux Admin/GrandCommissaire d'éditer les données de jeu (équipes, postes, compétences, limites mot-clé) via une page dédiée, avec un système de versions de règles qui isole les éditions par `RulesVersion`.

**Architecture:** Lier `TeamType` et `Skill` à `RulesVersion` (au lieu de `Game`). Page `/admin/donnees` avec sélecteurs Game+Version + 3 onglets (Équipes, Compétences, Versions). Service `DataEditService` qui centralise les CRUD avec validations (unicité par version, blocage suppression si dépendances). Clonage de version transactionnel pour créer Saison N+1 à partir de Saison N.

**Tech Stack:** .NET 9, EF Core 9 (SQLite), Blazor Server, MudBlazor 8.

**Spec de référence:** `docs/superpowers/specs/2026-05-19-data-edit-versioning-design.md`

**Note tests** : par directive utilisateur, les tests d'intégration sont reportés à une itération ultérieure. Le plan inclut juste des smoke tests post-build pour vérifier que le seed et l'app démarrent.

---

## Structure des fichiers

### À créer

| Fichier | Responsabilité |
|---|---|
| `src/BolDeSangManager/Services/DataEditService.cs` | CRUD validé pour TeamType, PlayerPosition, Skill, RulesVersion, TeamTypeKeywordLimit |
| `src/BolDeSangManager/Components/Pages/Admin/Donnees.razor` | Page principale `/admin/donnees` avec onglets |
| `src/BolDeSangManager/Components/Pages/Admin/EditionEquipe.razor` | Page édition d'un TeamType (form + sous-tableaux) |
| `src/BolDeSangManager/Components/Pages/Admin/EditionPosteDialog.razor` | Modale édition d'un PlayerPosition (form complet) |
| `src/BolDeSangManager/Components/Pages/Admin/EditionSkillDialog.razor` | Modale édition d'un Skill |
| `src/BolDeSangManager/Components/Pages/Admin/CreerVersionDialog.razor` | Modale création RulesVersion (avec/sans clonage) |
| `src/BolDeSangManager/Data/Migrations/<ts>_AddRulesVersionToTeamTypeAndSkill.cs` | Migration EF schema-only (auto-gen) |

### À modifier

| Fichier | Changement |
|---|---|
| `src/BolDeSangManager/Data/Models/TeamType.cs` | + `RulesVersionId` (FK), supprimer `GameId` (redondant via RulesVersion.GameId) |
| `src/BolDeSangManager/Data/Models/Skill.cs` | + `RulesVersionId` (FK), supprimer `GameSpecifique` |
| `src/BolDeSangManager/Data/ApplicationDbContext.cs` | + 2 FK configs |
| `src/BolDeSangManager/Data/Seeding/SkillSeedData.cs` | Refactor : générer 2 sets de skills (par version) au lieu d'un set avec `GameSpecifique` |
| `src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs` | Signature `GetTeams` accepte `bbVersionId` au lieu de `bbGameId` |
| `src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs` | Signature `GetColleges` accepte `dbVersionId` au lieu de `dbGameId` |
| `src/BolDeSangManager/Data/DbSeeder.cs` | Adapter les appels seed pour passer versionId + dédupliquer skills entre versions |
| `src/BolDeSangManager/Services/TeamService.cs` | Filtrer les TeamTypes par RulesVersion de la ligue (déjà fait via GameId, à mettre à jour) |
| `src/BolDeSangManager/Components/Layout/NavMenu.razor` | + lien "Édition de données" visible Admin+GC |
| `src/BolDeSangManager/Services/LeagueExportService.cs` | Adapter import/export pour le nouveau modèle (Skill par version) |
| `src/BolDeSangManager/Components/Pages/Equipes/Creer.razor` | Filtrer TeamType par `_ligue.RulesVersionId` au lieu de `Game` |

### Conventions

- Commits atomiques, message en français impératif.
- Build green à chaque étape.
- Pas de tests dans cette itération (cf note ci-dessus).
- Reset DB en dev autorisé (SQLite local).

---

## Task 1 — Modèle : `TeamType.RulesVersionId`

**Files:**
- Modify: `src/BolDeSangManager/Data/Models/TeamType.cs`

- [ ] **Step 1.1 — Ajouter le champ et la nav property**

Dans `TeamType.cs`, après la ligne `public Game Game { get; set; } = null!;`, ajouter :

```csharp
public int RulesVersionId { get; set; }
public RulesVersion RulesVersion { get; set; } = null!;
```

> On garde `GameId/Game` pour rétrocompat — `RulesVersion.GameId` est la source de vérité mais on duplique pour éviter une migration trop lourde sur cette itération. À nettoyer plus tard.

- [ ] **Step 1.2 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors (le seed plantera à l'exécution mais on s'en occupe plus tard).

- [ ] **Step 1.3 — Commit**

```bash
git add src/BolDeSangManager/Data/Models/TeamType.cs
git commit -m "feat: ajouter RulesVersionId sur TeamType"
```

---

## Task 2 — Modèle : `Skill.RulesVersionId` (remplace `GameSpecifique`)

**Files:**
- Modify: `src/BolDeSangManager/Data/Models/Skill.cs`

- [ ] **Step 2.1 — Remplacer `GameSpecifique` par `RulesVersionId`**

Dans `Skill.cs`, trouver :

```csharp
// null = skill universel ; sinon limité à ce jeu (ex: skills DungeonBowl uniquement)
public GameType? GameSpecifique { get; set; }
```

Remplacer par :

```csharp
// Chaque skill appartient à une version précise. Les skills universels sont dupliqués entre versions au seed.
public int RulesVersionId { get; set; }
public RulesVersion RulesVersion { get; set; } = null!;
```

- [ ] **Step 2.2 — Build (échouera : usages de GameSpecifique cassés)**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`

Expected : erreurs sur les usages de `GameSpecifique` dans `SkillSeedData.cs`. Noter les lignes, elles seront corrigées en Task 4.

- [ ] **Step 2.3 — Commit temporaire**

```bash
git add src/BolDeSangManager/Data/Models/Skill.cs
git commit -m "feat: remplacer Skill.GameSpecifique par RulesVersionId (build cassé jusqu'à Task 4)"
```

> Build cassé est OK ici car les Tasks 3 et 4 le rétablissent.

---

## Task 3 — Configurer DbContext (FK)

**Files:**
- Modify: `src/BolDeSangManager/Data/ApplicationDbContext.cs`

- [ ] **Step 3.1 — Ajouter FK config**

Dans `OnModelCreating`, juste avant la fermeture de l'accolade, ajouter :

```csharp
// TeamType → RulesVersion (Restrict pour éviter cascade circulaire avec Game→RulesVersion→TeamType)
builder.Entity<TeamType>()
    .HasOne(t => t.RulesVersion)
    .WithMany()
    .HasForeignKey(t => t.RulesVersionId)
    .OnDelete(DeleteBehavior.Restrict);

// Skill → RulesVersion
builder.Entity<Skill>()
    .HasOne(s => s.RulesVersion)
    .WithMany()
    .HasForeignKey(s => s.RulesVersionId)
    .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 3.2 — Build (toujours cassé sur SkillSeedData — OK)**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : mêmes erreurs sur `GameSpecifique` qu'en Task 2 (à corriger en Task 4).

- [ ] **Step 3.3 — Commit**

```bash
git add src/BolDeSangManager/Data/ApplicationDbContext.cs
git commit -m "feat: FK config TeamType.RulesVersionId + Skill.RulesVersionId"
```

---

## Task 4 — Adapter `SkillSeedData` pour générer 2 sets par version

**Files:**
- Modify: `src/BolDeSangManager/Data/Seeding/SkillSeedData.cs`

L'objectif : la méthode `GetSkills()` doit accepter une `versionId` et produire les skills assignés à cette version. Le `DbSeeder` appellera cette méthode 2 fois (BB S3 et DB Edition 2022) pour dupliquer les skills universels.

- [ ] **Step 4.1 — Changer la signature et le contenu**

Dans `SkillSeedData.cs`, remplacer la signature actuelle :

```csharp
public static IEnumerable<Skill> GetSkills()
{
    ...
}
```

par :

```csharp
public static IEnumerable<Skill> GetSkills(int versionId, GameType game)
{
    // ... corps adapté ci-dessous
}
```

Dans le corps, **remplacer chaque** `yield return new Skill { ... }` par `yield return new Skill { RulesVersionId = versionId, ... }`.

Trouver les 4 skills marqués `GameSpecifique = GameType.DungeonBowl` (Transmission dans la Course, Passe par un Portail, Lancer contre un Mur, Navigateur de Portail) et **les rendre conditionnels** :

Au lieu de :
```csharp
yield return new Skill { Nom = "Transmission dans la Course", Categorie = SkillCategory.Passe, Description = "...", GameSpecifique = GameType.DungeonBowl };
```

Faire :
```csharp
if (game == GameType.DungeonBowl)
    yield return new Skill { RulesVersionId = versionId, Nom = "Transmission dans la Course", Categorie = SkillCategory.Passe, Description = "..." };
```

(Et supprimer `GameSpecifique` partout.)

Pour les autres skills (universels), ils sont yield-returnés **inconditionnellement** mais avec le `RulesVersionId` passé en paramètre.

- [ ] **Step 4.2 — Vérifier le compte attendu**

À l'appel `GetSkills(versionS3, GameType.BloodBowl)` → ~116 skills (sans les 4 DB-specific).
À l'appel `GetSkills(versionDB, GameType.DungeonBowl)` → ~120 skills (116 universels + 4 DB-specific).

- [ ] **Step 4.3 — Build (toujours cassé sur DbSeeder qui appelle l'ancienne signature)**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : erreurs sur `DbSeeder.cs` (corrigées en Task 5).

- [ ] **Step 4.4 — Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/SkillSeedData.cs
git commit -m "refactor: SkillSeedData prend versionId et game, supprime GameSpecifique"
```

---

## Task 5 — Adapter `BloodBowlTeamSeedData` et `DungeonBowlTeamSeedData`

**Files:**
- Modify: `src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs`
- Modify: `src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs`

Les méthodes `GetTeams(int bbGameId)` et `GetColleges(int dbGameId)` doivent passer une `RulesVersionId` aux TeamTypes qu'elles créent.

- [ ] **Step 5.1 — `BloodBowlTeamSeedData.cs`**

Changer la signature :
```csharp
public static IEnumerable<TeamSeed> GetTeams(int bbGameId)
```
en :
```csharp
public static IEnumerable<TeamSeed> GetTeams(int bbGameId, int bbVersionId)
```

Dans chaque `new TeamType { ... }` ajouter le champ `RulesVersionId = bbVersionId,` (juste après `GameId = bbGameId,`).

- [ ] **Step 5.2 — `DungeonBowlTeamSeedData.cs`**

Idem : signature `GetColleges(int dbGameId, int dbVersionId)`, ajouter `RulesVersionId = dbVersionId,` dans chaque `new TeamType`.

- [ ] **Step 5.3 — Build (toujours cassé sur DbSeeder)**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : erreurs uniquement sur DbSeeder.cs (Task 6 corrigera).

- [ ] **Step 5.4 — Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs
git commit -m "refactor: seed data files passent RulesVersionId aux TeamTypes"
```

---

## Task 6 — Adapter `DbSeeder` (chaînage versionId)

**Files:**
- Modify: `src/BolDeSangManager/Data/DbSeeder.cs`

Le seed doit maintenant :
1. Créer les Games + RulesVersions (existant)
2. Capturer les VersionId actives (S3 pour BB, Edition 2022 pour DB)
3. Appeler `SkillSeedData.GetSkills(versionS3, BloodBowl)` puis `GetSkills(versionDB, DungeonBowl)` (2 sets)
4. Appeler `BloodBowlTeamSeedData.GetTeams(gameBB.Id, versionS3.Id)` et `DungeonBowlTeamSeedData.GetColleges(gameDB.Id, versionDB.Id)`
5. Lier `PlayerPositionSkill` aux skills de la même version (le code existant fait un lookup par nom — à filtrer par version)

- [ ] **Step 6.1 — Modifier `SeedGamesAndVersionsAsync` pour retourner les versions**

Si la méthode actuelle ne retourne pas les versions, modifier pour capturer leurs IDs. Garder la signature `Task` mais après l'appel, dans `SeedAsync`, requérir les versions actives :

```csharp
var versionBB = await db.RulesVersions.FirstAsync(v => v.GameId == bb.Id && v.EstActive);
var versionDB = await db.RulesVersions.FirstAsync(v => v.GameId == dbg.Id && v.EstActive);
```

> `bb` et `dbg` sont les variables `Game` créées dans `SeedGamesAndVersionsAsync`. Si elles sont locales à cette méthode, soit retourner les IDs, soit refaire un `FirstAsync` dans `SeedAsync`.

- [ ] **Step 6.2 — Modifier `SeedSkillsAsync`**

Actuel :
```csharp
private static async Task SeedSkillsAsync(ApplicationDbContext db)
{
    db.Skills.AddRange(SkillSeedData.GetSkills());
    await db.SaveChangesAsync();
}
```

Remplacer par :
```csharp
private static async Task SeedSkillsAsync(ApplicationDbContext db)
{
    var versionBB = await db.RulesVersions.FirstAsync(v => v.Game.Type == GameType.BloodBowl && v.EstActive);
    var versionDB = await db.RulesVersions.FirstAsync(v => v.Game.Type == GameType.DungeonBowl && v.EstActive);

    db.Skills.AddRange(SkillSeedData.GetSkills(versionBB.Id, GameType.BloodBowl));
    db.Skills.AddRange(SkillSeedData.GetSkills(versionDB.Id, GameType.DungeonBowl));
    await db.SaveChangesAsync();
}
```

- [ ] **Step 6.3 — Modifier `SeedBloodBowlTeamsAsync` et `SeedDungeonBowlTeamsAsync`**

Actuel :
```csharp
private static async Task SeedBloodBowlTeamsAsync(ApplicationDbContext db)
{
    var bbGame = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
    foreach (var (type, positions) in BloodBowlTeamSeedData.GetTeams(bbGame.Id))
    {
        ...
    }
}
```

Remplacer par :
```csharp
private static async Task SeedBloodBowlTeamsAsync(ApplicationDbContext db)
{
    var bbGame = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
    var bbVersion = await db.RulesVersions.FirstAsync(v => v.GameId == bbGame.Id && v.EstActive);

    foreach (var (type, positions) in BloodBowlTeamSeedData.GetTeams(bbGame.Id, bbVersion.Id))
    {
        ...
    }
}
```

> Note : le 3ᵉ élément du tuple (`LimitesMotsCles`) existe déjà — ne pas le casser.

Idem pour `SeedDungeonBowlTeamsAsync`.

- [ ] **Step 6.4 — Modifier `SeedPositionSkillsAsync` pour matcher skill par version**

Actuel : lookup `allSkills.TryGetValue(name, out var skill)`. Maintenant on a des doublons de nom entre versions. Il faut filtrer par version :

Remplacer :
```csharp
var allSkills = await db.Skills.ToDictionaryAsync(s => s.Nom.ToLower());
```

par :
```csharp
// Map (versionId, nom_lowercase) → Skill
var allSkillsByVersion = await db.Skills
    .GroupBy(s => s.RulesVersionId)
    .ToDictionaryAsync(g => g.Key, g => g.ToDictionary(s => s.Nom.ToLower()));
```

Puis dans la boucle, récupérer la version du TeamType de la position :

```csharp
var positionVersionId = position.TeamType?.RulesVersionId ?? 0;
if (positionVersionId == 0) continue; // safety

var skillsParVersion = allSkillsByVersion.GetValueOrDefault(positionVersionId);
if (skillsParVersion is null) continue;

foreach (var rawName in skillNames)
{
    var name = rawName.Trim().ToLower();
    if (skillsParVersion.TryGetValue(name, out var skill))
    {
        // crée PlayerPositionSkill (idem qu'avant)
    }
    else
    {
        missing.Add($"{position.Nom} (v{positionVersionId}) → {rawName.Trim()}");
    }
}
```

> Note : le `position.TeamType` doit être chargé. Modifier la requête initiale :
```csharp
var allPositions = await db.PlayerPositions
    .Include(p => p.CompetencesDepart)
    .Include(p => p.TeamType)  // ← ajouter ce Include
    .ToListAsync();
```

- [ ] **Step 6.5 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 6.6 — Reset DB + smoke**

```bash
rm -f src/BolDeSangManager/Data/boldesang.db
cd src/BolDeSangManager && timeout 25 dotnet run --no-launch-profile 2>&1 | head -60 ; cd ../..
```

Expected : démarrage propre, seed runs OK, app listens.

- [ ] **Step 6.7 — Commit**

```bash
git add src/BolDeSangManager/Data/DbSeeder.cs
git commit -m "refactor: DbSeeder chaîne RulesVersionId à skills et TeamTypes"
```

---

## Task 7 — Migration EF schema

**Files:**
- Auto-gen : `src/BolDeSangManager/Data/Migrations/<ts>_AddRulesVersionToTeamTypeAndSkill.cs`

- [ ] **Step 7.1 — Générer la migration**

Run :
```bash
cd src/BolDeSangManager && dotnet ef migrations add AddRulesVersionToTeamTypeAndSkill && cd ../..
```

Inspecter le fichier généré :
- `AddColumn RulesVersionId` sur `TeamTypes` et `Skills`
- `DropColumn GameSpecifique` sur `Skills`
- 2 nouveaux FK + index

- [ ] **Step 7.2 — Reset DB + run app**

```bash
rm -f src/BolDeSangManager/Data/boldesang.db
cd src/BolDeSangManager && timeout 25 dotnet run --no-launch-profile 2>&1 | head -40 ; cd ../..
```

Expected : migration appliquée, seed OK.

- [ ] **Step 7.3 — Commit**

```bash
git add src/BolDeSangManager/Data/Migrations/
git commit -m "feat: migration AddRulesVersionToTeamTypeAndSkill"
```

---

## Task 8 — Adapter `LeagueExportService` au nouveau modèle

**Files:**
- Modify: `src/BolDeSangManager/Services/LeagueExportService.cs`

Le service utilise probablement les noms de skills/positions pour résoudre lors de l'import. Avec versions multiples, il faut filtrer par `RulesVersionId` de la ligue.

- [ ] **Step 8.1 — Localiser les lookups par nom**

Run :
```bash
grep -n "FirstOrDefault\|Where\|Skills\|PlayerPositions" src/BolDeSangManager/Services/LeagueExportService.cs | head -20
```

- [ ] **Step 8.2 — Filtrer par RulesVersionId**

Pour chaque lookup de Skill ou TeamType par nom, ajouter un filtre `&& x.RulesVersionId == ligue.RulesVersionId`.

Exemple typique :
```csharp
var skill = await db.Skills.FirstOrDefaultAsync(s => s.Nom == nom);
```
Devient :
```csharp
var skill = await db.Skills.FirstOrDefaultAsync(s => s.Nom == nom && s.RulesVersionId == ligue.RulesVersionId);
```

Idem pour TeamType.

- [ ] **Step 8.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 8.4 — Commit**

```bash
git add src/BolDeSangManager/Services/LeagueExportService.cs
git commit -m "refactor: LeagueExportService filtre skills/teamtypes par RulesVersion de la ligue"
```

---

## Task 9 — Adapter `Equipes/Creer.razor` (filtre par version)

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Equipes/Creer.razor`
- Modify: `src/BolDeSangManager/Services/TeamService.cs` (méthode `GetTypesEquipesAsync`)

Actuellement la page Creer.razor liste les TeamTypes via `TeamService.GetTypesEquipesAsync(gameId)`. Il faut maintenant filtrer par la `RulesVersionId` de la ligue.

- [ ] **Step 9.1 — Ajouter une méthode au `TeamService`**

Dans `src/BolDeSangManager/Services/TeamService.cs`, ajouter (ou modifier la méthode existante) :

```csharp
public async Task<List<TeamType>> GetTypesEquipesParVersionAsync(int versionId) =>
    await db.TeamTypes
        .Include(t => t.Postes)
        .Where(t => t.RulesVersionId == versionId)
        .OrderBy(t => t.Nom)
        .ToListAsync();
```

Garder l'ancienne `GetTypesEquipesAsync(int gameId)` pour rétrocompat (si elle est appelée ailleurs).

- [ ] **Step 9.2 — Adapter `Creer.razor`**

Trouver l'appel `TeamService.GetTypesEquipesAsync(...)` dans `Creer.razor` (`grep -n "GetTypesEquipes" src/BolDeSangManager/Components/Pages/Equipes/Creer.razor`).

Remplacer par :
```csharp
_teamTypes = await TeamService.GetTypesEquipesParVersionAsync(_ligue.RulesVersionId);
```

- [ ] **Step 9.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 9.4 — Commit**

```bash
git add src/BolDeSangManager/Services/TeamService.cs src/BolDeSangManager/Components/Pages/Equipes/Creer.razor
git commit -m "feat: filtrer TeamTypes par RulesVersion de la ligue dans Creer.razor"
```

---

## Task 10 — `DataEditService` : squelette + DI

**Files:**
- Create: `src/BolDeSangManager/Services/DataEditService.cs`
- Modify: `src/BolDeSangManager/Program.cs`

- [ ] **Step 10.1 — Créer le squelette**

```csharp
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
                    RoleNom = pos.RoleNom,
                    RoleQuantiteMax = pos.RoleQuantiteMax,
                    Cout = pos.Cout,
                    Mouvement = pos.Mouvement,
                    Force = pos.Force,
                    Agilite = pos.Agilite,
                    CapacitePasse = pos.CapacitePasse,
                    Armure = pos.Armure,
                    CompetencesPrincipales = pos.CompetencesPrincipales,
                    CompetencesSecondaires = pos.CompetencesSecondaires,
                    EstGrosBras = pos.EstGrosBras,
                    DescriptionRole = pos.DescriptionRole,
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
        p.RoleNom = data.RoleNom;
        p.RoleQuantiteMax = data.RoleQuantiteMax;
        p.Cout = data.Cout;
        p.Mouvement = data.Mouvement;
        p.Force = data.Force;
        p.Agilite = data.Agilite;
        p.CapacitePasse = data.CapacitePasse;
        p.Armure = data.Armure;
        p.CompetencesPrincipales = data.CompetencesPrincipales;
        p.CompetencesSecondaires = data.CompetencesSecondaires;
        p.EstGrosBras = data.EstGrosBras;
        p.DescriptionRole = data.DescriptionRole;
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
```

- [ ] **Step 10.2 — Enregistrer en DI**

Dans `Program.cs`, ajouter (à côté des autres `AddScoped`) :

```csharp
builder.Services.AddScoped<DataEditService>();
```

- [ ] **Step 10.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 10.4 — Commit**

```bash
git add src/BolDeSangManager/Services/DataEditService.cs src/BolDeSangManager/Program.cs
git commit -m "feat: DataEditService — CRUD validé pour données de jeu"
```

---

## Task 11 — Page `/admin/donnees` (layout + sélecteurs + onglet Équipes)

**Files:**
- Create: `src/BolDeSangManager/Components/Pages/Admin/Donnees.razor`
- Modify: `src/BolDeSangManager/Components/Layout/NavMenu.razor` (lien menu)

- [ ] **Step 11.1 — Créer `Donnees.razor`**

```razor
@page "/admin/donnees"
@attribute [Authorize(Roles = "Admin,GrandCommissaire")]
@using BolDeSangManager.Data
@using BolDeSangManager.Data.Models
@using BolDeSangManager.Data.Enums
@using BolDeSangManager.Services
@inject DataEditService DataEditService
@inject LeagueService LeagueService
@inject NavigationManager Nav
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<PageTitle>Édition des données</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Édition des données</MudText>

    <MudStack Row="true" Spacing="2" Class="mb-4">
        <MudSelect T="int" Label="Jeu" Value="_selectedGameId" ValueChanged="@OnGameChanged"
                   Variant="Variant.Outlined" Margin="Margin.Dense" Style="min-width:200px">
            @foreach (var g in _games)
            {
                <MudSelectItem Value="@g.Id">@g.Nom</MudSelectItem>
            }
        </MudSelect>
        <MudSelect T="int" Label="Version de règles" Value="_selectedVersionId" ValueChanged="@OnVersionChanged"
                   Variant="Variant.Outlined" Margin="Margin.Dense" Style="min-width:200px">
            @foreach (var v in _versions)
            {
                <MudSelectItem Value="@v.Id">@v.Nom @(v.EstActive ? "(active)" : "")</MudSelectItem>
            }
        </MudSelect>
    </MudStack>

    <MudTabs Elevation="1" Rounded="true" PanelClass="pa-4">
        <MudTabPanel Text="Équipes">
            <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                <MudText Typo="Typo.subtitle1"><b>@_teamTypes.Count équipe(s)</b></MudText>
                <MudButton StartIcon="@Icons.Material.Filled.Add" Color="Color.Primary" Variant="Variant.Filled"
                           OnClick="AjouterTeamType">Ajouter une équipe</MudButton>
            </MudStack>
            <MudTable Items="@_teamTypes" Dense="true" Hover="true">
                <HeaderContent>
                    <MudTh>Nom</MudTh>
                    <MudTh>Catégorie</MudTh>
                    <MudTh>Coût relance</MudTh>
                    <MudTh>Nb postes</MudTh>
                    <MudTh>Actions</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Nom</MudTd>
                    <MudTd>@context.Categorie</MudTd>
                    <MudTd>@context.CoutRelance.ToString("N0") po</MudTd>
                    <MudTd>@(context.Postes?.Count ?? 0)</MudTd>
                    <MudTd>
                        <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                                       OnClick="@(() => EditerTeamType(context.Id))" />
                        <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                                       OnClick="@(() => SupprimerTeamType(context))" />
                    </MudTd>
                </RowTemplate>
            </MudTable>
        </MudTabPanel>

        <MudTabPanel Text="Compétences">
            <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                    <MudText Typo="Typo.subtitle1"><b>@_skillsFiltres.Count compétence(s)</b></MudText>
                    <MudSelect T="SkillCategory?" Label="Catégorie" Value="_categorieFiltre" ValueChanged="@OnCategorieChanged"
                               Variant="Variant.Outlined" Margin="Margin.Dense" Style="min-width:150px" Clearable="true">
                        @foreach (var c in Enum.GetValues<SkillCategory>())
                        {
                            <MudSelectItem T="SkillCategory?" Value="@((SkillCategory?)c)">@c</MudSelectItem>
                        }
                    </MudSelect>
                </MudStack>
                <MudButton StartIcon="@Icons.Material.Filled.Add" Color="Color.Primary" Variant="Variant.Filled"
                           OnClick="AjouterSkill">Ajouter une compétence</MudButton>
            </MudStack>
            <MudTable Items="@_skillsFiltres" Dense="true" Hover="true">
                <HeaderContent>
                    <MudTh>Nom</MudTh>
                    <MudTh>Catégorie</MudTh>
                    <MudTh>Trait/Élite</MudTh>
                    <MudTh>Description</MudTh>
                    <MudTh>Actions</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Nom</MudTd>
                    <MudTd>@context.Categorie</MudTd>
                    <MudTd>
                        @if (context.EstElite) { <MudChip T="string" Size="Size.Small" Color="Color.Warning">Élite</MudChip> }
                        @if (context.EstTrait) { <MudChip T="string" Size="Size.Small" Color="Color.Info">Trait</MudChip> }
                    </MudTd>
                    <MudTd Style="max-width:400px; white-space:normal">@context.Description</MudTd>
                    <MudTd>
                        <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                                       OnClick="@(() => EditerSkill(context))" />
                        <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                                       OnClick="@(() => SupprimerSkill(context))" />
                    </MudTd>
                </RowTemplate>
            </MudTable>
        </MudTabPanel>

        <MudTabPanel Text="Versions">
            <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                <MudText Typo="Typo.subtitle1"><b>@_versions.Count version(s)</b></MudText>
                <MudButton StartIcon="@Icons.Material.Filled.Add" Color="Color.Primary" Variant="Variant.Filled"
                           OnClick="CreerVersion">Créer une nouvelle version</MudButton>
            </MudStack>
            <MudTable Items="@_versions" Dense="true">
                <HeaderContent>
                    <MudTh>Nom</MudTh>
                    <MudTh>Ordre</MudTh>
                    <MudTh>Active</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Nom</MudTd>
                    <MudTd>@context.Ordre</MudTd>
                    <MudTd>@(context.EstActive ? "Oui" : "Non")</MudTd>
                </RowTemplate>
            </MudTable>
        </MudTabPanel>
    </MudTabs>
</MudContainer>

@code {
    List<Game> _games = [];
    List<RulesVersion> _versions = [];
    List<TeamType> _teamTypes = [];
    List<Skill> _skills = [];
    int _selectedGameId;
    int _selectedVersionId;
    SkillCategory? _categorieFiltre;

    List<Skill> _skillsFiltres =>
        _categorieFiltre is null ? _skills : _skills.Where(s => s.Categorie == _categorieFiltre).ToList();

    protected override async Task OnInitializedAsync()
    {
        _games = await LeagueService.GetGamesAsync();
        if (_games.Any())
        {
            _selectedGameId = _games[0].Id;
            await ChargerVersionsAsync();
        }
    }

    async Task ChargerVersionsAsync()
    {
        _versions = await DataEditService.GetVersionsAsync(_selectedGameId);
        _selectedVersionId = _versions.FirstOrDefault(v => v.EstActive)?.Id ?? _versions.FirstOrDefault()?.Id ?? 0;
        await ChargerDonneesAsync();
    }

    async Task ChargerDonneesAsync()
    {
        if (_selectedVersionId == 0) { _teamTypes = []; _skills = []; return; }
        _teamTypes = await DataEditService.GetTeamTypesAsync(_selectedVersionId);
        _skills = await DataEditService.GetSkillsAsync(_selectedVersionId);
    }

    async Task OnGameChanged(int id)
    {
        _selectedGameId = id;
        await ChargerVersionsAsync();
    }

    async Task OnVersionChanged(int id)
    {
        _selectedVersionId = id;
        await ChargerDonneesAsync();
    }

    void OnCategorieChanged(SkillCategory? c) => _categorieFiltre = c;

    void AjouterTeamType()
    {
        Nav.NavigateTo($"/admin/donnees/equipes/new?versionId={_selectedVersionId}");
    }

    void EditerTeamType(int id)
    {
        Nav.NavigateTo($"/admin/donnees/equipes/{id}");
    }

    async Task SupprimerTeamType(TeamType t)
    {
        var ok = await DialogService.ShowMessageBox("Confirmer", $"Supprimer définitivement « {t.Nom} » ?", "Supprimer", "Annuler");
        if (ok != true) return;
        try
        {
            await DataEditService.SupprimerTeamTypeAsync(t.Id);
            Snackbar.Add("Équipe supprimée", Severity.Success);
            await ChargerDonneesAsync();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    async Task AjouterSkill()
    {
        var parameters = new DialogParameters<EditionSkillDialog> { { x => x.VersionId, _selectedVersionId } };
        var dialog = await DialogService.ShowAsync<EditionSkillDialog>("Nouvelle compétence", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await ChargerDonneesAsync();
    }

    async Task EditerSkill(Skill s)
    {
        var parameters = new DialogParameters<EditionSkillDialog>
        {
            { x => x.VersionId, _selectedVersionId },
            { x => x.Existant, s }
        };
        var dialog = await DialogService.ShowAsync<EditionSkillDialog>("Éditer compétence", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await ChargerDonneesAsync();
    }

    async Task SupprimerSkill(Skill s)
    {
        var ok = await DialogService.ShowMessageBox("Confirmer", $"Supprimer la compétence « {s.Nom} » ?", "Supprimer", "Annuler");
        if (ok != true) return;
        try
        {
            await DataEditService.SupprimerSkillAsync(s.Id);
            Snackbar.Add("Compétence supprimée", Severity.Success);
            await ChargerDonneesAsync();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    async Task CreerVersion()
    {
        var parameters = new DialogParameters<CreerVersionDialog>
        {
            { x => x.GameId, _selectedGameId },
            { x => x.VersionsDispo, _versions }
        };
        var dialog = await DialogService.ShowAsync<CreerVersionDialog>("Créer une nouvelle version", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await ChargerVersionsAsync();
    }
}
```

- [ ] **Step 11.2 — Lien menu**

Dans `src/BolDeSangManager/Components/Layout/NavMenu.razor`, après les autres entrées de menu Admin+GC (cherche grep `AuthorizeView Roles="Admin"` ou un menu existant similaire), ajouter :

```razor
<AuthorizeView Roles="Admin,GrandCommissaire">
    <Authorized>
        <MudNavLink Href="/admin/donnees" Icon="@Icons.Material.Filled.DataObject">
            Édition des données
        </MudNavLink>
    </Authorized>
</AuthorizeView>
```

Place this near the existing admin menu items.

- [ ] **Step 11.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors. Le build peut signaler `EditionSkillDialog` et `CreerVersionDialog` introuvables — ces composants seront créés en Tasks 12-14. Pour passer le build, créer des **stubs minimum** :

Create `src/BolDeSangManager/Components/Pages/Admin/EditionSkillDialog.razor` with just :
```razor
<MudDialog>
    <DialogContent><MudText>TODO</MudText></DialogContent>
</MudDialog>
@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int VersionId { get; set; }
    [Parameter] public BolDeSangManager.Data.Models.Skill? Existant { get; set; }
}
```

Create `src/BolDeSangManager/Components/Pages/Admin/CreerVersionDialog.razor` with :
```razor
<MudDialog>
    <DialogContent><MudText>TODO</MudText></DialogContent>
</MudDialog>
@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int GameId { get; set; }
    [Parameter] public List<BolDeSangManager.Data.Models.RulesVersion> VersionsDispo { get; set; } = [];
}
```

These stubs make the build pass — they get full content in Tasks 13-14.

- [ ] **Step 11.4 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Admin/Donnees.razor src/BolDeSangManager/Components/Pages/Admin/EditionSkillDialog.razor src/BolDeSangManager/Components/Pages/Admin/CreerVersionDialog.razor src/BolDeSangManager/Components/Layout/NavMenu.razor
git commit -m "feat: page /admin/donnees avec 3 onglets + lien menu (dialogues en stub)"
```

---

## Task 12 — Page édition TeamType

**Files:**
- Create: `src/BolDeSangManager/Components/Pages/Admin/EditionEquipe.razor`
- Create: `src/BolDeSangManager/Components/Pages/Admin/EditionPosteDialog.razor`

- [ ] **Step 12.1 — Créer `EditionEquipe.razor`**

```razor
@page "/admin/donnees/equipes/{IdParam}"
@attribute [Authorize(Roles = "Admin,GrandCommissaire")]
@using BolDeSangManager.Data.Models
@using BolDeSangManager.Data.Enums
@using BolDeSangManager.Services
@inject DataEditService DataEditService
@inject NavigationManager Nav
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<PageTitle>Édition équipe</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-3">
        <MudText Typo="Typo.h4">@(IsNew ? "Nouvelle équipe" : "Édition équipe")</MudText>
        <MudButton OnClick="Retour">Retour</MudButton>
    </MudStack>

    @if (_team is null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudPaper Class="pa-4 mb-3" Elevation="1">
            <MudGrid>
                <MudItem xs="12" sm="6"><MudTextField @bind-Value="_team.Nom" Label="Nom" Variant="Variant.Outlined" /></MudItem>
                <MudItem xs="6" sm="3">
                    <MudSelect T="TeamCategory" @bind-Value="_team.Categorie" Label="Catégorie" Variant="Variant.Outlined">
                        @foreach (var c in Enum.GetValues<TeamCategory>())
                        {
                            <MudSelectItem Value="@c">@c</MudSelectItem>
                        }
                    </MudSelect>
                </MudItem>
                <MudItem xs="6" sm="3"><MudNumericField @bind-Value="_team.CoutRelance" Label="Coût relance" Variant="Variant.Outlined" /></MudItem>
                <MudItem xs="12"><MudTextField @bind-Value="_team.ReglesSpeciales" Label="Règles spéciales (texte libre)" Variant="Variant.Outlined" Lines="2" /></MudItem>
                <MudItem xs="12"><MudTextField @bind-Value="_team.ReglesSpecialesLigue" Label="Règles ligues thématiques (CSV)" Variant="Variant.Outlined" /></MudItem>
                <MudItem xs="12">
                    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SauvegarderEquipe">
                        Sauvegarder
                    </MudButton>
                </MudItem>
            </MudGrid>
        </MudPaper>

        @if (!IsNew)
        {
            <MudPaper Class="pa-4 mb-3" Elevation="1">
                <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                    <MudText Typo="Typo.h6">Postes</MudText>
                    <MudButton StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                               OnClick="AjouterPoste">Ajouter poste</MudButton>
                </MudStack>
                <MudTable Items="@_team.Postes" Dense="true" Hover="true">
                    <HeaderContent>
                        <MudTh>Nom</MudTh>
                        <MudTh>Quota</MudTh>
                        <MudTh>Coût</MudTh>
                        <MudTh>M/F/AG/CP/AR</MudTh>
                        <MudTh>Actions</MudTh>
                    </HeaderContent>
                    <RowTemplate>
                        <MudTd>@context.Nom</MudTd>
                        <MudTd>@context.QuantiteMax</MudTd>
                        <MudTd>@context.Cout.ToString("N0")</MudTd>
                        <MudTd>@context.Mouvement / @context.Force / @context.Agilite / @context.CapacitePasse / @context.Armure</MudTd>
                        <MudTd>
                            <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                                           OnClick="@(() => EditerPoste(context))" />
                            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                                           OnClick="@(() => SupprimerPoste(context))" />
                        </MudTd>
                    </RowTemplate>
                </MudTable>
            </MudPaper>

            <MudPaper Class="pa-4 mb-3" Elevation="1">
                <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mb-2">
                    <MudText Typo="Typo.h6">Limites par mot-clé</MudText>
                    <MudStack Row="true" Spacing="1">
                        <MudTextField @bind-Value="_nouveauMotCle" Label="Mot-clé" Variant="Variant.Outlined" Margin="Margin.Dense" />
                        <MudNumericField @bind-Value="_nouveauMax" Label="Max" Variant="Variant.Outlined" Margin="Margin.Dense" Style="width:100px" />
                        <MudButton OnClick="AjouterLimite" Color="Color.Primary">+ Ajouter</MudButton>
                    </MudStack>
                </MudStack>
                <MudTable Items="@_team.LimitesMotsCles" Dense="true">
                    <HeaderContent>
                        <MudTh>Mot-clé</MudTh>
                        <MudTh>Max</MudTh>
                        <MudTh></MudTh>
                    </HeaderContent>
                    <RowTemplate>
                        <MudTd>@context.MotCle</MudTd>
                        <MudTd>@context.Max</MudTd>
                        <MudTd><MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error" OnClick="@(() => SupprimerLimite(context.Id))" /></MudTd>
                    </RowTemplate>
                </MudTable>
            </MudPaper>
        }
    }
</MudContainer>

@code {
    [Parameter] public string IdParam { get; set; } = "";
    [SupplyParameterFromQuery] public int VersionId { get; set; }

    bool IsNew => IdParam == "new";
    TeamType? _team;
    string _nouveauMotCle = "";
    int _nouveauMax = 1;

    protected override async Task OnInitializedAsync() => await ChargerAsync();

    async Task ChargerAsync()
    {
        if (IsNew)
        {
            _team = new TeamType { Nom = "", Categorie = TeamCategory.Specialist, CoutRelance = 50_000 };
        }
        else if (int.TryParse(IdParam, out var id))
        {
            _team = await DataEditService.GetTeamTypeAsync(id);
            if (_team is null) { Nav.NavigateTo("/admin/donnees"); return; }
        }
    }

    async Task SauvegarderEquipe()
    {
        if (_team is null) return;
        try
        {
            if (IsNew)
            {
                var created = await DataEditService.CreerTeamTypeAsync(VersionId, _team);
                Snackbar.Add("Équipe créée", Severity.Success);
                Nav.NavigateTo($"/admin/donnees/equipes/{created.Id}", forceLoad: true);
            }
            else
            {
                await DataEditService.ModifierTeamTypeAsync(_team.Id, _team.Nom, _team.Categorie, _team.CoutRelance, _team.ReglesSpeciales, _team.ReglesSpecialesLigue);
                Snackbar.Add("Équipe modifiée", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
    }

    async Task AjouterPoste()
    {
        if (_team is null) return;
        var parameters = new DialogParameters<EditionPosteDialog>
        {
            { x => x.TeamTypeId, _team.Id },
            { x => x.VersionId, _team.RulesVersionId },
            { x => x.Existant, null }
        };
        var dialog = await DialogService.ShowAsync<EditionPosteDialog>("Nouveau poste", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await ChargerAsync();
    }

    async Task EditerPoste(PlayerPosition p)
    {
        if (_team is null) return;
        var parameters = new DialogParameters<EditionPosteDialog>
        {
            { x => x.TeamTypeId, _team.Id },
            { x => x.VersionId, _team.RulesVersionId },
            { x => x.Existant, p }
        };
        var dialog = await DialogService.ShowAsync<EditionPosteDialog>("Éditer poste", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await ChargerAsync();
    }

    async Task SupprimerPoste(PlayerPosition p)
    {
        var ok = await DialogService.ShowMessageBox("Confirmer", $"Supprimer « {p.Nom} » ?", "Supprimer", "Annuler");
        if (ok != true) return;
        try
        {
            await DataEditService.SupprimerPosteAsync(p.Id);
            Snackbar.Add("Poste supprimé", Severity.Success);
            await ChargerAsync();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    async Task AjouterLimite()
    {
        if (_team is null || string.IsNullOrWhiteSpace(_nouveauMotCle)) return;
        await DataEditService.AjouterLimiteAsync(_team.Id, _nouveauMotCle, _nouveauMax);
        _nouveauMotCle = "";
        _nouveauMax = 1;
        await ChargerAsync();
    }

    async Task SupprimerLimite(int id)
    {
        await DataEditService.SupprimerLimiteAsync(id);
        await ChargerAsync();
    }

    void Retour() => Nav.NavigateTo("/admin/donnees");
}
```

- [ ] **Step 12.2 — Créer `EditionPosteDialog.razor`**

```razor
@using BolDeSangManager.Data.Models
@using BolDeSangManager.Services
@inject DataEditService DataEditService
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudGrid>
            <MudItem xs="12" sm="6"><MudTextField @bind-Value="_data.Nom" Label="Nom" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="6" sm="3"><MudNumericField @bind-Value="_data.QuantiteMax" Label="Quota" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="6" sm="3"><MudNumericField @bind-Value="_data.Cout" Label="Coût" Variant="Variant.Outlined" /></MudItem>

            <MudItem xs="6" sm="3"><MudTextField @bind-Value="_data.RoleNom" Label="Rôle (optionnel)" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="6" sm="3"><MudNumericField @bind-Value="_data.RoleQuantiteMax" Label="Quota rôle" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="6" sm="3"><MudCheckBox @bind-Value="_data.EstGrosBras" Label="Gros Bras" /></MudItem>

            <MudItem xs="12"><MudText Typo="Typo.subtitle2">Caractéristiques</MudText></MudItem>
            <MudItem xs="4" sm="2"><MudNumericField @bind-Value="_data.Mouvement" Label="MOV" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="4" sm="2"><MudNumericField @bind-Value="_data.Force" Label="FOR" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="4" sm="2"><MudTextField @bind-Value="_data.Agilite" Label="AGI" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="4" sm="2"><MudTextField @bind-Value="_data.CapacitePasse" Label="CP" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="4" sm="2"><MudTextField @bind-Value="_data.Armure" Label="ARM" Variant="Variant.Outlined" /></MudItem>

            <MudItem xs="12" sm="6"><MudTextField @bind-Value="_data.CompetencesPrincipales" Label="Accès Principal (ex: GAF)" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="12" sm="6"><MudTextField @bind-Value="_data.CompetencesSecondaires" Label="Accès Secondaire (ex: ASF)" Variant="Variant.Outlined" /></MudItem>

            <MudItem xs="12"><MudTextField @bind-Value="_data.MotsCles" Label="Mots-clés (CSV: Trois-quart,Humain,...)" Variant="Variant.Outlined" /></MudItem>

            <MudItem xs="12">
                <MudText Typo="Typo.subtitle2">Compétences de départ</MudText>
                <MudSelect T="int" MultiSelection="true" SelectAll="false"
                           SelectedValues="_skillsSelectionnes" SelectedValuesChanged="@(v => _skillsSelectionnes = v.ToHashSet())"
                           Label="Skills" Variant="Variant.Outlined" Margin="Margin.Dense">
                    @foreach (var s in _skillsDispo)
                    {
                        <MudSelectItem T="int" Value="@s.Id">@s.Nom (@s.Categorie)</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>
        </MudGrid>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Annuler">Annuler</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Sauvegarder">Sauvegarder</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int TeamTypeId { get; set; }
    [Parameter] public int VersionId { get; set; }
    [Parameter] public PlayerPosition? Existant { get; set; }

    PlayerPosition _data = new() { Nom = "", QuantiteMax = 1, Cout = 50_000, Mouvement = 6, Force = 3, Agilite = "3+", CapacitePasse = "-", Armure = "9+" };
    HashSet<int> _skillsSelectionnes = new();
    List<Skill> _skillsDispo = [];

    protected override async Task OnInitializedAsync()
    {
        _skillsDispo = await DataEditService.GetSkillsAsync(VersionId);
        if (Existant is not null)
        {
            _data = new PlayerPosition
            {
                Id = Existant.Id,
                Nom = Existant.Nom,
                QuantiteMax = Existant.QuantiteMax,
                RoleNom = Existant.RoleNom,
                RoleQuantiteMax = Existant.RoleQuantiteMax,
                Cout = Existant.Cout,
                Mouvement = Existant.Mouvement,
                Force = Existant.Force,
                Agilite = Existant.Agilite,
                CapacitePasse = Existant.CapacitePasse,
                Armure = Existant.Armure,
                CompetencesPrincipales = Existant.CompetencesPrincipales,
                CompetencesSecondaires = Existant.CompetencesSecondaires,
                EstGrosBras = Existant.EstGrosBras,
                MotsCles = Existant.MotsCles,
                DescriptionRole = Existant.DescriptionRole
            };
            _skillsSelectionnes = Existant.CompetencesDepart.Select(c => c.SkillId).ToHashSet();
        }
    }

    void Annuler() => MudDialog.Cancel();

    async Task Sauvegarder()
    {
        try
        {
            if (Existant is null)
                await DataEditService.AjouterPosteAsync(TeamTypeId, _data, _skillsSelectionnes);
            else
                await DataEditService.ModifierPosteAsync(Existant.Id, _data, _skillsSelectionnes);
            Snackbar.Add("Poste sauvegardé", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
    }
}
```

- [ ] **Step 12.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 12.4 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Admin/EditionEquipe.razor src/BolDeSangManager/Components/Pages/Admin/EditionPosteDialog.razor
git commit -m "feat: page édition TeamType + modale édition PlayerPosition"
```

---

## Task 13 — Modale édition Skill (vrai contenu, remplace le stub)

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Admin/EditionSkillDialog.razor`

- [ ] **Step 13.1 — Remplacer le stub par le contenu complet**

Replace the file with :

```razor
@using BolDeSangManager.Data.Models
@using BolDeSangManager.Data.Enums
@using BolDeSangManager.Services
@inject DataEditService DataEditService
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudGrid>
            <MudItem xs="12" sm="8"><MudTextField @bind-Value="_data.Nom" Label="Nom" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="12" sm="4">
                <MudSelect T="SkillCategory" @bind-Value="_data.Categorie" Label="Catégorie" Variant="Variant.Outlined">
                    @foreach (var c in Enum.GetValues<SkillCategory>())
                    {
                        <MudSelectItem Value="@c">@c</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>
            <MudItem xs="12"><MudTextField @bind-Value="_data.Description" Label="Description" Lines="3" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="6"><MudCheckBox @bind-Value="_data.EstElite" Label="Élite (accessible à fort palier)" /></MudItem>
            <MudItem xs="6"><MudCheckBox @bind-Value="_data.EstTrait" Label="Trait (innée, non apprenable)" /></MudItem>
        </MudGrid>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Annuler">Annuler</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Sauvegarder">Sauvegarder</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int VersionId { get; set; }
    [Parameter] public Skill? Existant { get; set; }

    Skill _data = new() { Nom = "", Description = "", Categorie = SkillCategory.Generale };

    protected override void OnInitialized()
    {
        if (Existant is not null)
        {
            _data = new Skill
            {
                Id = Existant.Id,
                Nom = Existant.Nom,
                Categorie = Existant.Categorie,
                Description = Existant.Description,
                EstElite = Existant.EstElite,
                EstTrait = Existant.EstTrait
            };
        }
    }

    void Annuler() => MudDialog.Cancel();

    async Task Sauvegarder()
    {
        try
        {
            if (Existant is null)
                await DataEditService.CreerSkillAsync(VersionId, _data);
            else
                await DataEditService.ModifierSkillAsync(Existant.Id, _data.Nom, _data.Categorie, _data.Description, _data.EstElite, _data.EstTrait);
            Snackbar.Add("Compétence sauvegardée", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
    }
}
```

- [ ] **Step 13.2 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 13.3 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Admin/EditionSkillDialog.razor
git commit -m "feat: modale édition Skill (création + modification)"
```

---

## Task 14 — Modale création RulesVersion (avec clonage)

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Admin/CreerVersionDialog.razor`

- [ ] **Step 14.1 — Remplacer le stub par le contenu complet**

Replace file with :

```razor
@using BolDeSangManager.Data.Models
@using BolDeSangManager.Services
@inject DataEditService DataEditService
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudGrid>
            <MudItem xs="12" sm="8"><MudTextField @bind-Value="_nom" Label="Nom (ex: Saison 4)" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="12" sm="4"><MudNumericField @bind-Value="_ordre" Label="Ordre" Variant="Variant.Outlined" /></MudItem>
            <MudItem xs="12"><MudCheckBox @bind-Value="_estActive" Label="Définir comme version active (désactive l'actuelle)" /></MudItem>
            <MudItem xs="12">
                <MudSelect T="int?" @bind-Value="_cloneFromId" Label="Cloner depuis (optionnel)" Variant="Variant.Outlined" Clearable="true">
                    <MudSelectItem T="int?" Value="@((int?)null)">Vide (pas de clonage)</MudSelectItem>
                    @foreach (var v in VersionsDispo)
                    {
                        <MudSelectItem T="int?" Value="@((int?)v.Id)">@v.Nom @(v.EstActive ? "(active)" : "")</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>
            <MudItem xs="12">
                <MudText Typo="Typo.caption" Style="color:#9e9e9e">
                    Le clonage copie tous les TeamTypes, postes, compétences et limites de la version source. Opération transactionnelle.
                </MudText>
            </MudItem>
        </MudGrid>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Annuler">Annuler</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   OnClick="Valider" Disabled="@(string.IsNullOrWhiteSpace(_nom) || _saving)">
            @(_saving ? "Création…" : "Créer")
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int GameId { get; set; }
    [Parameter] public List<RulesVersion> VersionsDispo { get; set; } = [];

    string _nom = "";
    int _ordre = 1;
    bool _estActive = false;
    int? _cloneFromId;
    bool _saving;

    protected override void OnInitialized()
    {
        _ordre = (VersionsDispo.Any() ? VersionsDispo.Max(v => v.Ordre) : 0) + 1;
        _cloneFromId = VersionsDispo.FirstOrDefault(v => v.EstActive)?.Id;
    }

    void Annuler() => MudDialog.Cancel();

    async Task Valider()
    {
        _saving = true;
        try
        {
            await DataEditService.CreerVersionAsync(GameId, _nom, _ordre, _estActive, _cloneFromId);
            Snackbar.Add("Version créée", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }
}
```

- [ ] **Step 14.2 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 14.3 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Admin/CreerVersionDialog.razor
git commit -m "feat: modale création RulesVersion avec clonage transactionnel"
```

---

## Task 15 — Smoke test + mémoire

**Files:**
- None (verification only) + mémoire hors repo

- [ ] **Step 15.1 — Build complet**

Run : `dotnet build`
Expected : 0 errors.

- [ ] **Step 15.2 — Tests existants**

Run : `dotnet test`
Expected : 127 tests pass (les changements de modèle peuvent faire ajuster `DataSeeder.SeedGameAsync` côté tests si elle référence `Skill.GameSpecifique` — corriger si besoin).

Si tests cassent : adapter `tests/BolDeSangManager.Tests/Helpers/DataSeeder.cs` pour utiliser la nouvelle structure. Notamment si elle crée des Skills, ajouter `RulesVersionId = (await db.RulesVersions.FirstAsync()).Id` ou similaire.

- [ ] **Step 15.3 — Smoke test app**

```bash
rm -f src/BolDeSangManager/Data/boldesang.db
cd src/BolDeSangManager && timeout 30 dotnet run --no-launch-profile 2>&1 | head -60 ; cd ../..
```

Vérifier :
- Migrations appliquées
- Seed runs OK (no exceptions)
- App listens

Navigation manuelle suggérée (sans automatiser) :
- Connexion admin
- Aller à `/admin/donnees`
- Sélectionner Blood Bowl + Saison 3 → voir les 30 équipes
- Cliquer une équipe → voir édition
- Cliquer sur un poste → modale d'édition
- Aller à l'onglet "Versions" → tester créer une nouvelle version "Saison 4" en clonant Saison 3
- Vérifier après création que les données sont dupliquées

- [ ] **Step 15.4 — Mettre à jour la mémoire**

Créer `C:\Users\nide3\.claude\projects\C--Users-nide3-project-BolDeSangManager\memory\reference_data_edit.md` :

```markdown
---
name: reference-data-edit
description: Page /admin/donnees pour Admin/GC + versioning par RulesVersion (TeamType + Skill liés à une version)
metadata:
  type: reference
---

**Page d'édition** : `/admin/donnees` (Admin + GrandCommissaire). Onglets Équipes / Compétences / Versions.

**Versioning** :
- `TeamType.RulesVersionId` + `Skill.RulesVersionId` (FK) — chaque entité appartient à une version précise.
- Skills universels sont dupliqués entre versions actives (S3 BB + Edition 2022 DB) au seed.
- `Skill.GameSpecifique` supprimé — remplacé par RulesVersionId.

**Service** : `BolDeSangManager.Services.DataEditService` centralise les CRUD validés (TeamType, PlayerPosition, Skill, RulesVersion, TeamTypeKeywordLimit). Suppression bloquée si dépendances (Teams, TeamPlayers, TeamPlayerSkills, PlayerImprovements, PlayerPositionSkills).

**Clonage de version** (`CreerVersionAsync` avec `cloneFromVersionId`) : transactionnel, duplique TeamTypes + PlayerPositions + Skills + PlayerPositionSkills + LimitesMotsCles vers la nouvelle version.

**Composants UI** :
- `Components/Pages/Admin/Donnees.razor` — page principale
- `Components/Pages/Admin/EditionEquipe.razor` — édition TeamType + tableau postes + limites
- `Components/Pages/Admin/EditionPosteDialog.razor` — modale édition PlayerPosition
- `Components/Pages/Admin/EditionSkillDialog.razor` — modale édition Skill
- `Components/Pages/Admin/CreerVersionDialog.razor` — modale création RulesVersion

**Spec** : `docs/superpowers/specs/2026-05-19-data-edit-versioning-design.md`
**Plan exécuté** : `docs/superpowers/plans/2026-05-19-data-edit-versioning.md`

**Tests** : non écrits dans l'itération initiale (décision utilisateur). À ajouter ultérieurement sur `DataEditService` (CRUD + suppression bloquée + clonage).
```

Update `MEMORY.md` to add :
```
- [Édition de données + versioning](reference_data_edit.md) — page /admin/donnees ; TeamType/Skill liés à RulesVersion ; service DataEditService
```

## Récapitulatif

**15 tâches** :
1-2. Modèle (RulesVersionId sur TeamType + Skill, suppression GameSpecifique)
3. DbContext FK
4-6. Adapter SkillSeedData + Team seeds + DbSeeder
7. Migration EF
8-9. Adapter LeagueExportService + Equipes/Creer
10. DataEditService (CRUD complet)
11. Page /admin/donnees (layout + sélecteurs + onglet Équipes)
12. Page édition TeamType + modale poste
13. Modale skill
14. Modale création version (avec clonage)
15. Smoke test + mémoire

**Couverture spec** :
- §3 Modèle → Tasks 1, 2
- §3.3 Migration EF → Task 7
- §4 Page principale → Task 11
- §4.7 Modale création version → Task 14
- §5 Édition TeamType → Task 12
- §6 Édition Skill → Task 13
- §7 Service → Task 10
- §8 Clonage → Task 10 (`ClonerVersionAsync`)
- §9 Tests → REPORTÉ (par directive utilisateur)
- §10 Plan migration → ordre des tâches

**Hors-scope confirmé** :
- Tests d'intégration (à faire en itération séparée)
- Export/import JSON d'une version (mentionné dans §11 spec)
- Audit log
- Édition Star Players / Coups de Pouce
