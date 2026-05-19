# Refonte de la base BolDeSangManager : alignement BB S3 / Dungeon Bowl — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aligner le modèle de données et la logique métier sur les règles Blood Bowl LRB S3 + Dungeon Bowl Edition 2022, avec phase de repos pré-playoffs, paliers PSP corrects, classements et awards.

**Architecture:** Évolution du data layer EF Core (nouvelles entités, enums refactorés), refonte du DbSeeder en fichiers modulaires, refactor de 3 services (`MatchService`, `TeamService`, `LeagueService`). Reset complet de la DB SQLite (dev). Migration EF unique consolidée.

**Tech Stack:** .NET 9, EF Core 9 (SQLite), Blazor Server, xUnit (SQLite in-memory pour tests), Identity ASP.NET Core.

**Spec de référence:** `docs/superpowers/specs/2026-05-18-refonte-base-bb-db-design.md`

**Données source:** `docs/regles/bloodbowl.md` (LRB S3, 30 équipes, ~115 skills) et `docs/regles/dungeonbowl.md` (8 collèges, 4 skills spécifiques)

---

## Structure des fichiers

### À créer

| Fichier | Responsabilité |
|---|---|
| `src/BolDeSangManager/Data/Models/PlayerImprovement.cs` | Entité : améliorations gagnées par un joueur (palier, type, skill/stat) |
| `src/BolDeSangManager/Data/Models/PhaseDeReposValidation.cs` | Entité : trace une équipe ayant validé son après-match de repos |
| `src/BolDeSangManager/Data/Models/LeagueAward.cs` | Entité : récompense attribuée en fin de ligue (MVP, Champion, etc.) |
| `src/BolDeSangManager/Data/Seeding/ImprovementThresholds.cs` | Constantes des paliers PSP + hausses de valeur par type |
| `src/BolDeSangManager/Data/Seeding/SkillSeedData.cs` | Liste des ~115 skills à seeder (LRB S3 + DB spécifiques) |
| `src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs` | 30 équipes BB avec rosters |
| `src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs` | 8 collèges DB avec rosters |

### À modifier

| Fichier | Changement |
|---|---|
| `src/BolDeSangManager/Data/Enums/Enums.cs` | + `LeagueStatus.PhaseDeRepos`, + `TeamCategory`, + `ImprovementType`, + `AwardType` |
| `src/BolDeSangManager/Data/Models/TeamType.cs` | `Categorie` int → `TeamCategory`, + `ReglesSpecialesLigue` |
| `src/BolDeSangManager/Data/Models/Skill.cs` | + `GameSpecifique : GameType?` |
| `src/BolDeSangManager/Data/Models/Team.cs` (entité `TeamPlayer`) | - `NombreAmeliorations`, + collection `Improvements` |
| `src/BolDeSangManager/Data/Models/League.cs` | + collections `ValidationsRepos` et `Awards` |
| `src/BolDeSangManager/Data/ApplicationDbContext.cs` | + 3 DbSets + config FK |
| `src/BolDeSangManager/Data/DbSeeder.cs` | Devient orchestrateur léger, délègue aux fichiers `Seeding/` |
| `src/BolDeSangManager/Services/MatchService.cs` | `CalculerPSP` paramétré `GameType` (TD=5 en DB) |
| `src/BolDeSangManager/Services/TeamService.cs` | Remplacer `AttributerCompetenceAsync` par `AppliquerAmeliorationAsync` |
| `src/BolDeSangManager/Services/LeagueService.cs` | + `LancerPhaseDeReposAsync`, + `GetTop*Async`, + `AttribuerAwardAsync` |

### À supprimer

| Fichier |
|---|
| `src/BolDeSangManager/Data/Migrations/20260512025752_InitialCreate.cs` (+ Designer) |
| `src/BolDeSangManager/Data/Migrations/20260514095746_AddAppConfig.cs` (+ Designer) |
| `src/BolDeSangManager/Data/Migrations/20260515221954_AddPlayerPositionRole.cs` (+ Designer) |
| `src/BolDeSangManager/Data/Migrations/20260516000110_AddApresMatchValidation.cs` (+ Designer) |
| `src/BolDeSangManager/Data/Migrations/20260517090101_FixDungeonBowlRoleLimits.cs` (+ Designer) |
| `src/BolDeSangManager/Data/Migrations/ApplicationDbContextModelSnapshot.cs` |
| `src/BolDeSangManager/Data/boldesang.db` |

### Tests touchés

Pattern existant : 1 fichier `*Tests.cs` par service, `TestDbFactory` (SQLite `:memory:`) + `DataSeeder` helper.

| Fichier | Changement |
|---|---|
| `tests/BolDeSangManager.Tests/MatchServiceTests.cs` | + tests CalculerPSP par jeu |
| `tests/BolDeSangManager.Tests/TeamServiceTests.cs` | + tests AppliquerAmeliorationAsync |
| `tests/BolDeSangManager.Tests/LeagueServiceTests.cs` | + tests phase de repos + classements + awards |
| `tests/BolDeSangManager.Tests/DbSeederTests.cs` | NOUVEAU : vérifie comptes skills/équipes |
| `tests/BolDeSangManager.Tests/Helpers/DataSeeder.cs` | Mettre à jour pour utiliser `TeamCategory` enum |

---

## Conventions

- Commits atomiques par tâche, message en français impératif (style existant du repo).
- Co-Authored-By : ne pas l'ajouter (le repo ne l'utilise pas).
- TDD strict pour la logique (Tâches 10-14). Pour les fichiers de données (Tâches 6-8), les tests sont "vérifications de quantité + entrées clés".
- À la fin de chaque tâche : `dotnet build` doit passer, `dotnet test` ne doit pas régresser.

---

## Task 1 : Mettre à jour les enums

**Files:**
- Modify: `src/BolDeSangManager/Data/Enums/Enums.cs`

- [ ] **Step 1.1 : Ajouter `PhaseDeRepos` à `LeagueStatus`**

Remplacer le bloc `LeagueStatus` par :

```csharp
public enum LeagueStatus
{
    Creation,        // Le commissaire configure la ligue
    Inscription,     // Les coaches rejoignent et créent leurs équipes
    EnCours,         // Saison régulière en cours
    PhaseDeRepos,    // Entre saison régulière et playoffs : reset RPM + après-match
    PlayOffs,        // Phase de playoffs
    Termine          // Ligue terminée
}
```

- [ ] **Step 1.2 : Ajouter `TeamCategory`**

Ajouter à la fin du fichier :

```csharp
public enum TeamCategory
{
    Bashy,        // Nains, Orques Noirs, Khorne, Chaos Dwarfs, Nurgle…
    Staller,      // Élus du Chaos, Nordiques, Renégats du Chaos, Bretonniens…
    Agile,        // Elfes (tous), Skavens, Amazones, Hommes-lézards…
    Specialist    // Halflings, Snotlings, Ogres, Gobelins, Bas-fonds, Vampires…
}
```

- [ ] **Step 1.3 : Ajouter `ImprovementType`**

```csharp
public enum ImprovementType
{
    AleaPrimaire,            // Tirage D6/D6 sur catégorie primaire
    SelectionPrimaire,       // Choix dans la catégorie primaire
    AleaSecondaire,          // Tirage D6/D6 sur catégorie secondaire
    SelectionSecondaire,     // Choix dans la catégorie secondaire
    AmeliorationCarac,       // +1 M, AG ou CP
    AmeliorationForceArmure  // +1 F ou +1 AR
}
```

- [ ] **Step 1.4 : Ajouter `AwardType`**

```csharp
public enum AwardType
{
    Champion,            // Vainqueur de la ligue (rattaché à Team)
    MVP,                 // Meilleur joueur (rattaché à TeamPlayer)
    MeilleurMarqueur,    // Plus de TDs
    MeilleurDefenseur,   // Plus d'éliminations
    MeilleurPasseur,     // Plus de completions+interceptions
    MeilleurCoach        // Plus de victoires / points (rattaché à ApplicationUser)
}
```

- [ ] **Step 1.5 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : compilation OK (les enums sont seulement ajoutés, rien ne casse).

- [ ] **Step 1.6 : Commit**

```bash
git add src/BolDeSangManager/Data/Enums/Enums.cs
git commit -m "refactor: ajouter enums PhaseDeRepos, TeamCategory, ImprovementType, AwardType"
```

---

## Task 2 : Créer les nouvelles entités

**Files:**
- Create: `src/BolDeSangManager/Data/Models/PlayerImprovement.cs`
- Create: `src/BolDeSangManager/Data/Models/PhaseDeReposValidation.cs`
- Create: `src/BolDeSangManager/Data/Models/LeagueAward.cs`

- [ ] **Step 2.1 : Créer `PlayerImprovement.cs`**

```csharp
using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class PlayerImprovement
{
    public int Id { get; set; }
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;

    public int Palier { get; set; }              // 1..6 (correspond aux seuils 6/16/31/51/76/176 PSP)
    public ImprovementType Type { get; set; }

    // Skill acquise (si Type = AleaPrimaire/SelectionPrimaire/AleaSecondaire/SelectionSecondaire)
    public int? SkillId { get; set; }
    public Skill? Skill { get; set; }

    // Caractéristique améliorée (si Type = AmeliorationCarac ou AmeliorationForceArmure)
    public AffectedStat? StatAmelioree { get; set; }

    public int ValeurHausse { get; set; }        // kpo ajoutés à TeamPlayer.ValeurActuelle
    public DateTime AppliqueLe { get; set; } = DateTime.UtcNow;
    public bool EnAttenteValidation { get; set; } = false;

    // Traçabilité : null si appliqué pendant la phase de repos
    public int? MatchSheetId { get; set; }
}
```

- [ ] **Step 2.2 : Créer `PhaseDeReposValidation.cs`**

