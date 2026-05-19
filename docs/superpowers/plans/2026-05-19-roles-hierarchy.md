# Hiérarchie de rôles — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduire une hiérarchie de 4 rôles (Admin / GrandCommissaire / CommissaireDeLigue / Coach) avec un service d'autorisation centralisé et une UI pour promouvoir/révoquer les rôles.

**Architecture:** Renommer le rôle Identity existant `Commissaire` en `Admin`, ajouter `GrandCommissaire`, ajouter une entité many-to-many `LeagueCommissioner` pour les commissaires per-ligue. Service `AuthorizationService` injecté qui centralise les checks (`EstAdmin`, `EstGrandCommissaire`, `EstCommissaireDeLigue`, `PeutGererLigue`).

**Tech Stack:** .NET 9, EF Core (SQLite), Blazor Server, ASP.NET Identity, xUnit.

**Spec de référence:** `docs/superpowers/specs/2026-05-19-roles-hierarchy-design.md`

---

## Structure des fichiers

### À créer

| Fichier | Responsabilité |
|---|---|
| `src/BolDeSangManager/Data/Models/LeagueCommissioner.cs` | Entité many-to-many : un coach commissaire d'une ligue donnée |
| `src/BolDeSangManager/Services/IAuthorizationService.cs` | Interface du service d'autorisation |
| `src/BolDeSangManager/Services/AuthorizationService.cs` | Implémentation : checks de rôles + LeagueCommissioner |
| `tests/BolDeSangManager.Tests/AuthorizationServiceTests.cs` | Tests unitaires du service |

### À modifier

| Fichier | Changement |
|---|---|
| `src/BolDeSangManager/Data/Models/League.cs` | + `ICollection<LeagueCommissioner> CommissairesDeLigue` |
| `src/BolDeSangManager/Data/ApplicationDbContext.cs` | + DbSet + FK config + unique index |
| `src/BolDeSangManager/Data/DbSeeder.cs` | Migration rôles : créer Admin + GrandCommissaire, transférer Commissaire→Admin, supprimer Commissaire |
| `src/BolDeSangManager/Program.cs` | Enregistrement DI du service |
| `src/BolDeSangManager/Components/Layout/MainLayout.razor` | `Roles="Commissaire"` → `Roles="Admin"` |
| `src/BolDeSangManager/Components/Layout/NavMenu.razor` | idem |
| `src/BolDeSangManager/Components/Pages/Admin/Index.razor` | `[Authorize(Roles="Commissaire")]` → `[Authorize(Roles="Admin")]` + UI dropdown rôle |
| `src/BolDeSangManager/Components/Pages/Home.razor` | `Roles="Commissaire"` → `Roles="Admin,GrandCommissaire"` |
| `src/BolDeSangManager/Components/Pages/Ligues/Creer.razor` | idem |
| `src/BolDeSangManager/Components/Pages/Ligues/Index.razor` | idem (2 occurrences) |
| `src/BolDeSangManager/Components/Pages/Ligues/Detail.razor` | Remplacer check `CommissaireId == userId` par `IAuthorizationService.PeutGererLigueAsync` + UI section commissaires |
| `src/BolDeSangManager/Components/Pages/Matchs/Validation.razor` | `[Authorize(Roles="Commissaire")]` → check service `PeutGererLigueAsync` |
| `src/BolDeSangManager/Services/LeagueService.cs` | `EstCommissaireAsync` devient un wrapper sur `IAuthorizationService.PeutGererLigueAsync` |

### Tests touchés

| Fichier | Changement |
|---|---|
| `tests/BolDeSangManager.Tests/AuthorizationServiceTests.cs` | NOUVEAU : 7 tests sur le service |
| `tests/BolDeSangManager.Tests/Helpers/DataSeeder.cs` | Si le helper crée des utilisateurs avec rôle `Commissaire`, le mettre à jour à `Admin` |

---

## Conventions

- Commits atomiques, message en français impératif (style existant : `feat:`, `refactor:`, `fix:`, `test:`).
- TDD strict pour la logique métier (AuthorizationService).
- Build green à la fin de chaque tâche.

---

## Task 1 — Entité `LeagueCommissioner`

**Files:**
- Create: `src/BolDeSangManager/Data/Models/LeagueCommissioner.cs`
- Modify: `src/BolDeSangManager/Data/Models/League.cs`

- [ ] **Step 1.1 — Créer l'entité**

Contenu de `LeagueCommissioner.cs` :

```csharp
namespace BolDeSangManager.Data.Models;

/// <summary>
/// Relation many-to-many : un coach promu commissaire d'une ligue donnée.
/// Plusieurs commissaires possibles par ligue, un coach peut être commissaire de plusieurs ligues.
/// </summary>
public class LeagueCommissioner
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime AssigneLe { get; set; } = DateTime.UtcNow;
    public string? AssignePar { get; set; } // UserId de l'Admin/GC qui a promu
}
```

- [ ] **Step 1.2 — Modifier `League.cs`**

Dans la classe `League`, après `ICollection<LeagueAward> Awards`, ajouter :

```csharp
public ICollection<LeagueCommissioner> CommissairesDeLigue { get; set; } = [];
```

- [ ] **Step 1.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 1.4 — Commit**

