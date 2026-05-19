# Hiérarchie de rôles : Admin / GrandCommissaire / CommissaireDeLigue / Coach

**Date** : 2026-05-19
**Statut** : Design validé, prêt pour plan d'implémentation
**Prérequis** : aucun — fondation pour le Spec 2 (page d'édition de données)

---

## 1. Objectif

Définir 4 niveaux d'autorisation pour permettre à l'application d'avoir une gouvernance plus fine :

- **Admin** : tout (incluant Paramètres système)
- **Grand Commissaire** : créer/gérer toutes les ligues + éditer les données de jeu
- **Commissaire de Ligue** : coach promu pour gérer une ligue spécifique (validation matchs, awards, phase repos, playoffs)
- **Coach** : utilisateur de base (rejoindre ligues, gérer équipe)

## 2. Hors-scope

- L'auto-promotion (un coach demande sa promotion).
- Audit log des changements de rôle (à ajouter ultérieurement si besoin).
- Délégation temporaire (un commissaire passe le flambeau).

## 3. Modèle de données

### 3.1 Rôles Identity

| Rôle Identity | Statut |
|---|---|
| `Admin` | **Renommé** depuis `Commissaire`. Tous les `Commissaire` existants deviennent `Admin`. |
| `GrandCommissaire` | **Nouveau**. Aucun utilisateur assigné par défaut — l'Admin promeut manuellement. |
| `Coach` | Inchangé. Assigné automatiquement à l'inscription. |

> `CommissaireDeLigue` n'est **pas** un rôle Identity. C'est une relation per-league.

### 3.2 Nouvelle entité `LeagueCommissioner`

```csharp
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

Index unique sur `(LeagueId, UserId)`.

Sur `League`, ajouter :
```csharp
public ICollection<LeagueCommissioner> CommissairesDeLigue { get; set; } = [];
```

### 3.3 Migration EF

Migration `AddLeagueCommissioner` :
- CREATE TABLE `LeagueCommissioners`
- Index unique `(LeagueId, UserId)`
- FK Cascade sur LeagueId, Restrict sur UserId

Migration data (au `DbSeeder.SeedRolesAsync`, exécuté à chaque démarrage) :
1. Créer le rôle `Admin` s'il n'existe pas (`RoleManager.CreateAsync(new IdentityRole("Admin"))`)
2. Créer le rôle `GrandCommissaire` s'il n'existe pas
3. Si le rôle `Commissaire` existe encore : pour chaque utilisateur ayant ce rôle, ajouter le rôle `Admin` (`userManager.AddToRoleAsync`) puis retirer `Commissaire` (`RemoveFromRoleAsync`). Une fois plus aucun utilisateur n'a `Commissaire`, supprimer le rôle (`RoleManager.DeleteAsync`).
4. Le seed du compte admin (config `BolDeSang:AdminEmail`) est mis dans `Admin` (au lieu de `Commissaire`).

L'opération est idempotente : à chaque redémarrage, si `Commissaire` n'existe déjà plus, on saute l'étape 3.

## 4. Service d'autorisation

`AuthorizationService` (Scoped) injecté partout où on a besoin de checks :

```csharp
public interface IAuthorizationService
{
    Task<bool> EstAdminAsync(string userId);
    Task<bool> EstGrandCommissaireAsync(string userId);
    Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId);
    Task<bool> PeutGererLigueAsync(string userId, int ligueId);
    Task<bool> PeutEditerDonneesAsync(string userId);
    Task<bool> PeutGererSettingsAsync(string userId);
}
```

Sémantique :
- `PeutGererLigueAsync` = `Admin` ∨ `GrandCommissaire` ∨ `LeagueCommissioner(ligueId)`
- `PeutEditerDonneesAsync` = `Admin` ∨ `GrandCommissaire`
- `PeutGererSettingsAsync` = `Admin` uniquement

## 5. Migration des pages existantes

Liste des fichiers à modifier (cf. `grep -rn "Authorize.*Commissaire" src/`) :

| Fichier | Avant | Après |
|---|---|---|
| `Components/Pages/Admin/Index.razor` | `Roles="Commissaire"` | `Roles="Admin"` |
| `Components/Pages/Admin/Index.razor` (onglet Paramètres) | (implicite Commissaire) | check service `PeutGererSettings` |
| `Components/Pages/Ligues/Creer.razor` | `Roles="Commissaire"` | `Roles="Admin,GrandCommissaire"` |
| `Components/Pages/Ligues/Index.razor` (AuthorizeView "Créer") | `Roles="Commissaire"` | `Roles="Admin,GrandCommissaire"` |
| `Components/Pages/Home.razor` (panel commissaire) | `Roles="Commissaire"` | `Roles="Admin,GrandCommissaire"` |
| `Components/Pages/Ligues/Detail.razor` (boutons admin) | (probablement check inline) | service `PeutGererLigueAsync` |
| `Components/Pages/Matchs/Validation.razor` | (à vérifier au grep — actuellement check inline `EstCommissaireAsync` sur LeagueService) | check service `PeutGererLigueAsync` |
| `Components/Account/...` | unchanged | unchanged |

À l'implémentation, faire un `grep` exhaustif pour ne rien oublier.

## 6. UI : promotion / démotion

### 6.1 Admin > Utilisateurs

Page existante : `Components/Pages/Admin/Index.razor` onglet Utilisateurs.

Ajout : à côté de chaque utilisateur, dropdown du rôle global :
- Options : `Coach`, `GrandCommissaire`, `Admin`
- Action sur changement : `userManager.RemoveFromRolesAsync` puis `AddToRoleAsync`
- Visible **uniquement à l'Admin** (check via service)
- Désactivé pour le compte courant (un Admin ne peut pas se rétrograder lui-même via ce dropdown — protection contre verrouillage accidentel)

### 6.2 Détail Ligue : section commissaires

Sur `Components/Pages/Ligues/Detail.razor`, ajouter une section visible quand `Statut >= EnCours` :

**Pour Admin / Grand Comm** :
- Carte "Commissaires de ligue"
- Liste des commissaires actuels avec bouton "Retirer"
- Bouton "+ Promouvoir un coach" → modale avec liste des coaches de la ligue (sélection multiple possible)

**Pour Coach** :
- Si lui-même est commissaire → badge "Commissaire de cette ligue" + liste read-only des autres commissaires
- Sinon → rien

### 6.3 Pages de gestion de ligue

Sur `Detail.razor`, les boutons :
- "Démarrer inscriptions"
- "Lancer saison"
- "Lancer phase de repos"
- "Générer playoffs"
- "Terminer la ligue"
- "Supprimer la ligue"

Auparavant visibles aux `Commissaire`, doivent maintenant être visibles selon la règle :
- "Supprimer la ligue" : Admin uniquement (action destructive)
- Toutes les autres : `PeutGererLigueAsync(userId, ligueId)` (Admin OU GC OU CommissaireDeLigue)

## 7. Tests

### 7.1 Service `AuthorizationService`

```csharp
[Fact] EstAdmin_ReturnsTrueForAdminUser
[Fact] EstAdmin_ReturnsFalseForCoach
[Fact] EstCommissaireDeLigue_ReturnsTrueForPromotedCoach
[Fact] EstCommissaireDeLigue_ReturnsFalseForCoachInDifferentLeague
[Fact] PeutGererLigue_AdminAlwaysTrue
[Fact] PeutGererLigue_GrandCommissaireAlwaysTrue
[Fact] PeutGererLigue_LeagueCommissionerOnlyForOwnLeague
```

### 7.2 Endpoints / pages

- Test qu'un Coach **non** commissaire de ligue ne peut pas appeler les services de validation match d'une ligue dont il n'est pas commissaire (le service refuse).
- Test qu'un Coach commissaire de la ligue X **peut** lancer la phase de repos sur X.

## 8. Plan de migration

1. Ajouter entité + migration EF + DbContext config.
2. Service `AuthorizationService` + tests unitaires.
3. Migration des rôles Identity dans `DbSeeder.SeedRolesAsync` (rename + create + reassign admin user).
4. Migrer les `[Authorize(Roles="Commissaire")]` dans les .razor.
5. Renforcer les services côté backend (`LeagueService.GenererPlayoffsAsync`, `MatchService.ValiderFeuilleAsync`, etc.) pour vérifier `PeutGererLigueAsync` avant action.
6. UI Admin > Utilisateurs : dropdown rôle.
7. UI Détail ligue : section commissaires de ligue.
8. Tests d'intégration.

## 9. Validation

Choix utilisateur :
- ✅ Renommer `Commissaire` → `Admin`, ajouter `GrandCommissaire`, garder `Coach`
- ✅ `CommissaireDeLigue` = relation per-league (plusieurs commissaires possibles par ligue)
- ✅ Matrice de droits : Admin tout / GC = créer ligues + éditer données / CL = gérer SA ligue / Coach = jouer