```csharp
namespace BolDeSangManager.Data.Models;

public class PhaseDeReposValidation
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTime ValideLe { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2.3 : Créer `LeagueAward.cs`**

```csharp
using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class LeagueAward
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public AwardType Type { get; set; }

    // Au moins une de ces FK est non-null selon le type d'award
    public int? TeamPlayerId { get; set; }
    public TeamPlayer? TeamPlayer { get; set; }
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
    public string? CoachId { get; set; }
    public ApplicationUser? Coach { get; set; }

    public DateTime AttribueLe { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2.4 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : compilation OK (entités isolées, pas encore liées au DbContext).

- [ ] **Step 2.5 : Commit**

```bash
git add src/BolDeSangManager/Data/Models/PlayerImprovement.cs src/BolDeSangManager/Data/Models/PhaseDeReposValidation.cs src/BolDeSangManager/Data/Models/LeagueAward.cs
git commit -m "feat: ajouter entités PlayerImprovement, PhaseDeReposValidation, LeagueAward"
```

---

## Task 3 : Modifier les entités existantes

**Files:**
- Modify: `src/BolDeSangManager/Data/Models/TeamType.cs`
- Modify: `src/BolDeSangManager/Data/Models/Skill.cs`
- Modify: `src/BolDeSangManager/Data/Models/Team.cs`
- Modify: `src/BolDeSangManager/Data/Models/League.cs`

- [ ] **Step 3.1 : `TeamType.cs` — remplacer `Categorie` int par `TeamCategory` enum + ajouter `ReglesSpecialesLigue`**

Remplacer le bloc :

```csharp
public int CoutRelance { get; set; } = 50000;
public int Categorie { get; set; } = 1; // Catégorie Jeu Égal (1-4)
```

par :

```csharp
public int CoutRelance { get; set; } = 50000;
public TeamCategory Categorie { get; set; } = TeamCategory.Specialist;

// CSV des règles spéciales d'éligibilité aux ligues thématiques.
// Ex: "OldWorldClassic,BadlandsBrawl". Vide = aucune règle spéciale.
public string ReglesSpecialesLigue { get; set; } = string.Empty;
```

Ajouter en haut du fichier (si pas déjà présent) :

```csharp
using BolDeSangManager.Data.Enums;
```

- [ ] **Step 3.2 : `Skill.cs` — ajouter `GameSpecifique`**

Après la ligne `public bool EstTrait { get; set; } = false;`, ajouter :

```csharp
// null = skill universel ; sinon limité à ce jeu (ex: skills DungeonBowl uniquement)
public GameType? GameSpecifique { get; set; }
```

- [ ] **Step 3.3 : `Team.cs` — modifier `TeamPlayer` (supprimer `NombreAmeliorations`, ajouter collection `Improvements`)**

Dans la classe `TeamPlayer`, remplacer la ligne :

```csharp
public int NombreAmeliorations { get; set; } = 0;
```

par :

```csharp
// Collection des améliorations de palier (voir PlayerImprovement).
// Le nombre de paliers consommés = Improvements.Count.
public ICollection<PlayerImprovement> Improvements { get; set; } = [];
```

- [ ] **Step 3.4 : `League.cs` — ajouter collections `ValidationsRepos` et `Awards`**

Dans la classe `League`, après `public ICollection<Team> Equipes { get; set; } = [];`, ajouter :

```csharp
public ICollection<PhaseDeReposValidation> ValidationsRepos { get; set; } = [];
public ICollection<LeagueAward> Awards { get; set; } = [];
```

- [ ] **Step 3.5 : Recherche des usages cassés**

Lister les fichiers qui référencent `NombreAmeliorations` ou comparent `Categorie` à un int :

Run :
```bash
grep -rn "NombreAmeliorations\|\.Categorie\s*[=<>]\s*[0-9]" src/ tests/
```

Pour chaque usage trouvé, ajuster :
- `TeamService.AttributerCompetenceAsync` : la ligne `joueur.NombreAmeliorations++;` sera **supprimée** dans la tâche 11 (le service entier est refactoré). Pour l'instant, remplacer temporairement par `// joueur.NombreAmeliorations++; // Migré en Task 11` afin que le build passe.
- Si une page Razor lit `j.NombreAmeliorations`, remplacer par `j.Improvements.Count`.
- Si un test compare `Categorie == 1`, remplacer par `Categorie == TeamCategory.Agile` (ou la valeur adaptée).

- [ ] **Step 3.6 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : compilation OK.

- [ ] **Step 3.7 : Commit**

```bash
git add -A
git commit -m "refactor: TeamCategory enum, GameSpecifique sur Skill, Improvements collection sur TeamPlayer, awards/repos sur League"
```

---

## Task 4 : Configurer DbContext

**Files:**
- Modify: `src/BolDeSangManager/Data/ApplicationDbContext.cs`

- [ ] **Step 4.1 : Ajouter les `DbSet`**

Après la ligne `public DbSet<AppConfig> AppConfigs => Set<AppConfig>();`, ajouter :

```csharp
public DbSet<PlayerImprovement> PlayerImprovements => Set<PlayerImprovement>();
public DbSet<PhaseDeReposValidation> PhaseDeReposValidations => Set<PhaseDeReposValidation>();
public DbSet<LeagueAward> LeagueAwards => Set<LeagueAward>();
```

- [ ] **Step 4.2 : Configurer les FK dans `OnModelCreating`**

À la fin de la méthode `OnModelCreating`, juste avant la fermeture de l'accolade, ajouter :

```csharp
// PlayerImprovement → TeamPlayer (cascade)
builder.Entity<PlayerImprovement>()
    .HasOne(pi => pi.TeamPlayer)
    .WithMany(tp => tp.Improvements)
    .HasForeignKey(pi => pi.TeamPlayerId)
    .OnDelete(DeleteBehavior.Cascade);

// PlayerImprovement → Skill (set null si skill supprimé)
builder.Entity<PlayerImprovement>()
    .HasOne(pi => pi.Skill)
    .WithMany()
    .HasForeignKey(pi => pi.SkillId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);

// PhaseDeReposValidation → League (cascade)
builder.Entity<PhaseDeReposValidation>()
    .HasOne(prv => prv.League)
    .WithMany(l => l.ValidationsRepos)
    .HasForeignKey(prv => prv.LeagueId)
    .OnDelete(DeleteBehavior.Cascade);

// PhaseDeReposValidation → Team (restrict pour éviter cascade circulaire)
builder.Entity<PhaseDeReposValidation>()
    .HasOne(prv => prv.Team)
    .WithMany()
    .HasForeignKey(prv => prv.TeamId)
    .OnDelete(DeleteBehavior.Restrict);

// LeagueAward → League (cascade)
builder.Entity<LeagueAward>()
    .HasOne(a => a.League)
    .WithMany(l => l.Awards)
    .HasForeignKey(a => a.LeagueId)
    .OnDelete(DeleteBehavior.Cascade);

// LeagueAward → TeamPlayer / Team / Coach (tous optionnels, set null)
builder.Entity<LeagueAward>()
    .HasOne(a => a.TeamPlayer)
    .WithMany()
    .HasForeignKey(a => a.TeamPlayerId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);

builder.Entity<LeagueAward>()
    .HasOne(a => a.Team)
    .WithMany()
    .HasForeignKey(a => a.TeamId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);

builder.Entity<LeagueAward>()
    .HasOne(a => a.Coach)
    .WithMany()
    .HasForeignKey(a => a.CoachId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);
```

- [ ] **Step 4.3 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : compilation OK.

- [ ] **Step 4.4 : Commit**

```bash
git add src/BolDeSangManager/Data/ApplicationDbContext.cs
git commit -m "feat: enregistrer DbSets et FK pour PlayerImprovement, PhaseDeReposValidation, LeagueAward"
```

---

## Task 5 : Supprimer migrations obsolètes + DB

**Files:**
- Delete: `src/BolDeSangManager/Data/Migrations/20260512025752_InitialCreate.cs` (+ Designer)
- Delete: `src/BolDeSangManager/Data/Migrations/20260514095746_AddAppConfig.cs` (+ Designer)
- Delete: `src/BolDeSangManager/Data/Migrations/20260515221954_AddPlayerPositionRole.cs` (+ Designer)
- Delete: `src/BolDeSangManager/Data/Migrations/20260516000110_AddApresMatchValidation.cs` (+ Designer)
- Delete: `src/BolDeSangManager/Data/Migrations/20260517090101_FixDungeonBowlRoleLimits.cs` (+ Designer)
- Delete: `src/BolDeSangManager/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- Delete: `src/BolDeSangManager/Data/boldesang.db`

- [ ] **Step 5.1 : Lister les migrations applicatives**

Run :
```bash
ls src/BolDeSangManager/Data/Migrations/
```
Expected : voir les 5 migrations applicatives (2026*) + Identity (`00000000000000_CreateIdentitySchema.cs` + Designer) + Snapshot.

- [ ] **Step 5.2 : Supprimer les migrations applicatives + snapshot + DB**

Run :
```bash
rm src/BolDeSangManager/Data/Migrations/20260512025752_InitialCreate.cs
rm src/BolDeSangManager/Data/Migrations/20260512025752_InitialCreate.Designer.cs
rm src/BolDeSangManager/Data/Migrations/20260514095746_AddAppConfig.cs
rm src/BolDeSangManager/Data/Migrations/20260514095746_AddAppConfig.Designer.cs
rm src/BolDeSangManager/Data/Migrations/20260515221954_AddPlayerPositionRole.cs
rm src/BolDeSangManager/Data/Migrations/20260515221954_AddPlayerPositionRole.Designer.cs
rm src/BolDeSangManager/Data/Migrations/20260516000110_AddApresMatchValidation.cs
rm src/BolDeSangManager/Data/Migrations/20260516000110_AddApresMatchValidation.Designer.cs
rm src/BolDeSangManager/Data/Migrations/20260517090101_FixDungeonBowlRoleLimits.cs
rm src/BolDeSangManager/Data/Migrations/20260517090101_FixDungeonBowlRoleLimits.Designer.cs
rm src/BolDeSangManager/Data/Migrations/ApplicationDbContextModelSnapshot.cs
rm -f src/BolDeSangManager/Data/boldesang.db
```

- [ ] **Step 5.3 : Vérifier qu'il ne reste que la migration Identity**

Run :
```bash
ls src/BolDeSangManager/Data/Migrations/
```
Expected : `00000000000000_CreateIdentitySchema.cs` et `00000000000000_CreateIdentitySchema.Designer.cs` uniquement.

- [ ] **Step 5.4 : Build (peut échouer si DbSeeder référence des choses inexistantes — c'est OK pour l'instant)**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`

Si erreurs liées à `Categorie` int ou `NombreAmeliorations`, c'est normal — elles seront résolues par les tâches suivantes. Noter les erreurs pour s'assurer qu'elles sont traitées.

- [ ] **Step 5.5 : Commit**

```bash
git add -A
git commit -m "chore: supprimer migrations applicatives obsolètes et snapshot (reset DB en dev)"
```

---

## Task 6 : Constantes d'amélioration (`ImprovementThresholds`)

**Files:**
- Create: `src/BolDeSangManager/Data/Seeding/ImprovementThresholds.cs`

- [ ] **Step 6.1 : Créer le fichier**

```csharp
using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Paliers de Points Star Player (PSP) et hausse de valeur associée selon le LRB Saison 3.
/// </summary>
public static class ImprovementThresholds
{
    /// <summary>
    /// PSP cumulés requis pour atteindre chaque palier (palier 1 = 6 PSP, palier 2 = 16, etc.).
    /// </summary>
    public static readonly int[] PspParPalier = [6, 16, 31, 51, 76, 176];

    /// <summary>
    /// Hausse de la valeur d'un joueur (en pièces d'or) selon le type d'amélioration choisi.
    /// Source : bloodbowl.md §11 (LRB S3).
    /// </summary>
    public static int HausseValeur(ImprovementType type, AffectedStat? stat = null) => type switch
    {
        ImprovementType.AleaPrimaire             => 10_000,
        ImprovementType.SelectionPrimaire        => 20_000,
        ImprovementType.AleaSecondaire           => 20_000,
        ImprovementType.SelectionSecondaire     => 40_000,
        ImprovementType.AmeliorationCarac        => 30_000,
        ImprovementType.AmeliorationForceArmure  => stat == AffectedStat.Force ? 80_000 : 40_000,
        _ => 0
    };

    /// <summary>
    /// Calcule le palier le plus haut atteint pour un nombre donné de PSP cumulés (0 si < 6 PSP).
    /// </summary>
    public static int PalierAtteint(int pspCumules) =>
        PspParPalier.Count(seuil => pspCumules >= seuil);
}
```

- [ ] **Step 6.2 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK.

- [ ] **Step 6.3 : Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/ImprovementThresholds.cs
git commit -m "feat: constantes des paliers PSP et hausses de valeur (LRB S3)"
```

---

## Task 7 : Données de seed des Skills

**Files:**
- Create: `src/BolDeSangManager/Data/Seeding/SkillSeedData.cs`

> **Source de vérité** : `docs/regles/bloodbowl.md §9` (compétences et traits par catégorie) et `docs/regles/dungeonbowl.md §5.4` (4 skills spécifiques DB). Au minimum **~115 skills** doivent être présents.

- [ ] **Step 7.1 : Créer le fichier skeleton avec les ~115 skills**

```csharp
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/bloodbowl.md §9 (LRB S3) et docs/regles/dungeonbowl.md §5.4 (DB-only).
/// </summary>
public static class SkillSeedData
{
    public static IEnumerable<Skill> GetSkills()
    {
        // ═══════════════════ AGILITÉ (A) ═══════════════════
        yield return new Skill { Nom = "Esquive", Categorie = SkillCategory.Agilite, Description = "Le joueur peut relancer un jet d'Esquive raté une fois par activation." };
        yield return new Skill { Nom = "Balle Collante", Categorie = SkillCategory.Agilite, Description = "Le joueur ne lâche pas le ballon en tombant s'il réussit un test d'Agilité." };
        yield return new Skill { Nom = "Bondissant", Categorie = SkillCategory.Agilite, Description = "Le joueur peut Bondir par-dessus les joueurs Debout adverses.", EstElite = true };
        yield return new Skill { Nom = "Crampon", Categorie = SkillCategory.Agilite, Description = "Le joueur ne peut Foncer sur un résultat de 1 ou 2 (au lieu de seulement 1)." };
        yield return new Skill { Nom = "Défieur", Categorie = SkillCategory.Agilite, Description = "Quand cible d'un Blocage, peut annuler le résultat et forcer un nouveau jet." };
        yield return new Skill { Nom = "Délestage", Categorie = SkillCategory.Agilite, Description = "Peut passer le ballon à un coéquipier adjacent comme action gratuite en tombant." };
        yield return new Skill { Nom = "Filou", Categorie = SkillCategory.Agilite, Description = "Peut ramasser le ballon dans la ZdT d'un adversaire sans modificateur." };
        yield return new Skill { Nom = "Jongleur", Categorie = SkillCategory.Agilite, Description = "Peut tenter de Réceptionner plusieurs passes dans le même tour." };
        yield return new Skill { Nom = "Pas Chassé", Categorie = SkillCategory.Agilite, Description = "Peut se déplacer dans la case qu'un adversaire vient de quitter." };
        yield return new Skill { Nom = "Prise Sûre", Categorie = SkillCategory.Agilite, Description = "Peut relancer un jet de Ramassage ou de Réception raté." };
        yield return new Skill { Nom = "Protection du Ballon", Categorie = SkillCategory.Agilite, Description = "Si porteur Poussé, ne lâche pas le ballon." };
        yield return new Skill { Nom = "Réflexes", Categorie = SkillCategory.Agilite, Description = "Peut annuler un dé de Blocage avec un jet d'Agilité." };
        yield return new Skill { Nom = "Saut", Categorie = SkillCategory.Agilite, Description = "Peut Bondir par-dessus les joueurs À Terre ou Sonnés." };
        yield return new Skill { Nom = "Tacle Plongeant", Categorie = SkillCategory.Agilite, Description = "Peut effectuer un Tacle en se plaçant À Terre." };
        yield return new Skill { Nom = "Réception Plongeante", Categorie = SkillCategory.Agilite, Description = "Peut tenter de Réceptionner une passe à 1 case en se plaçant À Terre." };
        yield return new Skill { Nom = "Sprint", Categorie = SkillCategory.Agilite, Description = "Peut Foncer 3 fois par activation (au lieu de 2)." };
        yield return new Skill { Nom = "Sournois", Categorie = SkillCategory.Agilite, Description = "Sur Foul/Compétence Scélérate, n'est expulsé que sur 1-2 (au lieu de 1)." };
        yield return new Skill { Nom = "Équilibre", Categorie = SkillCategory.Agilite, Description = "Peut tenter un jet d'Agilité pour rester debout après un résultat Repousse contre soi." };
        yield return new Skill { Nom = "Libération Contrôlée", Categorie = SkillCategory.Agilite, Description = "Peut sortir de zones de tacle sans malus dans certaines conditions." };
        yield return new Skill { Nom = "Glissade Contrôlée", Categorie = SkillCategory.Agilite, Description = "Quand le joueur tombe, il n'est pas Sonné mais reste À Terre." };

        // ═══════════════════ FORCE (F) ═══════════════════
        yield return new Skill { Nom = "Bagarreur", Categorie = SkillCategory.Force, Description = "Peut relancer un dé de Blocage qui ne lui convient pas." };
        yield return new Skill { Nom = "Bras Musclé", Categorie = SkillCategory.Force, Description = "Augmente la portée du Lancer de Coéquipier." };
        yield return new Skill { Nom = "Clé de Bras", Categorie = SkillCategory.Force, Description = "Sur résultat Repousse subi, peut forcer l'adversaire à rester en place." };
        yield return new Skill { Nom = "Crâne Épais", Categorie = SkillCategory.Force, Description = "Peut relancer son premier jet d'AR par match." };
        yield return new Skill { Nom = "Esquive en Force", Categorie = SkillCategory.Force, Description = "Quand cible d'un Blocage, peut tenter de Repousser l'attaquant." };
        yield return new Skill { Nom = "Garde", Categorie = SkillCategory.Force, Description = "Fournit du soutien offensif même s'il est Marqué." };
        yield return new Skill { Nom = "Gros Bras", Categorie = SkillCategory.Force, Description = "Peut Lancer un coéquipier avec Poids Plume.", EstTrait = true };
        yield return new Skill { Nom = "Juggernaut", Categorie = SkillCategory.Force, Description = "Lors d'un Blitz, peut traiter Plaqué adverse comme Repousse." };
        yield return new Skill { Nom = "Lancer de Coéquipier", Categorie = SkillCategory.Force, Description = "Peut Lancer un coéquipier avec Poids Plume comme Action de Passe." };
        yield return new Skill { Nom = "Marteau-pilon", Categorie = SkillCategory.Force, Description = "Inflige +1 sur le jet d'AR quand il Plaque un adversaire." };
        yield return new Skill { Nom = "Blocage Multiple", Categorie = SkillCategory.Force, Description = "Peut Bloquer deux adversaires adjacents simultanément." };
        yield return new Skill { Nom = "Peau de Fer", Categorie = SkillCategory.Force, Description = "Bénéficie de +1 sur ses jets d'AR." };
        yield return new Skill { Nom = "Projection", Categorie = SkillCategory.Force, Description = "Peut projeter un joueur adverse dans un espace adjacent lors d'un Blocage." };
        yield return new Skill { Nom = "Stabilité", Categorie = SkillCategory.Force, Description = "Ne peut pas être Repoussé par un Blocage." };
        yield return new Skill { Nom = "Châtaigne", Categorie = SkillCategory.Force, Description = "Modificateur +1 sur le jet d'AR de la cible lors d'un Blocage.", EstTrait = true };

        // ═══════════════════ GÉNÉRALE (G) ═══════════════════
        yield return new Skill { Nom = "Arracher le Ballon", Categorie = SkillCategory.Generale, Description = "Peut tenter d'arracher le ballon à un joueur adverse qui le porte." };
        yield return new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale, Description = "Les dés Plaqué comptent comme Repousse lors d'un Blocage subi ou infligé." };
        yield return new Skill { Nom = "Chef", Categorie = SkillCategory.Generale, Description = "Une fois par tour, utilise une Relance sans en consommer." };
        yield return new Skill { Nom = "Défenseur", Categorie = SkillCategory.Generale, Description = "Fournit du soutien défensif même s'il est Marqué." };
        yield return new Skill { Nom = "Frappe et Cours", Categorie = SkillCategory.Generale, Description = "Peut Bloquer puis se déplacer dans la même activation." };
        yield return new Skill { Nom = "Intrépide", Categorie = SkillCategory.Generale, Description = "Ne souffre pas des malus de Force contre des adversaires plus forts." };
        yield return new Skill { Nom = "Joueur Déloyal", Categorie = SkillCategory.Generale, Description = "Sur Foul, +1 à un jet d'AR ou de Blessure (au choix après le dé)." };
        yield return new Skill { Nom = "Lutte", Categorie = SkillCategory.Generale, Description = "Peut forcer les deux joueurs à tomber lors d'un Blocage." };
        yield return new Skill { Nom = "Nerfs d'Acier", Categorie = SkillCategory.Generale, Description = "Peut Réceptionner même à 2 cases d'un adversaire Debout." };
        yield return new Skill { Nom = "Parade", Categorie = SkillCategory.Generale, Description = "Bonus défensif contre les Blocages." };
        yield return new Skill { Nom = "Poursuite", Categorie = SkillCategory.Generale, Description = "Peut se déplacer d'une case vers un adversaire qui esquive sa ZdT." };
        yield return new Skill { Nom = "Pro", Categorie = SkillCategory.Generale, Description = "Une fois par tour, peut relancer un dé (3+) sans utiliser de Relance." };
        yield return new Skill { Nom = "Réception", Categorie = SkillCategory.Generale, Description = "Ignore les modificateurs négatifs sur les jets de Réception." };
        yield return new Skill { Nom = "Rétablissement", Categorie = SkillCategory.Generale, Description = "Peut se relever sans utiliser 3 cases de Mouvement." };
        yield return new Skill { Nom = "Sur le Ballon", Categorie = SkillCategory.Generale, Description = "Peut tenter de récupérer le ballon dans la ZdT d'un adversaire." };
        yield return new Skill { Nom = "Tacle", Categorie = SkillCategory.Generale, Description = "Les adversaires dans sa ZdT ne peuvent utiliser Esquive." };
        yield return new Skill { Nom = "Frénésie", Categorie = SkillCategory.Generale, Description = "Doit continuer à Bloquer/Blitzer le même adversaire après l'avoir Repoussé.", EstTrait = true };
        yield return new Skill { Nom = "Plaquage", Categorie = SkillCategory.Generale, Description = "Les adversaires dans sa ZdT ne peuvent utiliser Esquive ou Glissade Contrôlée." };

        // ═══════════════════ PASSE (P) ═══════════════════
        yield return new Skill { Nom = "Passe", Categorie = SkillCategory.Passe, Description = "Peut relancer son premier jet de Passe raté par match." };
        yield return new Skill { Nom = "Passe Assurée", Categorie = SkillCategory.Passe, Description = "Peut relancer un jet de Passe Courte ou Rapide raté." };
        yield return new Skill { Nom = "Passe dans la Course", Categorie = SkillCategory.Passe, Description = "Peut effectuer une Passe pendant un Blitz." };
        yield return new Skill { Nom = "Passe Longue Portée", Categorie = SkillCategory.Passe, Description = "+2 cases de portée sur ses passes." };
        yield return new Skill { Nom = "Précision", Categorie = SkillCategory.Passe, Description = "Bonus sur les jets de précision de passe." };
        yield return new Skill { Nom = "Fumblerooskie", Categorie = SkillCategory.Passe, Description = "Peut volontairement lâcher le ballon dans une case adjacente." };
        yield return new Skill { Nom = "Sur le Ballon (Passe)", Categorie = SkillCategory.Passe, Description = "Variante Passe de Sur le Ballon." };
        yield return new Skill { Nom = "Botter de Coéquipier", Categorie = SkillCategory.Passe, Description = "Peut botter un coéquipier avec Poids Plume." };
        yield return new Skill { Nom = "Transmission dans la Course", Categorie = SkillCategory.Passe, Description = "Peut continuer son mouvement après une Transmission.", GameSpecifique = GameType.DungeonBowl };
        yield return new Skill { Nom = "Passe par un Portail", Categorie = SkillCategory.Passe, Description = "Peut déclarer une Passe après avoir utilisé un portail.", GameSpecifique = GameType.DungeonBowl };
        yield return new Skill { Nom = "Lancer contre un Mur", Categorie = SkillCategory.Passe, Description = "+1 quand vise un mur lors d'une Passe.", GameSpecifique = GameType.DungeonBowl };

        // ═══════════════════ MUTATION (M) ═══════════════════
        yield return new Skill { Nom = "Bec Acéré", Categorie = SkillCategory.Mutation, Description = "Peut relancer un jet d'AR quand il inflige une Blessure.", EstElite = true };
        yield return new Skill { Nom = "Bras Supplémentaire", Categorie = SkillCategory.Mutation, Description = "+1 sur jets de Ramassage, Réception et Interception." };
        yield return new Skill { Nom = "Deux Têtes", Categorie = SkillCategory.Mutation, Description = "+1 sur jets d'Esquive." };
        yield return new Skill { Nom = "Grande Gueule", Categorie = SkillCategory.Mutation, Description = "Peut Croquer un adversaire adjacent (immobilise).", EstTrait = true };
        yield return new Skill { Nom = "Griffes", Categorie = SkillCategory.Mutation, Description = "Ignore l'Armure sur 8+ aux 2D6." };
        yield return new Skill { Nom = "Cornes", Categorie = SkillCategory.Mutation, Description = "+1 Force lors d'un Blitz." };
        yield return new Skill { Nom = "Main Démesurée", Categorie = SkillCategory.Mutation, Description = "+1 sur jets de Ramassage et d'Interception." };
        yield return new Skill { Nom = "Présence Perturbante", Categorie = SkillCategory.Mutation, Description = "Malus aux Passes adverses à 3 cases.", EstTrait = true };
        yield return new Skill { Nom = "Queue Préhensile", Categorie = SkillCategory.Mutation, Description = "Les adversaires subissent -1 aux jets d'Esquive dans sa ZdT." };
        yield return new Skill { Nom = "Répulsion", Categorie = SkillCategory.Mutation, Description = "Lors d'un Blocage, peut Repousser sans contact." };
        yield return new Skill { Nom = "Tentacules", Categorie = SkillCategory.Mutation, Description = "Un adversaire quittant sa ZdT doit réussir Agilité ou rester Marqué." };
        yield return new Skill { Nom = "Très Longues Jambes", Categorie = SkillCategory.Mutation, Description = "+1 sur Bond et peut Bondir par-dessus joueurs Debout." };

        // ═══════════════════ SCÉLÉRATE (S) ═══════════════════
        yield return new Skill { Nom = "Coup de Poing Vicieux", Categorie = SkillCategory.Scelerate, Description = "Peut frapper un joueur À Terre adjacent sans action de Blocage." };
        yield return new Skill { Nom = "Poignard", Categorie = SkillCategory.Scelerate, Description = "Peut poignarder un adversaire (+1 AR)." };
        yield return new Skill { Nom = "Meurtre Prémédité", Categorie = SkillCategory.Scelerate, Description = "Si inflige Élimination, peut aggraver la blessure.", EstElite = true };
        yield return new Skill { Nom = "Croc-en-jambe", Categorie = SkillCategory.Scelerate, Description = "Peut forcer un adversaire dans sa ZdT à relancer un Esquive réussi." };
        yield return new Skill { Nom = "Coup de Pied Sournois", Categorie = SkillCategory.Scelerate, Description = "Peut frapper un joueur au sol durant un Mouvement." };
        yield return new Skill { Nom = "Vraiment Sournois", Categorie = SkillCategory.Scelerate, Description = "Sur un Foul, peut être moins facilement repéré." };

        // ═══════════════════ TRAITS (passifs, non choisissables en amélioration) ═══════════════════
        yield return new Skill { Nom = "Animosité", Categorie = SkillCategory.Mutation, Description = "Peut refuser de passer le ballon à certains coéquipiers.", EstTrait = true };
        yield return new Skill { Nom = "Sauvagerie Animale", Categorie = SkillCategory.Mutation, Description = "Peut attaquer ses coéquipiers si activation ratée.", EstTrait = true };
        yield return new Skill { Nom = "Solitaire", Categorie = SkillCategory.Mutation, Description = "Doit réussir un jet pour utiliser une Relance d'équipe.", EstTrait = true };
        yield return new Skill { Nom = "Soif de Sang", Categorie = SkillCategory.Mutation, Description = "Doit boire le sang d'un coéquipier en début de tour.", EstTrait = true };
        yield return new Skill { Nom = "Regard Hypnotique", Categorie = SkillCategory.Mutation, Description = "Peut hypnotiser un adversaire (l'empêche d'utiliser Esquive).", EstTrait = true };
        yield return new Skill { Nom = "Cerveau Lent", Categorie = SkillCategory.Force, Description = "Doit réussir un jet pour Foncer.", EstTrait = true };
        yield return new Skill { Nom = "Gros Débile", Categorie = SkillCategory.Force, Description = "Doit réussir un jet pour activer son Trait à chaque tour.", EstTrait = true };
        yield return new Skill { Nom = "Toujours Affamé", Categorie = SkillCategory.Mutation, Description = "Peut accidentellement manger son coéquipier lors d'un Lancer.", EstTrait = true };
        yield return new Skill { Nom = "Prendre Racine", Categorie = SkillCategory.Force, Description = "Peut s'enraciner : ne se déplace plus mais difficile à pousser.", EstTrait = true };
        yield return new Skill { Nom = "Régénération", Categorie = SkillCategory.Mutation, Description = "Sur Élimination, 4+ → seulement KO/rate fin du match.", EstTrait = true };
        yield return new Skill { Nom = "Décomposition", Categorie = SkillCategory.Mutation, Description = "Perd 1 AR à chaque match.", EstTrait = true };
        yield return new Skill { Nom = "Contagieux", Categorie = SkillCategory.Mutation, Description = "Joueurs Éliminés peuvent contracter une maladie.", EstTrait = true };
        yield return new Skill { Nom = "Microbe", Categorie = SkillCategory.Mutation, Description = "Si petit, fournit du soutien offensif sans être Marqué.", EstTrait = true };
        yield return new Skill { Nom = "Poids Plume", Categorie = SkillCategory.Mutation, Description = "Peut être Lancé par un coéquipier avec Lancer de Coéquipier.", EstTrait = true };
        yield return new Skill { Nom = "Minus", Categorie = SkillCategory.Mutation, Description = "-1 sur ses jets d'AR.", EstTrait = true };
        yield return new Skill { Nom = "Poivrot", Categorie = SkillCategory.Mutation, Description = "Commence aléatoirement chaque match ivre ou sobre.", EstTrait = true };
        yield return new Skill { Nom = "Furie Débridée", Categorie = SkillCategory.Mutation, Description = "+1 dé lors d'un Blitz.", EstTrait = true };
        yield return new Skill { Nom = "Timmm-ber", Categorie = SkillCategory.Mutation, Description = "Peut tomber intentionnellement sur un adversaire.", EstTrait = true };
        yield return new Skill { Nom = "Gerbe de Vomi", Categorie = SkillCategory.Mutation, Description = "Peut régurgiter de la bile sur les adversaires adjacents.", EstTrait = true };

        // ═══════════════════ SPÉCIFIQUES DUNGEON BOWL ═══════════════════
        yield return new Skill { Nom = "Navigateur de Portail", Categorie = SkillCategory.Generale, Description = "Peut relancer le D6 lorsqu'il détermine le portail d'arrivée.", GameSpecifique = GameType.DungeonBowl };
    }
}
```

> **Note** : la liste ci-dessus contient environ ~85-90 skills "actifs/élites" + traits. Si l'implémentation détecte des skills référencés par les rosters (Tâches 8-9) mais absents de cette liste, **ajouter au fil de l'eau** plutôt que créer un test exhaustif a priori. Le seed est tolérant : `SeedPositionSkillsAsync` ignore les skills inconnus (mais log un warning à ajouter).

- [ ] **Step 7.2 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK (le fichier est isolé, pas encore consommé).

- [ ] **Step 7.3 : Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/SkillSeedData.cs
git commit -m "feat: extraire la liste des skills (LRB S3 + spécifiques DungeonBowl)"
```