```bash
git add src/BolDeSangManager/Data/Models/
git commit -m "feat: ajouter entité LeagueCommissioner"
```

---

## Task 2 — Configurer DbContext + migration EF

**Files:**
- Modify: `src/BolDeSangManager/Data/ApplicationDbContext.cs`
- Create (auto-gen): `src/BolDeSangManager/Data/Migrations/<timestamp>_AddLeagueCommissioner.cs`

- [ ] **Step 2.1 — Ajouter DbSet**

Dans `ApplicationDbContext.cs`, après les autres DbSets, ajouter :

```csharp
public DbSet<LeagueCommissioner> LeagueCommissioners => Set<LeagueCommissioner>();
```

- [ ] **Step 2.2 — Configurer FK + unique index**

À la fin de `OnModelCreating`, juste avant la fermeture de l'accolade :

```csharp
// LeagueCommissioner — many-to-many League↔User
builder.Entity<LeagueCommissioner>()
    .HasOne(lc => lc.League)
    .WithMany(l => l.CommissairesDeLigue)
    .HasForeignKey(lc => lc.LeagueId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<LeagueCommissioner>()
    .HasOne(lc => lc.User)
    .WithMany()
    .HasForeignKey(lc => lc.UserId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<LeagueCommissioner>()
    .HasIndex(lc => new { lc.LeagueId, lc.UserId })
    .IsUnique();
```

- [ ] **Step 2.3 — Générer la migration**

Run :
```bash
cd src/BolDeSangManager && dotnet ef migrations add AddLeagueCommissioner && cd ../..
```

Verifier que le fichier `<timestamp>_AddLeagueCommissioner.cs` est créé et contient :
- `CreateTable` pour `LeagueCommissioners`
- Index unique sur `(LeagueId, UserId)`
- Foreign keys vers `Leagues` (cascade) et `AspNetUsers` (restrict)

- [ ] **Step 2.4 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 2.5 — Commit**

```bash
git add src/BolDeSangManager/Data/ApplicationDbContext.cs src/BolDeSangManager/Data/Migrations/
git commit -m "feat: DbContext + migration AddLeagueCommissioner"
```

---

## Task 3 — `IAuthorizationService` + implémentation

**Files:**
- Create: `src/BolDeSangManager/Services/IAuthorizationService.cs`
- Create: `src/BolDeSangManager/Services/AuthorizationService.cs`
- Modify: `src/BolDeSangManager/Program.cs`

- [ ] **Step 3.1 — Créer l'interface**

Contenu de `IAuthorizationService.cs` :

```csharp
namespace BolDeSangManager.Services;

public interface IAuthorizationService
{
    /// <summary>Returns true si l'utilisateur a le rôle Identity "Admin".</summary>
    Task<bool> EstAdminAsync(string userId);

    /// <summary>Returns true si l'utilisateur a le rôle Identity "GrandCommissaire".</summary>
    Task<bool> EstGrandCommissaireAsync(string userId);

    /// <summary>Returns true si l'utilisateur est un LeagueCommissioner de la ligue donnée.</summary>
    Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId);

    /// <summary>Returns true si l'utilisateur peut gérer cette ligue (Admin OU GrandCommissaire OU CommissaireDeLigue de cette ligue).</summary>
    Task<bool> PeutGererLigueAsync(string userId, int ligueId);

    /// <summary>Returns true si l'utilisateur peut éditer les données de jeu (Admin OU GrandCommissaire).</summary>
    Task<bool> PeutEditerDonneesAsync(string userId);

    /// <summary>Returns true si l'utilisateur peut modifier les Paramètres système (Admin uniquement).</summary>
    Task<bool> PeutGererSettingsAsync(string userId);
}
```

- [ ] **Step 3.2 — Créer l'implémentation**

Contenu de `AuthorizationService.cs` :

```csharp
using BolDeSangManager.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class AuthorizationService(
    ApplicationDbContext db,
    UserManager<Data.ApplicationUser> userManager)
    : IAuthorizationService
{
    public async Task<bool> EstAdminAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.IsInRoleAsync(user, "Admin");
    }

    public async Task<bool> EstGrandCommissaireAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.IsInRoleAsync(user, "GrandCommissaire");
    }

    public async Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId)
    {
        if (string.IsNullOrEmpty(userId) || ligueId <= 0) return false;
        return await db.LeagueCommissioners
            .AnyAsync(lc => lc.UserId == userId && lc.LeagueId == ligueId);
    }

    public async Task<bool> PeutGererLigueAsync(string userId, int ligueId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        if (await EstAdminAsync(userId)) return true;
        if (await EstGrandCommissaireAsync(userId)) return true;
        return await EstCommissaireDeLigueAsync(userId, ligueId);
    }

    public async Task<bool> PeutEditerDonneesAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        if (await EstAdminAsync(userId)) return true;
        return await EstGrandCommissaireAsync(userId);
    }

    public Task<bool> PeutGererSettingsAsync(string userId)
        => EstAdminAsync(userId);
}
```

- [ ] **Step 3.3 — Enregistrer le service en DI**

Modifier `src/BolDeSangManager/Program.cs`. Find the section where services are registered (look for `builder.Services.AddScoped<LeagueService>` or similar) and add :

```csharp
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
```

If you don't see existing service registrations, use `grep -n "AddScoped" src/BolDeSangManager/Program.cs` to find them.