---

## Task 8 : Données de seed Blood Bowl (30 équipes)

**Files:**
- Create: `src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs`

> **Source** : `docs/regles/bloodbowl.md §12` (30 équipes, ordre alphabétique).

- [ ] **Step 8.1 : Créer le fichier avec le helper `Pos` et 2 équipes d'exemple**

Créer le fichier complet ci-dessous. L'implémenteur doit **ajouter les 28 équipes restantes en lisant `bloodbowl.md §12`** ; chaque entrée respecte exactement le format des deux exemples.

```csharp
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/bloodbowl.md §12 (LRB Saison 3, 30 équipes).
/// </summary>
public static class BloodBowlTeamSeedData
{
    public record TeamSeed(TeamType Type, List<PlayerPosition> Positions);

    public static IEnumerable<TeamSeed> GetTeams(int bbGameId)
    {
        // =================== ALLIANCE DU VIEUX MONDE ===================
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                Nom = "Alliance du Vieux Monde",
                CoutRelance = 70_000,
                Categorie = TeamCategory.Staller,
                ReglesSpecialesLigue = "OldWorldClassic",
                ReglesSpeciales = "Alliance hétérogène d'humains, nains et halflings."
            },
            [
                Pos("Lineman Humain", 12, 50_000, 6, 3, "3+", "4+", "9+", "GAF", ""),
                Pos("Lanceur Humain", 2, 75_000, 6, 3, "3+", "2+", "9+", "GPAF", "", skills: "Passe,Prise Sûre"),
                Pos("Receveur Humain", 2, 65_000, 8, 2, "3+", "5+", "8+", "AGF", "", skills: "Réception,Esquive"),
                Pos("Blitzer Humain", 2, 85_000, 7, 3, "3+", "4+", "9+", "GFAP", "", skills: "Blocage"),
                Pos("Bloqueur Nain", 4, 70_000, 4, 3, "4+", "5+", "10+", "GFA", "", skills: "Blocage,Tacle,Crâne Épais"),
                Pos("Halfling Hopeful", 2, 30_000, 5, 2, "3+", "4+", "7+", "AGF", "", skills: "Esquive,Poids Plume,Minus"),
                Pos("Tréant Halfling", 1, 120_000, 2, 6, "5+", "5+", "11+", "FAGP", "", isBigGuy: true, skills: "Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier,Timmm-ber"),
            ]
        );

        // =================== AMAZONES ===================
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                Nom = "Amazones",
                CoutRelance = 50_000,
                Categorie = TeamCategory.Agile,
                ReglesSpecialesLigue = "LustrianSuperleague",
                ReglesSpeciales = "Toutes les amazones ont Esquive de base. Équipe agile sans Gros Bras."
            },
            [
                Pos("Linewoman Amazone", 16, 50_000, 6, 3, "3+", "4+", "7+", "GAF", "", skills: "Esquive"),
                Pos("Lanceuse Amazone", 2, 70_000, 6, 3, "3+", "2+", "7+", "GPAF", "", skills: "Esquive,Passe"),
                Pos("Receveuse Amazone", 2, 70_000, 6, 3, "3+", "5+", "7+", "AGF", "", skills: "Esquive,Réception"),
                Pos("Blitzeuse Amazone", 4, 90_000, 6, 3, "3+", "4+", "7+", "GFAP", "", skills: "Esquive,Blocage"),
            ]
        );

        // =================== BAS-FONDS ===================
        // TODO: ajouter selon bloodbowl.md §12
        // =================== BRETONNIENS ===================
        // TODO
        // ... (28 équipes restantes à ajouter en suivant exactement ce format)
    }

    private static PlayerPosition Pos(
        string nom, int qteMax, int cout,
        int mv, int force, string ag, string cp, string ar,
        string principal, string secondaire,
        bool isBigGuy = false, string skills = "",
        string? roleNom = null, int roleMax = 0)
    {
        var p = new PlayerPosition
        {
            Nom = nom,
            QuantiteMax = qteMax,
            Cout = cout,
            Mouvement = mv,
            Force = force,
            Agilite = ag,
            CapacitePasse = cp,
            Armure = ar,
            CompetencesPrincipales = principal,
            CompetencesSecondaires = secondaire,
            EstGrosBras = isBigGuy,
            RoleNom = roleNom,
            RoleQuantiteMax = roleMax > 0 ? roleMax : qteMax,
        };
        p._StartingSkillsTemp = skills;
        return p;
    }
}
```

- [ ] **Step 8.2 : Compléter les 28 équipes restantes**

Pour chaque équipe listée dans `bloodbowl.md §12` (et absente du fichier), reprendre le format et créer un `yield return new TeamSeed(...)`. Ordre : Bas-fonds, Bretonniens, Elfes Noirs, Elfes Sylvains, Élus du Chaos, Gnomes, Gobelins, Halflings, Hauts Elfes, Hommes-lézards, Horreurs Nécromantiques, Humains, Khorne, Morts-Ambulants, Nains, Nains du Chaos, Noblesse Impériale, Nordiques, Nurgle, Ogres, Orques, Orques Noirs, Renégats du Chaos, Rois des Tombes, Skavens, Snotlings, Union Elfique, Vampires.

**Pour chaque équipe** :
- `Categorie` : déduire du style de jeu (Bashy/Staller/Agile/Specialist).
- `ReglesSpecialesLigue` : CSV des règles d'éligibilité ligues thématiques (souvent vide ou `OldWorldClassic`, `BadlandsBrawl`, `FavoredOfChaos`, `FavoredOfNurgle`, `MastersOfUndeath`, `ElvenKingdoms`, `LowCostLinemen`, `HalflingThimbleCup`, `UnderworldChallenge`, etc. — cf §12 du markdown).
- `ReglesSpeciales` : description libre courte.
- `Postes` : copier exactement les stats, coûts, accès Principal/Secondaire et compétences de départ du markdown.
- **Cohérence des skills de départ** : tout `skill:` cité doit exister dans `SkillSeedData.cs` (Task 7). Si manquant, ajouter dans `SkillSeedData`.