- [ ] **Step 3.4 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 3.5 — Commit**

```bash
git add src/BolDeSangManager/Services/IAuthorizationService.cs src/BolDeSangManager/Services/AuthorizationService.cs src/BolDeSangManager/Program.cs
git commit -m "feat: IAuthorizationService centralise les checks de rôles"
```

---

## Task 4 — Tests `AuthorizationService`

**Files:**
- Create: `tests/BolDeSangManager.Tests/AuthorizationServiceTests.cs`

TDD strict — écrire chaque test, voir qu'il échoue (s'il fait référence à un comportement non-implémenté), implémenter / corriger, voir qu'il passe.

> **Note** : `AuthorizationService` est déjà implémenté en Task 3. Les tests servent ici de filet de sécurité, donc on peut écrire les 7 tests d'un coup et vérifier qu'ils passent.

- [ ] **Step 4.1 — Créer le fichier de tests**

Contenu :

```csharp
using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class AuthorizationServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<(AuthorizationService service, UserManager<ApplicationUser> um, ApplicationDbContext db)>
        CreateServiceAsync()
    {
        var db = _factory.CreateContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "GrandCommissaire", "Coach" })
        {
            if (!await rm.RoleExistsAsync(role))
                await rm.CreateAsync(new IdentityRole(role));
        }

        var service = new AuthorizationService(db, um);
        return (service, um, db);
    }

    private async Task<ApplicationUser> CreateUserWithRoleAsync(UserManager<ApplicationUser> um, string suffix, string role)
    {
        var user = new ApplicationUser
        {
            UserName = $"u{suffix}@test.fr",
            Email = $"u{suffix}@test.fr",
            EmailConfirmed = true,
            PseudoCoach = $"User{suffix}"
        };
        await um.CreateAsync(user, "Password123!");
        await um.AddToRoleAsync(user, role);
        return user;
    }

    [Fact]
    public async Task EstAdmin_ReturnsTrueForAdminUser()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "admin", "Admin");
        Assert.True(await svc.EstAdminAsync(admin.Id));
    }

    [Fact]
    public async Task EstAdmin_ReturnsFalseForCoach()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "coach", "Coach");
        Assert.False(await svc.EstAdminAsync(coach.Id));
    }

    [Fact]
    public async Task EstGrandCommissaire_ReturnsTrueForGCUser()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var gc = await CreateUserWithRoleAsync(um, "gc", "GrandCommissaire");
        Assert.True(await svc.EstGrandCommissaireAsync(gc.Id));
    }

    [Fact]
    public async Task EstCommissaireDeLigue_TrueQuandLeagueCommissionerExiste()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "lc1", "Coach");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var commissaire = await CreateUserWithRoleAsync(um, "creator", "Admin");
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, commissaire.Id);

        db.LeagueCommissioners.Add(new LeagueCommissioner
        {
            LeagueId = ligue.Id,
            UserId = coach.Id,
            AssignePar = commissaire.Id
        });
        await db.SaveChangesAsync();

        Assert.True(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue.Id));
    }

    [Fact]
    public async Task EstCommissaireDeLigue_FalseDansAutreLigue()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "lc2", "Coach");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var admin = await CreateUserWithRoleAsync(um, "admin2", "Admin");
        var ligue1 = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);
        var ligue2 = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        db.LeagueCommissioners.Add(new LeagueCommissioner { LeagueId = ligue1.Id, UserId = coach.Id });
        await db.SaveChangesAsync();

        Assert.True(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue1.Id));
        Assert.False(await svc.EstCommissaireDeLigueAsync(coach.Id, ligue2.Id));
    }

    [Fact]
    public async Task PeutGererLigue_AdminAlwaysTrue()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "a3", "Admin");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        Assert.True(await svc.PeutGererLigueAsync(admin.Id, ligue.Id));
    }

    [Fact]
    public async Task PeutGererLigue_CoachSansPromotionFalse()
    {
        var (svc, um, db) = await CreateServiceAsync();
        var coach = await CreateUserWithRoleAsync(um, "c4", "Coach");
        var admin = await CreateUserWithRoleAsync(um, "a4", "Admin");
        var (game, version) = await DataSeeder.SeedGameAsync(db);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, version.Id, admin.Id);

        Assert.False(await svc.PeutGererLigueAsync(coach.Id, ligue.Id));
    }

    [Fact]
    public async Task PeutEditerDonnees_AdminEtGCTrue_CoachFalse()
    {
        var (svc, um, _) = await CreateServiceAsync();
        var admin = await CreateUserWithRoleAsync(um, "ed-a", "Admin");
        var gc = await CreateUserWithRoleAsync(um, "ed-gc", "GrandCommissaire");
        var coach = await CreateUserWithRoleAsync(um, "ed-c", "Coach");

        Assert.True(await svc.PeutEditerDonneesAsync(admin.Id));
        Assert.True(await svc.PeutEditerDonneesAsync(gc.Id));
        Assert.False(await svc.PeutEditerDonneesAsync(coach.Id));
    }
}
```

- [ ] **Step 4.2 — Lancer les tests**

Run :
```bash
dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj --filter "FullyQualifiedName~AuthorizationServiceTests"
```
Expected : 8 tests PASS.