- [ ] **Step 8.3 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK.

- [ ] **Step 8.4 : Vérifier le compte d'équipes**

Run (depuis le répertoire racine) :
```bash
grep -c "yield return new TeamSeed" src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs
```
Expected : `30`.

- [ ] **Step 8.5 : Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/BloodBowlTeamSeedData.cs src/BolDeSangManager/Data/Seeding/SkillSeedData.cs
git commit -m "feat: seed des 30 équipes Blood Bowl LRB S3"
```

---

## Task 9 : Données de seed Dungeon Bowl (8 collèges)

**Files:**
- Create: `src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs`

> **Source** : `docs/regles/dungeonbowl.md §2` (8 collèges).

- [ ] **Step 9.1 : Créer le fichier**

```csharp
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/dungeonbowl.md §2 (Edition 2022, 8 collèges de magie).
/// Toutes les équipes payent 50 000 po pour les Relances (règle uniforme DB).
/// </summary>
public static class DungeonBowlTeamSeedData
{
    public record TeamSeed(TeamType Type, List<PlayerPosition> Positions);

    public static IEnumerable<TeamSeed> GetColleges(int dbGameId)
    {
        // =================== COLLÈGE DES CIEUX ===================
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                Nom = "Collège des Cieux",
                CoutRelance = 50_000,
                Categorie = TeamCategory.Agile,
                ReglesSpeciales = "Magicien Céleste disponible. Vitesse et équilibre."
            },
            [
                Pos("Skink Runner", 16, 60_000, 8, 2, "3+", "4+", "8+", "AG", "PF", skills: "Esquive,Minus", roleNom: "Joueur de ligne", roleMax: 16),
                Pos("Norse Lineman", 16, 60_000, 6, 3, "3+", "4+", "8+", "GA", "F", skills: "Blocage,Poivrot,Crâne Épais", roleNom: "Joueur de ligne", roleMax: 16),
                Pos("Eagle Warrior", 16, 50_000, 6, 3, "3+", "4+", "8+", "GA", "F", skills: "Esquive", roleNom: "Joueur de ligne", roleMax: 16),
                Pos("Noble Blitzer", 4, 105_000, 7, 3, "3+", "4+", "9+", "AG", "PF", skills: "Blocage,Réception", roleNom: "Blitzeur", roleMax: 4),
                Pos("Norse Berzerker", 4, 90_000, 6, 3, "3+", "5+", "8+", "GFA", "P", skills: "Blocage,Frénésie,Rétablissement", roleNom: "Blitzeur", roleMax: 4),
                Pos("Piranaha Warrior", 4, 90_000, 7, 3, "3+", "5+", "8+", "GAF", "P", skills: "Esquive,Frappe et Cours,Rétablissement", roleNom: "Blitzeur", roleMax: 4),
                Pos("Lanceur Humain (Cieux)", 2, 80_000, 6, 3, "3+", "2+", "9+", "GPAF", "", skills: "Passe,Prise Sûre", roleNom: "Lanceur", roleMax: 2),
                Pos("Python Warrior", 2, 75_000, 6, 3, "3+", "3+", "8+", "GA", "F", skills: "Esquive,Sur le Ballon,Passe,Passe Assurée", roleNom: "Lanceur", roleMax: 2),
                Pos("Saurus (Cieux)", 6, 85_000, 6, 4, "5+", "6+", "10+", "GF", "A", roleNom: "Défenseur", roleMax: 6),
                Pos("Jaguar Warrior", 6, 110_000, 6, 4, "3+", "5+", "9+", "GFA", "", skills: "Défenseur,Esquive", roleNom: "Défenseur", roleMax: 6),
            ]
        );

        // =================== COLLÈGE DU FEU ===================
        // TODO: copier depuis dungeonbowl.md §2.2
        // =================== COLLÈGE DE L'OMBRE ===================
        // TODO §2.3
        // =================== COLLÈGE DE LA LUMIÈRE ===================
        // TODO §2.4
        // =================== COLLÈGE DE LA VIE ===================
        // TODO §2.5
        // =================== COLLÈGE DU MÉTAL ===================
        // TODO §2.6
        // =================== COLLÈGE DE LA MORT ===================
        // TODO §2.7
        // =================== COLLÈGE DES BÊTES ===================
        // TODO §2.8
    }

    private static PlayerPosition Pos(
        string nom, int qteMax, int cout,
        int mv, int force, string ag, string cp, string ar,
        string principal, string secondaire,
        bool isBigGuy = false, string skills = "",
        string? roleNom = null, int roleMax = 0)
    {
        var p = new PlayerPosition
        {
            Nom = nom,
            QuantiteMax = qteMax,
            Cout = cout,
            Mouvement = mv,
            Force = force,
            Agilite = ag,
            CapacitePasse = cp,
            Armure = ar,
            CompetencesPrincipales = principal,
            CompetencesSecondaires = secondaire,
            EstGrosBras = isBigGuy,
            RoleNom = roleNom,
            RoleQuantiteMax = roleMax > 0 ? roleMax : qteMax,
        };
        p._StartingSkillsTemp = skills;
        return p;
    }
}
```

- [ ] **Step 9.2 : Compléter les 7 collèges restants**

Pour chacun (Feu, Ombre, Lumière, Vie, Métal, Mort, Bêtes), copier depuis le markdown les postes avec **leur `RoleNom` et `RoleQuantiteMax`** (les sections du tableau dans le markdown — `Lineman (0-16)`, `Blitzer (0-X)`, etc.). Le `RoleNom` doit grouper les postes du même type (ex: "Joueur de ligne" pour tous les linemen d'un collège).

Pour les marqueurs ⚠️ du markdown (alignements suspects), interpréter au plus juste et ajouter un commentaire `// ⚠️ vérifier coût/stat` si doute.

- [ ] **Step 9.3 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK.

- [ ] **Step 9.4 : Vérifier le compte**

```bash
grep -c "yield return new TeamSeed" src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs
```
Expected : `8`.

- [ ] **Step 9.5 : Commit**

```bash
git add src/BolDeSangManager/Data/Seeding/DungeonBowlTeamSeedData.cs
git commit -m "feat: seed des 8 collèges Dungeon Bowl Edition 2022"
```

---

## Task 10 : Refactor du `DbSeeder` (orchestrateur)

**Files:**
- Modify: `src/BolDeSangManager/Data/DbSeeder.cs`

- [ ] **Step 10.1 : Remplacer entièrement le contenu**

Remplacer le fichier `DbSeeder.cs` par :

```csharp
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Data.Seeding;
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
        await SeedRolesAsync(roleManager);

        if (!db.Games.Any())
        {
            await SeedGamesAndVersionsAsync(db);
            await SeedSkillsAsync(db);
            await SeedBloodBowlTeamsAsync(db);
            await SeedDungeonBowlTeamsAsync(db);
            await SeedPositionSkillsAsync(db, logger);
        }

        await SeedAdminUserAsync(userManager, config);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Commissaire", "Coach" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
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
            await userManager.AddToRoleAsync(admin, "Commissaire");
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
        db.Skills.AddRange(SkillSeedData.GetSkills());
        await db.SaveChangesAsync();
    }

    private static async Task SeedBloodBowlTeamsAsync(ApplicationDbContext db)
    {
        var bbGame = await db.Games.FirstAsync(g => g.Type == GameType.BloodBowl);
        foreach (var (type, positions) in BloodBowlTeamSeedData.GetTeams(bbGame.Id))
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

    private static async Task SeedDungeonBowlTeamsAsync(ApplicationDbContext db)
    {
        var dbGame = await db.Games.FirstAsync(g => g.Type == GameType.DungeonBowl);
        foreach (var (type, positions) in DungeonBowlTeamSeedData.GetColleges(dbGame.Id))
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

    private static async Task SeedPositionSkillsAsync(ApplicationDbContext db, ILogger logger)
    {
        var allPositions = await db.PlayerPositions.Include(p => p.CompetencesDepart).ToListAsync();
        var allSkills = await db.Skills.ToDictionaryAsync(s => s.Nom.ToLower());

        var missing = new HashSet<string>();
        foreach (var position in allPositions)
        {
            if (string.IsNullOrEmpty(position._StartingSkillsTemp)) continue;

            var skillNames = position._StartingSkillsTemp.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawName in skillNames)
            {
                var name = rawName.Trim().ToLower();
                if (allSkills.TryGetValue(name, out var skill))
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
                    missing.Add($"{position.Nom} → {rawName.Trim()}");
                }
            }
        }
        await db.SaveChangesAsync();

        if (missing.Count > 0)
            logger.LogWarning("Skills de départ non trouvés dans la base : {Missing}", string.Join(" ; ", missing));
    }
}
```

- [ ] **Step 10.2 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK.

- [ ] **Step 10.3 : Commit**

```bash
git add src/BolDeSangManager/Data/DbSeeder.cs
git commit -m "refactor: DbSeeder devient orchestrateur, délègue aux fichiers Seeding/"
```

---

## Task 11 : `MatchService.CalculerPSP` — gameType-aware

**Files:**
- Modify: `src/BolDeSangManager/Services/MatchService.cs`
- Modify: `tests/BolDeSangManager.Tests/MatchServiceTests.cs`

- [ ] **Step 11.1 : Écrire le test BloodBowl (TD = 3 PSP)**

Ajouter à la fin de la classe `MatchServiceTests` dans `tests/BolDeSangManager.Tests/MatchServiceTests.cs` :

```csharp
[Fact]
public void CalculerPSP_BloodBowl_TouchdownDonne3Points()
{
    var record = new MatchPlayerRecord { Touchdowns = 2 };
    var psp = MatchService.CalculerPSPPublic(record, GameType.BloodBowl);
    Assert.Equal(6, psp);
}

[Fact]
public void CalculerPSP_DungeonBowl_TouchdownDonne5Points()
{
    var record = new MatchPlayerRecord { Touchdowns = 2 };
    var psp = MatchService.CalculerPSPPublic(record, GameType.DungeonBowl);
    Assert.Equal(10, psp);
}

[Fact]
public void CalculerPSP_TousLesEvenementsCombines_BloodBowl()
{
    var record = new MatchPlayerRecord
    {
        Touchdowns = 1,          // 3 PSP
        Completions = 2,         // 2 PSP
        Interceptions = 1,       // 2 PSP
        EliminationsInfligees = 2, // 4 PSP
        EstMVP = true            // 4 PSP
    };
    var psp = MatchService.CalculerPSPPublic(record, GameType.BloodBowl);
    Assert.Equal(15, psp);
}
```

- [ ] **Step 11.2 : Lancer les tests (doivent échouer — méthode `CalculerPSPPublic` n'existe pas)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~CalculerPSP"`
Expected : compilation error ou test FAIL (méthode non trouvée).

- [ ] **Step 11.3 : Modifier `MatchService.CalculerPSP`**

Dans `src/BolDeSangManager/Services/MatchService.cs`, remplacer la méthode actuelle :

```csharp
private static int CalculerPSP(MatchPlayerRecord record, GameType gameType)
{
    int psp = 0;
    psp += record.Touchdowns * 3;
    psp += record.Completions * 1;
    psp += record.Interceptions * 2;
    psp += record.EliminationsInfligees * 2;
    if (record.EstMVP) psp += 4;
    return psp;
}
```

par :

```csharp
/// <summary>
/// Calcule les Points Star Player gagnés selon le LRB Saison 3 / Dungeon Bowl Edition 2022.
/// </summary>
/// <param name="record">Statistiques du joueur sur le match</param>
/// <param name="gameType">Type de jeu (TD = 5 en DungeonBowl, 3 sinon)</param>
public static int CalculerPSPPublic(MatchPlayerRecord record, GameType gameType)
{
    int pspParTd = gameType == GameType.DungeonBowl ? 5 : 3;
    int psp = 0;
    psp += record.Touchdowns * pspParTd;
    psp += record.Completions * 1;
    psp += record.Interceptions * 2;
    psp += record.EliminationsInfligees * 2;
    if (record.EstMVP) psp += 4;
    return psp;
}

private static int CalculerPSP(MatchPlayerRecord record, GameType gameType)
    => CalculerPSPPublic(record, gameType);
```

> **Note** : on garde la version `private` pour ne pas casser les appelants internes. La version `public static` permet aux tests d'appeler directement.

- [ ] **Step 11.4 : Lancer les tests (doivent passer)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~CalculerPSP"`
Expected : 3 tests PASS.

- [ ] **Step 11.5 : Commit**

```bash
git add src/BolDeSangManager/Services/MatchService.cs tests/BolDeSangManager.Tests/MatchServiceTests.cs
git commit -m "feat: CalculerPSP applique TD=5 en DungeonBowl, TD=3 en BloodBowl"
```

---

## Task 12 : `TeamService.AppliquerAmeliorationAsync` (remplace `AttributerCompetenceAsync`)

**Files:**
- Modify: `src/BolDeSangManager/Services/TeamService.cs`
- Modify: `tests/BolDeSangManager.Tests/TeamServiceTests.cs`
- Modify: `src/BolDeSangManager/Services/MatchService.cs` (appel via `ValiderApresMatchCoachAsync`)

- [ ] **Step 12.1 : Écrire le test "palier non atteint = exception"**

Ajouter à la fin de `tests/BolDeSangManager.Tests/TeamServiceTests.cs` :

```csharp
[Fact]
public async Task AppliquerAmelioration_PalierNonAtteint_LeveException()
{
    await using var db = _factory.CreateContext();
    var (game, _) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var coach = DataSeeder.CreateUser("p1");
    db.Users.Add(coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, (await db.RulesVersions.FirstAsync()).Id, coach.Id);
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
    var joueur = new TeamPlayer
    {
        TeamId = equipe.Id,
        PlayerPositionId = position.Id,
        Nom = "Test", Numero = 1, ValeurActuelle = 50_000,
        PointsStarPlayer = 3 // Moins que le seuil de 6
    };
    db.TeamPlayers.Add(joueur);
    await db.SaveChangesAsync();

    var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale };
    db.Skills.Add(skill);
    await db.SaveChangesAsync();

    var service = new TeamService(db, NullLogger<TeamService>.Instance);

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.AppliquerAmeliorationAsync(joueur.Id, ImprovementType.SelectionPrimaire, skillId: skill.Id));
}
```

- [ ] **Step 12.2 : Écrire le test "palier atteint = improvement créé + valeur augmentée"**

Ajouter dans le même fichier :

```csharp
[Fact]
public async Task AppliquerAmelioration_PalierAtteint_CreeImprovementEtAugmenteValeur()
{
    await using var db = _factory.CreateContext();
    var (game, _) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var coach = DataSeeder.CreateUser("p2");
    db.Users.Add(coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, (await db.RulesVersions.FirstAsync()).Id, coach.Id);
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
    var joueur = new TeamPlayer
    {
        TeamId = equipe.Id,
        PlayerPositionId = position.Id,
        Nom = "Test", Numero = 1, ValeurActuelle = 50_000,
        PointsStarPlayer = 6
    };
    db.TeamPlayers.Add(joueur);
    await db.SaveChangesAsync();

    var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale };
    db.Skills.Add(skill);
    await db.SaveChangesAsync();

    var service = new TeamService(db, NullLogger<TeamService>.Instance);
    await service.AppliquerAmeliorationAsync(joueur.Id, ImprovementType.SelectionPrimaire, skillId: skill.Id);

    var maj = await db.TeamPlayers.Include(j => j.Improvements).Include(j => j.Competences).FirstAsync(j => j.Id == joueur.Id);
    Assert.Single(maj.Improvements);
    Assert.Equal(1, maj.Improvements.First().Palier);
    Assert.Equal(ImprovementType.SelectionPrimaire, maj.Improvements.First().Type);
    Assert.Equal(70_000, maj.ValeurActuelle); // 50_000 + 20_000
    Assert.Contains(maj.Competences, c => c.SkillId == skill.Id && !c.EstCompetenceDepart);
}
```

- [ ] **Step 12.3 : Lancer les tests (doivent échouer — méthode inexistante)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~AppliquerAmelioration"`
Expected : compilation error.

- [ ] **Step 12.4 : Implémenter `AppliquerAmeliorationAsync` dans `TeamService`**

Dans `src/BolDeSangManager/Services/TeamService.cs`, **supprimer** la méthode `AttributerCompetenceAsync` et la remplacer par :

```csharp
public async Task AppliquerAmeliorationAsync(
    int joueurId,
    ImprovementType type,
    int? skillId = null,
    AffectedStat? statAmelioree = null,
    int? matchSheetId = null)
{
    var joueur = await db.TeamPlayers
        .Include(j => j.Improvements)
        .FirstOrDefaultAsync(j => j.Id == joueurId)
        ?? throw new InvalidOperationException("Joueur introuvable");

    var palierDispo = ImprovementThresholds.PalierAtteint(joueur.PointsStarPlayer) - joueur.Improvements.Count;
    if (palierDispo <= 0)
        throw new InvalidOperationException(
            $"Aucun palier d'amélioration disponible (PSP={joueur.PointsStarPlayer}, déjà consommés={joueur.Improvements.Count}).");

    // Validation du type vs paramètres fournis
    bool requiertSkill = type is ImprovementType.AleaPrimaire or ImprovementType.SelectionPrimaire
                           or ImprovementType.AleaSecondaire or ImprovementType.SelectionSecondaire;
    bool requiertStat = type is ImprovementType.AmeliorationCarac or ImprovementType.AmeliorationForceArmure;

    if (requiertSkill && skillId is null)
        throw new InvalidOperationException("Un skillId est requis pour ce type d'amélioration.");
    if (requiertStat && statAmelioree is null)
        throw new InvalidOperationException("Une stat ciblée est requise pour ce type d'amélioration.");

    var prochainPalier = joueur.Improvements.Count + 1;
    var hausse = ImprovementThresholds.HausseValeur(type, statAmelioree);

    var improvement = new PlayerImprovement
    {
        TeamPlayerId = joueurId,
        Palier = prochainPalier,
        Type = type,
        SkillId = skillId,
        StatAmelioree = statAmelioree,
        ValeurHausse = hausse,
        MatchSheetId = matchSheetId
    };
    db.PlayerImprovements.Add(improvement);

    // Si skill : ajouter à la liste des compétences acquises (non de départ)
    if (skillId.HasValue)
    {
        db.TeamPlayerSkills.Add(new TeamPlayerSkill
        {
            TeamPlayerId = joueurId,
            SkillId = skillId.Value,
            EstCompetenceDepart = false,
            EnAttenteValidation = false
        });
    }

    // Si stat : appliquer le modificateur
    if (statAmelioree.HasValue)
    {
        switch (statAmelioree.Value)
        {
            case AffectedStat.Mouvement: joueur.ModMouvement++; break;
            case AffectedStat.Force: joueur.ModForce++; break;
            case AffectedStat.Agilite: joueur.ModAgilite++; break;
            case AffectedStat.CapacitePasse: joueur.ModCapacitePasse++; break;
            case AffectedStat.Armure: joueur.ModArmure++; break;
        }
    }

    joueur.ValeurActuelle += hausse;
    await db.SaveChangesAsync();

    logger.LogInformation(
        "Joueur id={JoueurId} : amélioration palier {Palier} (type={Type}, skill={SkillId}, stat={Stat}, hausse={Hausse})",
        joueurId, prochainPalier, type, skillId, statAmelioree, hausse);
}
```

Ajouter en haut du fichier :
```csharp
using BolDeSangManager.Data.Seeding; // pour ImprovementThresholds
```

- [ ] **Step 12.5 : Mettre à jour `MatchService.ValiderApresMatchCoachAsync`**

Dans `src/BolDeSangManager/Services/MatchService.cs`, trouver la portion qui appelle `teamService.AttributerCompetenceAsync` :

```csharp
// Compétences
foreach (var (joueurId, skillId, estPrincipale) in competences)
    await teamService.AttributerCompetenceAsync(joueurId, skillId, estPrincipale);
```

Remplacer par :

```csharp
// Améliorations (Sélection Primaire si principale, Secondaire sinon)
foreach (var (joueurId, skillId, estPrincipale) in competences)
{
    var type = estPrincipale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire;
    await teamService.AppliquerAmeliorationAsync(joueurId, type, skillId: skillId, matchSheetId: feuille.Id);
}
```

> Note : la signature `(joueurId, skillId, estPrincipale)` est conservée pour compatibilité UI. Une évolution future enrichira l'UI pour permettre les autres types d'améliorations (Aléa, Carac).

- [ ] **Step 12.6 : Adapter `ModifierFeuilleAsync` pour inverser les Improvements**

Dans `MatchService.ModifierFeuilleAsync`, ajouter la suppression des `PlayerImprovement` liés à la feuille (sinon ils restent orphelins) :

```csharp
// Avant l'insertion des nouveaux records : supprimer les Improvements liés à cette feuille
var oldImprovements = await db.PlayerImprovements
    .Where(pi => pi.MatchSheetId == feuille.Id)
    .ToListAsync();
foreach (var imp in oldImprovements)
{
    // Inverser la hausse de valeur sur le joueur
    var j = await db.TeamPlayers.FindAsync(imp.TeamPlayerId);
    if (j != null) j.ValeurActuelle = Math.Max(0, j.ValeurActuelle - imp.ValeurHausse);
}
db.PlayerImprovements.RemoveRange(oldImprovements);
await db.SaveChangesAsync();
```

(à insérer dans le bloc de la méthode, juste après l'inversion des PSP). Faire un grep pour trouver la zone exacte si nécessaire :
```bash
grep -n "ModifierFeuilleAsync\|RecordsJoueurs" src/BolDeSangManager/Services/MatchService.cs
```

- [ ] **Step 12.7 : Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : OK.

- [ ] **Step 12.8 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj`
Expected : tous tests PASS (les 2 nouveaux + pas de régression).

- [ ] **Step 12.9 : Commit**

```bash
git add -A
git commit -m "feat: AppliquerAmeliorationAsync remplace AttributerCompetenceAsync avec paliers PSP LRB S3"
```

---

## Task 13 : Phase de repos — `LancerPhaseDeReposAsync` + `ValiderApresMatchReposAsync`

**Files:**
- Modify: `src/BolDeSangManager/Services/LeagueService.cs`
- Modify: `tests/BolDeSangManager.Tests/LeagueServiceTests.cs`

- [ ] **Step 13.1 : Écrire le test**

Ajouter à la fin de `LeagueServiceTests.cs` :

```csharp
[Fact]
public async Task LancerPhaseDeRepos_ChangeStatutEtResetRPM()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c");
    var coach = DataSeeder.CreateUser("co");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();

    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    ligue.Statut = LeagueStatus.EnCours;
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
    var j1 = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "j1", Numero = 1, ManqueSuivantMatch = true };
    var j2 = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "j2", Numero = 2, ManqueSuivantMatch = true };
    db.TeamPlayers.AddRange(j1, j2);
    await db.SaveChangesAsync();

    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);
    await service.LancerPhaseDeReposAsync(ligue.Id);

    var maj = await db.Leagues.FindAsync(ligue.Id);
    var joueurs = await db.TeamPlayers.Where(j => j.TeamId == equipe.Id).ToListAsync();

    Assert.Equal(LeagueStatus.PhaseDeRepos, maj!.Statut);
    Assert.All(joueurs, j => Assert.False(j.ManqueSuivantMatch));
}
```

- [ ] **Step 13.2 : Lancer le test (doit échouer)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~LancerPhaseDeRepos"`
Expected : FAIL (méthode inexistante).