> Si tu vois `Assert.Empty` ou `Assert.Single` warnings sur `Improvements`, ignore — ils viennent d'autres tests.

- [ ] **Step 4.3 — Test suite complète**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj 2>&1 | tail -3`
Expected : 119 + 8 = 127 tests PASS.

- [ ] **Step 4.4 — Commit**

```bash
git add tests/BolDeSangManager.Tests/AuthorizationServiceTests.cs
git commit -m "test: AuthorizationService (8 tests sur les 6 méthodes du service)"
```

---

## Task 5 — Migration des rôles dans `DbSeeder`

**Files:**
- Modify: `src/BolDeSangManager/Data/DbSeeder.cs`

- [ ] **Step 5.1 — Modifier `SeedRolesAsync`**

Dans `src/BolDeSangManager/Data/DbSeeder.cs`, la méthode actuelle ressemble à :

```csharp
private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    foreach (var role in new[] { "Commissaire", "Coach" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
```

Remplacer entièrement par :

```csharp
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
```

- [ ] **Step 5.2 — Modifier `SeedAdminUserAsync`**

Trouver la ligne :
```csharp
await userManager.AddToRoleAsync(admin, "Commissaire");
```
Remplacer par :
```csharp
await userManager.AddToRoleAsync(admin, "Admin");
```

- [ ] **Step 5.3 — Adapter l'appelant `SeedAsync`**

Le mock actuel passait juste `roleManager`. Maintenant on a besoin du `userManager` aussi. Trouver la ligne :
```csharp
await SeedRolesAsync(roleManager);
```
Remplacer par :
```csharp
await SeedRolesAsync(roleManager, userManager);
```

- [ ] **Step 5.4 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 5.5 — Test smoke (run app brièvement)**

```bash
rm -f src/BolDeSangManager/Data/boldesang.db
cd src/BolDeSangManager && timeout 20 dotnet run --no-launch-profile 2>&1 | head -40 ; cd ../..
```
Expected : démarrage propre, rôles `Admin`/`GrandCommissaire`/`Coach` créés, le compte admin auto-seedé est en `Admin`.

Vérifier après arrêt via SQL :
```bash
# Si sqlite3 dispo
sqlite3 src/BolDeSangManager/Data/boldesang.db "SELECT Name FROM AspNetRoles;"
```
Expected : `Admin`, `Coach`, `GrandCommissaire`.

Sinon, faire confiance au log de démarrage.

- [ ] **Step 5.6 — Commit**

```bash
git add src/BolDeSangManager/Data/DbSeeder.cs
git commit -m "refactor: migrer rôle Commissaire vers Admin + créer GrandCommissaire"
```

---

## Task 6 — Migrer les pages avec rôle `Admin` simple

**Files (5 occurrences) :**
- Modify: `src/BolDeSangManager/Components/Layout/MainLayout.razor`
- Modify: `src/BolDeSangManager/Components/Layout/NavMenu.razor`
- Modify: `src/BolDeSangManager/Components/Pages/Admin/Index.razor`

Ces 3 pages restent **réservées à l'Admin uniquement** (Admin > Paramètres système et menus admin).

- [ ] **Step 6.1 — `MainLayout.razor`**

Ligne 21 : remplacer `<AuthorizeView Roles="Commissaire" Context="adminCtx">` par `<AuthorizeView Roles="Admin" Context="adminCtx">`.

- [ ] **Step 6.2 — `NavMenu.razor`**

Ligne 42 : remplacer `<AuthorizeView Roles="Commissaire">` par `<AuthorizeView Roles="Admin">`.

- [ ] **Step 6.3 — `Admin/Index.razor`**

Ligne 3 : remplacer `@attribute [Authorize(Roles = "Commissaire")]` par `@attribute [Authorize(Roles = "Admin")]`.

- [ ] **Step 6.4 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 6.5 — Commit**

```bash
git add src/BolDeSangManager/Components/Layout/ src/BolDeSangManager/Components/Pages/Admin/Index.razor
git commit -m "refactor: pages Admin réservées au rôle Admin"
```

---

## Task 7 — Migrer les pages partagées Admin+GrandCommissaire

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Home.razor`
- Modify: `src/BolDeSangManager/Components/Pages/Ligues/Creer.razor`
- Modify: `src/BolDeSangManager/Components/Pages/Ligues/Index.razor` (2 occurrences)

Ces pages permettent de **créer des ligues** et sont accessibles à Admin OU GrandCommissaire.

- [ ] **Step 7.1 — `Home.razor`**

Ligne 22 : remplacer `<AuthorizeView Roles="Commissaire" Context="commissaireCtx">` par `<AuthorizeView Roles="Admin,GrandCommissaire" Context="commissaireCtx">`.

- [ ] **Step 7.2 — `Ligues/Creer.razor`**

Ligne 3 : remplacer `@attribute [Authorize(Roles = "Commissaire")]` par `@attribute [Authorize(Roles = "Admin,GrandCommissaire")]`.

- [ ] **Step 7.3 — `Ligues/Index.razor` (1ʳᵉ occurrence)**

Ligne 15 : remplacer `<AuthorizeView Roles="Commissaire">` par `<AuthorizeView Roles="Admin,GrandCommissaire">`.

- [ ] **Step 7.4 — `Ligues/Index.razor` (2ᵉ occurrence)**

Ligne 87 : remplacer `<AuthorizeView Roles="Commissaire" Context="commCtx">` par `<AuthorizeView Roles="Admin,GrandCommissaire" Context="commCtx">`.

- [ ] **Step 7.5 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 7.6 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Home.razor src/BolDeSangManager/Components/Pages/Ligues/
git commit -m "refactor: pages Home + Ligues accessibles Admin et GrandCommissaire"
```

---

## Task 8 — Refactor `Ligues/Detail.razor` avec check service

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Ligues/Detail.razor`

L'actuel check est inline (ligne 209) : `_estCommissaire = _ligue?.CommissaireId == userId;`. Il faut le remplacer par un appel au service `IAuthorizationService.PeutGererLigueAsync`.

- [ ] **Step 8.1 — Injecter le service**

En haut de `Detail.razor`, après les autres `@inject`, ajouter :

```razor
@inject BolDeSangManager.Services.IAuthorizationService Auth
```

- [ ] **Step 8.2 — Remplacer le check de commissaire**

Trouver la ligne 209 (utiliser `grep -n "_estCommissaire = _ligue" src/BolDeSangManager/Components/Pages/Ligues/Detail.razor`) :

```csharp
_estCommissaire = _ligue?.CommissaireId == userId;
```

Remplacer par :

```csharp
_estCommissaire = _ligue is not null && await Auth.PeutGererLigueAsync(userId, _ligue.Id);
```

Si la méthode `OnInitializedAsync` ou similaire n'est pas async, la rendre async (la signature `async Task OnInitializedAsync()`).

- [ ] **Step 8.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 8.4 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Ligues/Detail.razor
git commit -m "refactor: Detail.razor utilise IAuthorizationService.PeutGererLigueAsync"
```

---

## Task 9 — Sécuriser `Matchs/Validation.razor`

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Matchs/Validation.razor`

Cette page valide les feuilles de match. Actuellement `[Authorize(Roles="Commissaire")]`. Devrait être ouverte à toute personne pouvant gérer la ligue en question (Admin, GC, CommissaireDeLigue de la ligue du match).

- [ ] **Step 9.1 — Modifier l'attribute**

Ligne 3 : remplacer `@attribute [Authorize(Roles = "Commissaire")]` par `@attribute [Authorize]` (tout user connecté).

> Le check fin (qui peut valider quoi) se fait dans le code-behind via `Auth.PeutGererLigueAsync`.

- [ ] **Step 9.2 — Ajouter check dans le code-behind**

Injecter le service en haut du fichier :
```razor
@inject BolDeSangManager.Services.IAuthorizationService Auth
@inject AuthenticationStateProvider AuthProvider
@inject NavigationManager Nav
```

(Si certains sont déjà présents, ne pas dupliquer.)

Trouver `OnInitializedAsync` ou la méthode de chargement. Au début, ajouter un check :

```csharp
var authState = await AuthProvider.GetAuthenticationStateAsync();
var userId = authState.User.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value ?? "";

// Charger le match pour récupérer son LigueId
var match = await MatchService.GetMatchAsync(MatchId); // MatchId est un parameter de la page
if (match?.Division?.LeagueId is null
    || !await Auth.PeutGererLigueAsync(userId, match.Division.LeagueId))
{
    Nav.NavigateTo("/", forceLoad: true);
    return;
}
```

> Le code exact dépend de la structure existante de Validation.razor. Adapter en lisant le fichier d'abord. Si la méthode ne fait pas ça correctement, l'objectif est : un utilisateur qui n'est pas Admin/GC/CL de la ligue est redirigé hors de la page.

- [ ] **Step 9.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 9.4 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Matchs/Validation.razor
git commit -m "refactor: Validation.razor restreinte aux gestionnaires de la ligue"
```

---

## Task 10 — `LeagueService.EstCommissaireAsync` → délégation au service auth

**Files:**
- Modify: `src/BolDeSangManager/Services/LeagueService.cs`

Le service `LeagueService` a déjà une méthode `EstCommissaireAsync(int ligueId, string userId)`. Il faut qu'elle utilise le nouveau service d'autorisation pour rester cohérente.

- [ ] **Step 10.1 — Injecter le service**

Dans `LeagueService.cs`, modifier la signature de constructeur (primary constructor) :

```csharp
public class LeagueService(
    ApplicationDbContext db,
    ILogger<LeagueService> logger,
    IAuthorizationService authService)
```

Ajouter le `using` en haut si besoin : `using BolDeSangManager.Services;` (déjà présent puisque le fichier est dans le même namespace, donc skip).

- [ ] **Step 10.2 — Modifier `EstCommissaireAsync`**

Trouver la méthode existante (utiliser `grep -n "EstCommissaireAsync" src/BolDeSangManager/Services/LeagueService.cs`). Remplacer son corps par :

```csharp
public async Task<bool> EstCommissaireAsync(int ligueId, string userId)
    => await authService.PeutGererLigueAsync(userId, ligueId);
```

> La méthode garde son nom historique pour ne pas casser les appelants, mais sa logique délègue au service centralisé.

- [ ] **Step 10.3 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 10.4 — Tests**

Run : `dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj 2>&1 | tail -3`
Expected : tous tests passent (127 incluant les 8 nouveaux).

- [ ] **Step 10.5 — Commit**

```bash
git add src/BolDeSangManager/Services/LeagueService.cs
git commit -m "refactor: LeagueService.EstCommissaireAsync délègue à IAuthorizationService"
```

---

## Task 11 — UI Admin > Utilisateurs : dropdown rôle global

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Admin/Index.razor`

L'onglet "Utilisateurs" existe déjà. Il faut ajouter un dropdown pour changer le rôle de chaque utilisateur. Visible Admin uniquement (la page est déjà `@attribute [Authorize(Roles = "Admin")]` depuis Task 6).

- [ ] **Step 11.1 — Lire la structure existante**

Run :
```bash
grep -n "Utilisateurs\|MudTable\|UserManager\|GetUsers" src/BolDeSangManager/Components/Pages/Admin/Index.razor | head -20
```

Localiser la section qui liste les utilisateurs.

- [ ] **Step 11.2 — Injecter `UserManager` si pas déjà fait**

En haut du fichier, après les autres `@inject` :
```razor
@inject UserManager<ApplicationUser> UserManager
```

Ajouter les `using` si nécessaire :
```razor
@using BolDeSangManager.Data
@using Microsoft.AspNetCore.Identity
```

- [ ] **Step 11.3 — Ajouter colonne "Rôle" dans la table**

Dans la table des utilisateurs, ajouter une colonne :

```razor
<MudTd DataLabel="Rôle">
    @if (_rolesById.TryGetValue(user.Id, out var roleActuel))
    {
        <MudSelect T="string" Value="@roleActuel"
                   ValueChanged="@(async (string r) => await ChangerRoleAsync(user, r))"
                   Dense="true" Variant="Variant.Outlined" Margin="Margin.Dense"
                   Disabled="@(user.Id == _currentUserId)">
            <MudSelectItem Value="@("Coach")">Coach</MudSelectItem>
            <MudSelectItem Value="@("GrandCommissaire")">Grand Commissaire</MudSelectItem>
            <MudSelectItem Value="@("Admin")">Admin</MudSelectItem>
        </MudSelect>
    }
    else
    {
        <MudText>—</MudText>
    }
</MudTd>
```

- [ ] **Step 11.4 — Code-behind : chargement des rôles + méthode `ChangerRoleAsync`**

Dans le bloc `@code { }`, ajouter :

```csharp
Dictionary<string, string> _rolesById = new();
string _currentUserId = "";

protected override async Task OnInitializedAsync()
{
    var auth = await AuthProvider.GetAuthenticationStateAsync();
    _currentUserId = auth.User.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value ?? "";
    await ChargerRolesAsync();
}

async Task ChargerRolesAsync()
{
    _rolesById.Clear();
    var users = await UserManager.Users.ToListAsync();
    foreach (var user in users)
    {
        var roles = await UserManager.GetRolesAsync(user);
        _rolesById[user.Id] = roles.FirstOrDefault() ?? "Coach";
    }
}

async Task ChangerRoleAsync(ApplicationUser user, string nouveauRole)
{
    var rolesActuels = await UserManager.GetRolesAsync(user);
    foreach (var r in rolesActuels)
        await UserManager.RemoveFromRoleAsync(user, r);
    await UserManager.AddToRoleAsync(user, nouveauRole);
    _rolesById[user.Id] = nouveauRole;
    Snackbar.Add($"Rôle de {user.Email} changé en {nouveauRole}", Severity.Success);
    StateHasChanged();
}
```

> Si la page utilise déjà un `Snackbar` ou `AuthProvider`, ne pas dupliquer les injects. Sinon, ajouter `@inject ISnackbar Snackbar` et `@inject AuthenticationStateProvider AuthProvider`.

- [ ] **Step 11.5 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 11.6 — Commit**

```bash
git add src/BolDeSangManager/Components/Pages/Admin/Index.razor
git commit -m "feat: dropdown rôle global dans Admin > Utilisateurs"
```

---

## Task 12 — UI Détail Ligue : section commissaires de ligue

**Files:**
- Modify: `src/BolDeSangManager/Components/Pages/Ligues/Detail.razor`

Ajouter une section visible quand `Statut >= EnCours` (saison régulière démarrée). Affiche les commissaires de ligue actuels, permet à un Admin/GC d'en ajouter/retirer.

- [ ] **Step 12.1 — Injecter `UserManager`**

En haut de `Detail.razor`, après les autres `@inject` :
```razor
@inject UserManager<ApplicationUser> UserManager
```

Si pas déjà présent :
```razor
@using BolDeSangManager.Data
@using Microsoft.AspNetCore.Identity
```

- [ ] **Step 12.2 — Charger les commissaires de ligue**

Dans le `@code` block, ajouter :

```csharp
List<LeagueCommissioner> _commissaires = [];
bool _peutGererLigue;

async Task ChargerCommissairesAsync()
{
    if (_ligue is null) return;
    _commissaires = await DbContext.LeagueCommissioners
        .Include(lc => lc.User)
        .Where(lc => lc.LeagueId == _ligue.Id)
        .ToListAsync();
    _peutGererLigue = await Auth.PeutGererLigueAsync(_currentUserId, _ligue.Id);
}
```

> Note : `DbContext` doit être injecté si pas déjà. Voir si la page utilise déjà un service pour ses requêtes (probablement `LeagueService`). Si oui, créer une nouvelle méthode `LeagueService.GetCommissairesDeLigueAsync(int ligueId)` plutôt que d'injecter DbContext directement.

Pour rester simple, on ajoute la méthode dans `LeagueService` :

```csharp
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
}

public async Task RetirerCommissaireDeLigueAsync(int ligueId, string userId)
{
    var entry = await db.LeagueCommissioners
        .FirstOrDefaultAsync(lc => lc.LeagueId == ligueId && lc.UserId == userId);
    if (entry is null) return;
    db.LeagueCommissioners.Remove(entry);
    await db.SaveChangesAsync();
}
```

Ajouter ces 3 méthodes au fichier `src/BolDeSangManager/Services/LeagueService.cs`.

- [ ] **Step 12.3 — UI : section commissaires dans Detail.razor**

À l'emplacement approprié dans la page (typiquement après la section "Équipes" et avant "Matchs", utiliser le contexte pour décider), ajouter :

```razor
@if (_ligue is not null && _ligue.Statut >= LeagueStatus.EnCours)
{
    <MudPaper Class="pa-3 mb-3" Elevation="1">
        <MudStack Row="true" AlignItems="AlignItems.Center" Justify="Justify.SpaceBetween">
            <MudText Typo="Typo.h6">Commissaires de ligue</MudText>
            @if (_peutGererLigue)
            {
                <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small"
                           StartIcon="@Icons.Material.Filled.PersonAdd"
                           OnClick="OuvrirModalePromotion">
                    Promouvoir un coach
                </MudButton>
            }
        </MudStack>
        @if (_commissaires.Any())
        {
            <MudList T="LeagueCommissioner" Dense="true" Class="mt-2">
                @foreach (var c in _commissaires)
                {
                    <MudListItem T="LeagueCommissioner" Value="@c">
                        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                            <MudIcon Icon="@Icons.Material.Filled.SupervisorAccount" Size="Size.Small" />
                            <MudText Typo="Typo.body2">@(c.User?.PseudoCoach ?? c.User?.Email ?? "—")</MudText>
                            @if (_peutGererLigue)
                            {
                                <MudIconButton Icon="@Icons.Material.Filled.RemoveCircleOutline"
                                               Size="Size.Small" Color="Color.Error"
                                               OnClick="@(() => RetirerCommissaireAsync(c.UserId))" />
                            }
                        </MudStack>
                    </MudListItem>
                }
            </MudList>
        }
        else
        {
            <MudText Typo="Typo.caption" Class="mt-2">Aucun commissaire de ligue pour l'instant.</MudText>
        }
    </MudPaper>
}
```

- [ ] **Step 12.4 — Modale de promotion**

Toujours dans `Detail.razor`, ajouter en bas du markup une modale (MudDialog ou MudPopover ou simple paper conditionnel). Approche simple : utiliser un `MudDialog` via `IDialogService`.

D'abord, injecter `IDialogService` :
```razor
@inject IDialogService DialogService
```

Ajouter au code :

```csharp
async Task OuvrirModalePromotion()
{
    var coaches = await GetCoachesDisponiblesAsync();
    var parameters = new DialogParameters<PromotionDialog>
    {
        { x => x.LigueId, _ligue!.Id },
        { x => x.Coaches, coaches },
    };
    var dialog = await DialogService.ShowAsync<PromotionDialog>("Promouvoir un coach", parameters);
    var result = await dialog.Result;
    if (result is not null && !result.Canceled)
        await ChargerCommissairesAsync();
}

async Task<List<ApplicationUser>> GetCoachesDisponiblesAsync()
{
    // Les coaches de la ligue qui ne sont pas déjà commissaires
    var teams = _ligue?.Divisions?.SelectMany(d => d.Equipes).Select(e => e.Coach).Distinct().ToList() ?? [];
    var dejaPromus = _commissaires.Select(c => c.UserId).ToHashSet();
    return teams.Where(c => c is not null && !dejaPromus.Contains(c.Id)).ToList();
}

async Task RetirerCommissaireAsync(string userId)
{
    if (_ligue is null) return;
    await LeagueService.RetirerCommissaireDeLigueAsync(_ligue.Id, userId);
    await ChargerCommissairesAsync();
    Snackbar.Add("Commissaire retiré", Severity.Success);
}
```

- [ ] **Step 12.5 — Créer le composant `PromotionDialog`**

Fichier : `src/BolDeSangManager/Components/Pages/Ligues/PromotionDialog.razor`

```razor
@using BolDeSangManager.Data
@using BolDeSangManager.Services
@inject LeagueService LeagueService
@inject AuthenticationStateProvider AuthProvider
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        @if (Coaches.Any())
        {
            <MudText Class="mb-2">Sélectionne les coaches à promouvoir commissaires de cette ligue :</MudText>
            @foreach (var c in Coaches)
            {
                <MudCheckBox @bind-Value="_selection[c.Id]" Label="@(c.PseudoCoach ?? c.Email)" />
            }
        }
        else
        {
            <MudText>Aucun coach disponible (tous déjà commissaires ou pas d'équipe inscrite).</MudText>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Annuler">Annuler</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   OnClick="Valider" Disabled="@(!_selection.Values.Any(v => v))">
            Promouvoir
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int LigueId { get; set; }
    [Parameter] public List<ApplicationUser> Coaches { get; set; } = [];

    readonly Dictionary<string, bool> _selection = new();

    protected override void OnInitialized()
    {
        foreach (var c in Coaches) _selection[c.Id] = false;
    }

    void Annuler() => MudDialog.Cancel();

    async Task Valider()
    {
        var auth = await AuthProvider.GetAuthenticationStateAsync();
        var assignePar = auth.User.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value ?? "";

        foreach (var (userId, selected) in _selection)
        {
            if (!selected) continue;
            await LeagueService.PromouvoirCommissaireDeLigueAsync(LigueId, userId, assignePar);
        }

        Snackbar.Add("Promotion effectuée", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }
}
```

- [ ] **Step 12.6 — Appeler `ChargerCommissairesAsync` dans `OnInitializedAsync`**

Trouver la méthode `OnInitializedAsync` de `Detail.razor` et ajouter à la fin :

```csharp
await ChargerCommissairesAsync();
```

- [ ] **Step 12.7 — Build**

Run : `dotnet build src/BolDeSangManager/BolDeSangManager.csproj`
Expected : 0 errors.

- [ ] **Step 12.8 — Test smoke**

```bash
rm -f src/BolDeSangManager/Data/boldesang.db
cd src/BolDeSangManager && timeout 25 dotnet run --no-launch-profile 2>&1 | head -40 ; cd ../..
```

Vérifier dans le log : pas d'erreur SQL au démarrage.

- [ ] **Step 12.9 — Commit**

```bash
git add -A
git commit -m "feat: section Commissaires de Ligue sur Detail.razor + modale promotion"
```

---

## Task 13 — Vérification d'intégration

**Files:**
- None (verification only)

- [ ] **Step 13.1 — Build complet**

Run : `dotnet build`
Expected : 0 errors.

- [ ] **Step 13.2 — Test suite complète**

Run : `dotnet test`
Expected : tous tests passent (127).

- [ ] **Step 13.3 — Grep final pour vérifier qu'il ne reste plus de `Roles="Commissaire"`**

Run :
```bash
grep -rn 'Roles\s*=\s*"Commissaire"' src/
```
Expected : aucun résultat.

- [ ] **Step 13.4 — Mémoire**

Créer `C:\Users\nide3\.claude\projects\C--Users-nide3-project-BolDeSangManager\memory\reference_roles_hierarchie.md` :

```markdown
---
name: reference-roles-hierarchie
description: Hiérarchie de 4 rôles (Admin/GrandCommissaire/CommissaireDeLigue/Coach) + service IAuthorizationService
metadata:
  type: reference
---

Le projet utilise 4 niveaux d'autorisation :
- **Admin** (Identity role) : tout + Paramètres système. Compte original (config `BolDeSang:AdminEmail`).
- **GrandCommissaire** (Identity role) : créer/gérer ligues + édition de données (page admin/donnees, cf Spec 2).
- **CommissaireDeLigue** (relation per-league via `LeagueCommissioners` table) : gérer SA ligue uniquement.
- **Coach** (Identity role) : rejoindre ligues et y jouer.

**Service centralisé** : `IAuthorizationService` (`Services/`) avec `EstAdmin/EstGrandCommissaire/EstCommissaireDeLigue/PeutGererLigue/PeutEditerDonnees/PeutGererSettings`. Toujours préférer ce service aux checks `[Authorize(Roles="X")]` quand un check fin per-ligue est nécessaire.

**Migration historique** : le rôle `Commissaire` (anciennement utilisé) a été renommé en `Admin` au `DbSeeder.SeedRolesAsync` (migration idempotente).

**Promotion** : Admin via Admin > Utilisateurs (dropdown rôle). Coach → CommissaireDeLigue via Detail.razor > section "Commissaires de Ligue".
```

Mettre à jour `MEMORY.md` pour ajouter cette ligne :
```
- [Hiérarchie de rôles](reference_roles_hierarchie.md) — 4 niveaux : Admin/GC/CL/Coach + service IAuthorizationService
```

- [ ] **Step 13.5 — Commit du mémo (hors repo, pas de commit)**

(La mémoire est dans `~/.claude/`, hors du repo git du projet. Pas de commit requis.)

---

## Récapitulatif

**13 tâches** :
1. Entité LeagueCommissioner
2. DbContext + migration EF
3. IAuthorizationService + impl + DI
4. Tests AuthorizationService (8 tests)
5. Migration rôles dans DbSeeder
6. Pages Admin only (3 fichiers)
7. Pages Admin+GC (3 fichiers, 4 occurrences)
8. Detail.razor — service check
9. Validation.razor — accès aux gestionnaires
10. LeagueService.EstCommissaireAsync délégué
11. UI Admin > Utilisateurs — dropdown rôle
12. UI Detail > Commissaires de Ligue + modale promotion
13. Vérification finale + mémoire

**Couverture spec** :
- §3 Modèle (entité + migration) → Tasks 1, 2
- §4 Service d'autorisation → Task 3
- §5 Migration pages existantes → Tasks 6, 7, 8, 9, 10
- §6 UI promotion → Tasks 11, 12
- §7 Tests → Task 4
- §3.3 Migration data DbSeeder → Task 5
- §8 Plan migration → ordre des tâches