- [ ] **Step 13.3 : Implémenter la méthode**

Dans `src/BolDeSangManager/Services/LeagueService.cs`, ajouter (avant `TerminerLigueAsync`) :

```csharp
public async Task LancerPhaseDeReposAsync(int ligueId)
{
    var ligue = await db.Leagues.FindAsync(ligueId)
        ?? throw new InvalidOperationException("Ligue introuvable");

    if (ligue.Statut != LeagueStatus.EnCours)
        throw new InvalidOperationException("La phase de repos ne peut être lancée que depuis l'état EnCours.");

    // Reset RPM pour tous les joueurs des équipes de la ligue
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
```

- [ ] **Step 13.4 : Adapter `GenererPlayoffsAsync` pour exiger la phase de repos**

Dans la méthode `GenererPlayoffsAsync` (toujours dans `LeagueService.cs`), juste après la récupération de `ligue` :

```csharp
if (ligue.Statut != LeagueStatus.PhaseDeRepos && ligue.Statut != LeagueStatus.EnCours)
    throw new InvalidOperationException("Les playoffs ne peuvent être générés qu'après la saison régulière ou la phase de repos.");
```

> Note : on tolère `EnCours` pour rétrocompat, mais l'UI poussera désormais à passer par `PhaseDeRepos`.

- [ ] **Step 13.5 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj`
Expected : PASS.

- [ ] **Step 13.6 : Commit intermédiaire**

```bash
git add -A
git commit -m "feat: LancerPhaseDeReposAsync reset les RPM et passe en PhaseDeRepos"
```

- [ ] **Step 13.7 : Écrire le test `ValiderApresMatchRepos`**

Ajouter à `LeagueServiceTests.cs` :

```csharp
[Fact]
public async Task ValiderApresMatchRepos_CreeValidationEtAppliqueAchats()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c");
    var coach = DataSeeder.CreateUser("co");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();

    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    ligue.Statut = LeagueStatus.PhaseDeRepos;
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test");
    equipe.Tresorerie = 200_000;
    var joueur = new TeamPlayer
    {
        TeamId = equipe.Id,
        PlayerPositionId = position.Id,
        Nom = "J1", Numero = 1, ValeurActuelle = 50_000, PointsStarPlayer = 6
    };
    db.TeamPlayers.Add(joueur);
    var skill = new Skill { Nom = "Blocage", Categorie = SkillCategory.Generale };
    db.Skills.Add(skill);
    await db.SaveChangesAsync();

    var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);

    await service.ValiderApresMatchReposAsync(
        ligueId: ligue.Id,
        teamId: equipe.Id,
        competences: [(joueur.Id, skill.Id, estPrincipale: true)],
        nouveauxJoueurs: [],
        nouvellesRelances: 1,
        teamService: teamService);

    var validation = await db.PhaseDeReposValidations.FirstOrDefaultAsync(v => v.LeagueId == ligue.Id && v.TeamId == equipe.Id);
    Assert.NotNull(validation);

    var equipeMaj = await db.Teams.FindAsync(equipe.Id);
    Assert.Equal(1, equipeMaj!.NombreRelances);
    // Coût relance hors-saison : 2 × CoutRelance (50k pour le seed test)
    Assert.Equal(200_000 - 100_000, equipeMaj.Tresorerie);

    var jMaj = await db.TeamPlayers.Include(j => j.Improvements).FirstAsync(j => j.Id == joueur.Id);
    Assert.Single(jMaj.Improvements);
}

[Fact]
public async Task ValiderApresMatchRepos_DejaValide_LeveException()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c2");
    var coach = DataSeeder.CreateUser("co2");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    ligue.Statut = LeagueStatus.PhaseDeRepos;
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test2");

    db.PhaseDeReposValidations.Add(new PhaseDeReposValidation { LeagueId = ligue.Id, TeamId = equipe.Id });
    await db.SaveChangesAsync();

    var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.ValiderApresMatchReposAsync(ligue.Id, equipe.Id, [], [], 0, teamService));
}

[Fact]
public async Task ValiderApresMatchRepos_LigueHorsPhase_LeveException()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, _) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c3");
    var coach = DataSeeder.CreateUser("co3");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    ligue.Statut = LeagueStatus.EnCours; // pas en repos
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "Test3");
    await db.SaveChangesAsync();

    var teamService = new TeamService(db, NullLogger<TeamService>.Instance);
    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.ValiderApresMatchReposAsync(ligue.Id, equipe.Id, [], [], 0, teamService));
}
```

- [ ] **Step 13.8 : Lancer les tests (doivent échouer)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~ValiderApresMatchRepos"`
Expected : FAIL (méthode inexistante).

- [ ] **Step 13.9 : Implémenter `ValiderApresMatchReposAsync` sur `LeagueService`**

Dans `src/BolDeSangManager/Services/LeagueService.cs`, ajouter :

```csharp
/// <summary>
/// Validation post-match de repos par un coach : applique améliorations, recrutements et achat de relances
/// sans qu'un Match ne soit nécessaire. Trace via PhaseDeReposValidation.
/// </summary>
public async Task ValiderApresMatchReposAsync(
    int ligueId,
    int teamId,
    List<(int joueurId, int skillId, bool estPrincipale)> competences,
    List<(int positionId, string nom, int numero)> nouveauxJoueurs,
    int nouvellesRelances,
    Services.TeamService teamService)
{
    var ligue = await db.Leagues.FindAsync(ligueId)
        ?? throw new InvalidOperationException("Ligue introuvable");
    if (ligue.Statut != LeagueStatus.PhaseDeRepos)
        throw new InvalidOperationException("La validation de repos n'est possible que pendant la phase de repos.");

    var dejaValide = await db.PhaseDeReposValidations
        .AnyAsync(v => v.LeagueId == ligueId && v.TeamId == teamId);
    if (dejaValide)
        throw new InvalidOperationException("Cette équipe a déjà validé sa phase de repos.");

    // Compétences (Selection Primaire/Secondaire selon flag)
    foreach (var (joueurId, skillId, estPrincipale) in competences)
    {
        var type = estPrincipale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire;
        await teamService.AppliquerAmeliorationAsync(joueurId, type, skillId: skillId, matchSheetId: null);
    }

    // Nouveaux joueurs
    foreach (var (positionId, nom, numero) in nouveauxJoueurs)
        await teamService.RecruterJoueurAsync(teamId, positionId, nom, numero);

    // Relances (coût hors-saison = 2× le prix normal)
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
```

Ajouter en haut du fichier si nécessaire :
```csharp
using BolDeSangManager.Services; // pour TeamService si pas déjà importé
```

Note : la signature utilise `Services.TeamService` qualifié au cas où le namespace est ambigu ; sinon utiliser le nom court.

- [ ] **Step 13.10 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj`
Expected : tous PASS (3 nouveaux tests + pas de régression).

- [ ] **Step 13.11 : Commit**

```bash
git add -A
git commit -m "feat: ValiderApresMatchReposAsync — achats/améliorations pendant la phase de repos"
```

---

## Task 14 : `LeagueService` — classements (`GetTop*Async`)

**Files:**
- Modify: `src/BolDeSangManager/Services/LeagueService.cs`
- Modify: `tests/BolDeSangManager.Tests/LeagueServiceTests.cs`

- [ ] **Step 14.1 : Écrire le test pour `GetTopJoueursParPspAsync`**

Ajouter à `LeagueServiceTests.cs` :

```csharp
[Fact]
public async Task GetTopJoueursParPsp_RetourneJoueursDeLaLigueOrdonnesParPSP()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c");
    var coach = DataSeeder.CreateUser("co");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "T");

    db.TeamPlayers.AddRange(
        new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Bas", Numero = 1, PointsStarPlayer = 3 },
        new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Haut", Numero = 2, PointsStarPlayer = 25 },
        new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Moyen", Numero = 3, PointsStarPlayer = 10 }
    );
    await db.SaveChangesAsync();

    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);
    var top = await service.GetTopJoueursParPspAsync(ligue.Id, limit: 2);

    Assert.Equal(2, top.Count);
    Assert.Equal("Haut", top[0].Nom);
    Assert.Equal("Moyen", top[1].Nom);
}
```

- [ ] **Step 14.2 : Lancer le test (échec)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~GetTopJoueursParPsp"`
Expected : FAIL.

- [ ] **Step 14.3 : Implémenter les classements**

Dans `LeagueService.cs`, ajouter :

```csharp
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
        .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.Completions + r.Interceptions))
        .Take(limit)
        .ToListAsync();

public record CoachClassement(ApplicationUser Coach, int PointsLigue, int Victoires, int Nuls, int Defaites);

public async Task<List<CoachClassement>> GetTopCoachsAsync(int ligueId)
{
    var equipes = await db.Teams
        .Include(t => t.Coach)
        .Where(t => t.LeagueId == ligueId)
        .ToListAsync();

    return equipes
        .GroupBy(t => t.Coach)
        .Select(g => new CoachClassement(
            g.Key,
            g.Sum(t => t.PointsLigue),
            g.Sum(t => t.NombreVictoires),
            g.Sum(t => t.NombreNuls),
            g.Sum(t => t.NombreDefaites)))
        .OrderByDescending(c => c.PointsLigue)
        .ThenByDescending(c => c.Victoires)
        .ToList();
}
```

- [ ] **Step 14.4 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj`
Expected : PASS.

- [ ] **Step 14.5 : Commit**

```bash
git add -A
git commit -m "feat: classements de ligue (top PSP, marqueurs, élimineurs, passeurs, coachs)"
```

---

## Task 15 : `LeagueService.AttribuerAwardAsync` + `GetAwardsAsync`

**Files:**
- Modify: `src/BolDeSangManager/Services/LeagueService.cs`
- Modify: `tests/BolDeSangManager.Tests/LeagueServiceTests.cs`

- [ ] **Step 15.1 : Écrire le test**

```csharp
[Fact]
public async Task AttribuerAward_CreeLeagueAward()
{
    await using var db = _factory.CreateContext();
    var (game, version) = await DataSeeder.SeedGameAsync(db);
    var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
    var commissaire = DataSeeder.CreateUser("c");
    var coach = DataSeeder.CreateUser("co");
    db.Users.AddRange(commissaire, coach);
    await db.SaveChangesAsync();
    var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);
    var equipe = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach.Id, teamType.Id, "T");
    var joueur = new TeamPlayer { TeamId = equipe.Id, PlayerPositionId = position.Id, Nom = "Star", Numero = 1, PointsStarPlayer = 50 };
    db.TeamPlayers.Add(joueur);
    await db.SaveChangesAsync();

    var service = new LeagueService(db, NullLogger<LeagueService>.Instance);
    await service.AttribuerAwardAsync(ligue.Id, AwardType.MVP, teamPlayerId: joueur.Id);

    var awards = await service.GetAwardsAsync(ligue.Id);
    Assert.Single(awards);
    Assert.Equal(AwardType.MVP, awards[0].Type);
    Assert.Equal(joueur.Id, awards[0].TeamPlayerId);
}
```

- [ ] **Step 15.2 : Lancer le test (échec)**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~AttribuerAward"`
Expected : FAIL.

- [ ] **Step 15.3 : Implémenter les méthodes**

Dans `LeagueService.cs`, ajouter :

```csharp
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
```

- [ ] **Step 15.4 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj`
Expected : PASS.

- [ ] **Step 15.5 : Commit**

```bash
git add -A
git commit -m "feat: AttribuerAward + GetAwards sur LeagueService"
```

---

## Task 16 : Tests DbSeeder

**Files:**
- Create: `tests/BolDeSangManager.Tests/DbSeederTests.cs`

> Test de sanity sur le seed : compte d'équipes, présence des skills clés, intégrité des FK de PlayerPositionSkill.

- [ ] **Step 16.1 : Créer le fichier de test**

```csharp
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class DbSeederTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public void SkillSeedData_ContientAuMoins85Skills()
    {
        var skills = SkillSeedData.GetSkills().ToList();
        Assert.True(skills.Count >= 85, $"Attendu ≥ 85 skills, obtenu {skills.Count}");
    }

    [Fact]
    public void SkillSeedData_ContientLesQuatreSkillsDungeonBowl()
    {
        var skills = SkillSeedData.GetSkills().ToList();
        var dbSpecifiques = skills.Where(s => s.GameSpecifique == GameType.DungeonBowl).ToList();

        Assert.Contains(dbSpecifiques, s => s.Nom == "Navigateur de Portail");
        Assert.Contains(dbSpecifiques, s => s.Nom == "Transmission dans la Course");
        Assert.Contains(dbSpecifiques, s => s.Nom == "Passe par un Portail");
        Assert.Contains(dbSpecifiques, s => s.Nom == "Lancer contre un Mur");
    }

    [Fact]
    public void BloodBowlTeamSeedData_Contient30Equipes()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1).ToList();
        Assert.Equal(30, teams.Count);
    }

    [Fact]
    public void DungeonBowlTeamSeedData_ContientHuitColleges()
    {
        var colleges = DungeonBowlTeamSeedData.GetColleges(1).ToList();
        Assert.Equal(8, colleges.Count);
    }

    [Fact]
    public void BloodBowlTeamSeedData_ToutesLesEquipesOntAuMoinsUnPoste()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1).ToList();
        Assert.All(teams, t => Assert.NotEmpty(t.Positions));
    }

    [Fact]
    public void DungeonBowlTeamSeedData_ToutesLesEquipesOntCoutRelance50k()
    {
        var colleges = DungeonBowlTeamSeedData.GetColleges(1).ToList();
        Assert.All(colleges, t => Assert.Equal(50_000, t.Type.CoutRelance));
    }

    [Fact]
    public void BloodBowlTeamSeedData_ChaqueEquipeAUneCategorie()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1).ToList();
        Assert.All(teams, t => Assert.True(Enum.IsDefined(typeof(TeamCategory), t.Type.Categorie)));
    }
}
```

- [ ] **Step 16.2 : Lancer les tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~DbSeederTests"`
Expected : tous PASS.

- [ ] **Step 16.3 : Commit**

```bash
git add tests/BolDeSangManager.Tests/DbSeederTests.cs
git commit -m "test: vérifications de sanity sur le seed (counts, skills DB, catégories)"
```

---

## Task 17 : Générer la migration EF consolidée

**Files:**
- Create: `src/BolDeSangManager/Data/Migrations/<timestamp>_InitialSchemaV2.cs` (généré par EF)
- Create: `src/BolDeSangManager/Data/Migrations/ApplicationDbContextModelSnapshot.cs` (généré par EF)

- [ ] **Step 17.1 : Vérifier qu'EF Tools est installé**

Run : `dotnet tool list -g | grep dotnet-ef`
Expected : `dotnet-ef X.Y.Z`

Si absent, installer : `dotnet tool install --global dotnet-ef`

- [ ] **Step 17.2 : Générer la migration**

Run :
```bash
cd src/BolDeSangManager && dotnet ef migrations add InitialSchemaV2 && cd ../..
```

Expected : fichiers créés dans `src/BolDeSangManager/Data/Migrations/<timestamp>_InitialSchemaV2.cs` + Designer + Snapshot. Le fichier doit créer toutes les tables (Identity + Games, Skills, TeamTypes, PlayerPositions, Leagues, Teams, etc. + nouvelles : PlayerImprovements, PhaseDeReposValidations, LeagueAwards).

- [ ] **Step 17.3 : Inspecter la migration**

Run : `ls src/BolDeSangManager/Data/Migrations/` puis lire le `Up()` du fichier généré.

Vérifier que les tables suivantes sont créées :
- AspNet* (Identity)
- Games, RulesVersions
- Skills (avec colonne `GameSpecifique`)
- TeamTypes (avec colonne `Categorie` enum), PlayerPositions
- Leagues, Divisions, Teams, TeamPlayers
- TeamPlayerSkills, PlayerPositionSkills, PlayerInjuries
- Matches, MatchSheets, MatchPlayerRecords
- **PlayerImprovements, PhaseDeReposValidations, LeagueAwards**
- AppConfigs

Si une table est manquante, c'est qu'une entité n'est pas enregistrée dans le DbContext (revoir Task 4).

- [ ] **Step 17.4 : Test du démarrage de l'app (sanity check)**

Run depuis la racine :
```bash
cd src/BolDeSangManager && timeout 30 dotnet run --no-launch-profile 2>&1 | head -60; cd ../..
```

Expected : démarrage sans erreur, log de `DbSeeder` indiquant le seed effectué (ou pas d'erreur EF).
La DB `src/BolDeSangManager/Data/boldesang.db` doit avoir été recréée.

Tuer le processus si toujours en cours.

- [ ] **Step 17.5 : Commit**

```bash
git add src/BolDeSangManager/Data/Migrations/
git commit -m "feat: migration consolidée InitialSchemaV2 (schéma complet refonte BB S3 / DB)"
```

---

## Task 18 : Vérification finale (intégration)

**Files:**
- None (verification only)

- [ ] **Step 18.1 : Build complet**

Run : `dotnet build`
Expected : 0 erreur, 0 warning bloquant.

- [ ] **Step 18.2 : Suite de tests complète**

Run : `dotnet test`
Expected : tous tests PASS.

- [ ] **Step 18.3 : Lancement de l'app + vérification UI rapide**

Lancer l'application :
```bash
cd src/BolDeSangManager && dotnet run
```

Dans un navigateur (`http://localhost:5129`) :
- Connexion avec l'admin auto-seedé : `commissaire@boldesang.fr` / `Commissaire123!`
- Aller dans Admin > Vérifier que les jeux Blood Bowl et Dungeon Bowl sont listés
- Créer une ligue Blood Bowl → l'écran de création doit lister les 30 équipes
- Créer une ligue Dungeon Bowl → l'écran doit lister les 8 collèges

Si une page Razor échoue (par ex. lecture de `j.NombreAmeliorations`), noter et corriger dans une tâche correctif (non incluse ici car l'UI sera adaptée en travail séparé — cf §7 du spec).

- [ ] **Step 18.4 : Commit du correctif éventuel + récapitulatif**

Si correctifs nécessaires :
```bash
git add -A
git commit -m "fix: ajuster page Razor X suite refonte modèle"
```

- [ ] **Step 18.5 : Mettre à jour la mémoire**

Ajouter à `MEMORY.md` un pointeur vers ce plan exécuté :

Modifier `C:\Users\nide3\.claude\projects\C--Users-nide3-project-BolDeSangManager\memory\MEMORY.md` pour ajouter :

```
- [Refonte BB S3 / Dungeon Bowl](../../projects/BolDeSangManager/docs/superpowers/plans/2026-05-18-refonte-base-bb-db.md) — paliers PSP, phase de repos, classements, awards, 30+8 rosters refaits
```

Plus créer un nouveau memory file `project_refonte_bb_s3.md` qui documente l'état post-refonte et les conventions (TeamCategory enum, ImprovementType, etc.).

---

## Récapitulatif

Les **18 tâches** couvrent l'intégralité du spec :

| Tâche | Section spec couverte |
|---|---|
| 1 | §4.1 Nouveaux enums |
| 2 | §4.2 Nouvelles entités |
| 3 | §4.3 Modifications d'entités existantes |
| 4 | §4.3 (DbContext) |
| 5 | §4.4 Reset DB |
| 6 | §5.1 Constantes paliers |
| 7-9 | §6 Refonte DbSeeder (skills + BB + DB) |
| 10 | §6 (orchestrateur) |
| 11 | §5.1 (PSP par jeu) |
| 12 | §5.1 (Améliorations) |
| 13 | §5.2 (Phase de repos) |
| 14 | §5.4 (Classements) |
| 15 | §5.4 (Awards) |
| 16 | §8 (Tests DbSeeder) |
| 17 | §4.4 (Migration EF) |
| 18 | §7.9-10 (vérification finale) |

**Hors-scope explicite** : adaptation des pages Razor pour les nouvelles API (ImprovementType, phase de repos, classements). Sera traité dans un plan séparé une fois ce plan exécuté et validé.
