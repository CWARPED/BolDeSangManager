# BolDeSang Manager

[![Docker Hub](https://img.shields.io/docker/v/cwarp/boldesangmanager?label=Docker%20Hub&logo=docker)](https://hub.docker.com/r/cwarp/boldesangmanager)
[![Architectures](https://img.shields.io/badge/arch-amd64%20%7C%20arm64-blue)](https://hub.docker.com/r/cwarp/boldesangmanager)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Application web de gestion de ligues **Blood Bowl** et **Dungeon Bowl** — création de ligues, inscriptions, saison régulière, playoffs, feuilles de match, progression des joueurs et export PDF.

> ⚠️ Projet de fan **non-officiel**. *Blood Bowl* et *Dungeon Bowl* sont des marques de **Games Workshop Ltd.** — voir la [section Licence](#licence).

---

## Table des matières

1. [Aperçu des fonctionnalités](#aperçu-des-fonctionnalités)
2. [Démarrage rapide](#démarrage-rapide)
3. [Docker](#docker)
4. [Architecture technique](#architecture-technique)
5. [Modèle de données](#modèle-de-données)
6. [Rôles et permissions](#rôles-et-permissions)
7. [Guide administrateur](#guide-administrateur)
8. [Guide coach](#guide-coach)
9. [Règles métier](#règles-métier)
10. [Compte et données personnelles](#compte-et-données-personnelles)
11. [Services](#services)
12. [Édition des données du jeu](#édition-des-données-du-jeu)
13. [Export / Import](#export--import)
14. [Configuration](#configuration)
15. [Déploiement en production](#déploiement-en-production)
16. [Licence](#licence)

---

## Aperçu des fonctionnalités

| Domaine | Fonctionnalités |
|---|---|
| **Ligues** | Création, inscription des équipes, 5 formats (Round Robin, avec playoffs, Libre, Libre avec playoffs, Open), calendrier automatique ou composé à la main, export/import JSON, règlement en markdown |
| **Équipes** | Races Blood Bowl + collèges Dungeon Bowl, postes seedés avec mots-clés canoniques, limites par mot-clé (ex : max 3 Gros Bras), staff paramétrable par ligue |
| **Matchs** | Saisie de feuille par un coach, confirmation par l'adversaire, après-match (XP, recrutement, relances), clôture automatique, calcul des PSP et des gains |
| **Joueurs** | Améliorations choisies par le coach (compétence ou hausse de carac), blessures, retraite, valeur actualisée |
| **PDF & QR** | Export feuille d'équipe A4 avec QR code vers la fiche match en ligne |
| **Agenda** | Export iCalendar (`.ics`) des matchs programmés — importable dans Google Agenda, Outlook, Apple Calendrier |
| **Données** | Versions de règles (LRB S3, Death Zone…), édition CRUD via interface admin, clonage transactionnel, Réserve de postes réutilisables |
| **Email** | Notifications via SMTP Gmail (mot de passe d'application) |
| **Auth** | Hiérarchie à 4 niveaux : Admin, Grand Commissaire, Commissaire de Ligue, Coach |
| **RGPD** | Export complet de ses données personnelles, suppression de compte avec anonymisation |

---

## Démarrage rapide

### Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQLite — aucune installation requise, le fichier est créé automatiquement

### Installation

```bash
git clone https://github.com/CWARPED/BolDeSangManager.git
cd BolDeSangManager
dotnet restore src/BolDeSangManager/BolDeSangManager.csproj
cd src/BolDeSangManager
dotnet run
```

L'application est accessible sur **http://localhost:5129**.

> Le `DbSeeder` peuple automatiquement la base au premier démarrage : jeux, races Blood Bowl, collèges Dungeon Bowl, compétences, mots-clés canoniques, staff standard, et un compte Admin par défaut. Les migrations EF sont appliquées automatiquement.

### Compte par défaut

| Champ | Valeur |
|---|---|
| Email | `commissaire@boldesang.fr` |
| Mot de passe | `Commissaire123!` |
| Rôle | `Admin` |

> 🔐 **À changer dès la première connexion en production.**

### Réinitialiser la base (dev)

```bash
rm src/BolDeSangManager/Data/boldesang.db
dotnet run   # le seeder repeuple tout
```

### Tests

```bash
dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj
```

---

## Docker

Image officielle : **[cwarp/boldesangmanager](https://hub.docker.com/r/cwarp/boldesangmanager)** — multi-arch `linux/amd64` (serveurs, VPS) et `linux/arm64` (Raspberry Pi 4/5, Freebox Ultra via VM).

```bash
docker compose up -d
```

Le compte Admin initial est créé au démarrage. Toutes les options (identifiants, URL externe pour les QR codes) se configurent via variables d'environnement dans `docker-compose.yml`. La base SQLite est persistée dans un volume Docker nommé.

> 📖 Voir **[DOCKER.md](DOCKER.md)** pour le guide complet : variables d'environnement, reverse proxy, Raspberry Pi, Freebox Ultra, build multi-arch.

---

## Architecture technique

### Stack

| Couche | Technologie |
|---|---|
| Frontend | Blazor Server (.NET 9), MudBlazor 8.6 |
| Backend | ASP.NET Core, Identity |
| ORM | EF Core 9 |
| Base de données | SQLite |
| PDF | QuestPDF (licence Community) |
| QR codes | QRCoder |
| Markdown | Markdig (règlements de ligue) |
| Email | MailKit (Gmail SMTP) |

### Modes de rendu

- **Pages applicatives** (`Components/Pages/`) → `InteractiveServer` via `HttpContext.AcceptsInteractiveRouting()`. Circuit Blazor actif, MudBlazor complet.
- **Pages de connexion** (`Components/Account/Pages/`) → SSR statique (formulaires `EditForm method="post"`). Marquées `[ExcludeFromInteractiveRouting]`.

> `MudTooltip` n'est pas compatible avec ce schéma de rendu hybride. Utiliser l'attribut HTML `title` à la place.

> Les composants MudBlazor **interactifs** (`MudTextField`, `MudSelect`…) ne fonctionnent pas dans une page SSR. Les formulaires de connexion utilisent des `InputText` stylés par les classes `.account-*` de `wwwroot/app.css`.

### Arborescence

```
BolDeSangManager/
├── src/BolDeSangManager/
│   ├── Components/
│   │   ├── Layout/             # MainLayout, AccountLayout, NavMenu, MudProviders
│   │   ├── Account/Pages/      # Parcours non connecté (Login, Register, mot de passe
│   │   │                       # oublié, confirmation d'e-mail) + SupprimerCompte
│   │   └── Pages/
│   │       ├── Admin/          # Panneau admin (utilisateurs, paramètres, données)
│   │       ├── Ligues/         # Index, Creer, Detail, CalendrierLibre, StaffLigue, PhaseRepos
│   │       ├── Equipes/        # MaFeuille, Rejoindre, Detail
│   │       ├── Matchs/         # Index, Feuille, Detail, Validation, ApresMatch
│   │       ├── Profil/         # Mon profil — pseudo, e-mail, mot de passe, RGPD
│   │       └── APropos.razor   # Licence + disclaimer Games Workshop
│   ├── Data/
│   │   ├── Models/             # Entités EF Core
│   │   ├── Enums/              # Enums du domaine
│   │   ├── Seeding/            # Seed data Blood Bowl + Dungeon Bowl, seuils d'XP
│   │   ├── ApplicationDbContext.cs
│   │   ├── ApplicationUser.cs
│   │   └── DbSeeder.cs         # Seed idempotent + migration automatique
│   ├── Helpers/                # DisplayHelpers, BrouillardHelpers, accès compétences
│   ├── Services/               # Logique métier (Scoped)
│   ├── Program.cs
│   └── wwwroot/                # CSS, images, favicon
├── docs/regles/                # Règles BB & Dungeon Bowl extraites des PDFs
├── tests/                      # xUnit — 403 tests
├── LICENSE                     # AGPL-3.0
├── DOCKER.md                   # Guide de déploiement
└── CLAUDE.md                   # Instructions Claude Code
```

---

## Modèle de données

### Vue d'ensemble

```
Game ─────────── RulesVersion ───┬── Skill ── SkillCategoryDef
                                 ├── PoolPosition (Réserve) ── PoolPositionSkill
                                 ├── StaffType
                                 └── TeamType ─── PlayerPosition ─── PlayerPositionSkill ─── Skill
                                       │
                                       └── TeamTypeKeywordLimit

League ────┬── LeagueCommissioner ── ApplicationUser
           ├── LeagueStaffType          (copie des StaffType à la création)
           ├── LeagueAward              (titres de fin de saison)
           ├── EcheanceRonde            (dates limites, formats Libre)
           │
           ├── Division ── Match ── MatchSheet ──┬── MatchPlayerRecord
           │                                     └── PlayerInjury
           │
           └── Team ──┬── TeamStaff
                      └── TeamPlayer ──┬── TeamPlayerSkill ── Skill
                                       ├── PlayerInjury
                                       ├── PlayerImprovement
                                       └── XpCorrection

AppConfig (clé/valeur runtime : SMTP, URL externe...)
```

### Entités clés

#### `RulesVersion`
Versionnement des données de jeu. Permet de cloner une version (LRB S3 → Death Zone) sans casser les ligues existantes. Le clonage recopie types d'équipe, postes, compétences, catégories, Réserve et staff dans une transaction atomique.

| Propriété | Type | Description |
|---|---|---|
| GameId | int | FK vers Game |
| Nom | string | "LRB Saison 3", "Death Zone 2025"... |
| Ordre | int | Tri |
| EstActive | bool | Une seule version active par jeu |

#### `TeamType` & `PlayerPosition`
Définition d'une race/collège et de ses postes. Chaque poste a des mots-clés (`MotsCles` CSV) qui pilotent les limites d'équipe.

`PlayerPosition` :

| Propriété | Type | Description |
|---|---|---|
| Nom | string | Ex: "Trois-quart Humain" |
| QuantiteMax | int | Plafond par équipe |
| Cout | int | Coût en pièces d'or |
| Mouvement, Force, Agilite, CapacitePasse, Armure | string/int | Caractéristiques |
| MotsCles | string | CSV: "Trois-quart,Humain,Squelette,Mort-Vivant" |

L'accès aux compétences passe par `PlayerPositionCategoryAccess` (catégories principales / secondaires), et non par une chaîne de lettres.

#### `SkillCategoryDef`
Les catégories de compétence sont une **table portée par une `RulesVersion`**, pas un enum figé. Chaque catégorie a un nom complet et un code de 1 à 2 caractères, uniques par version. Une catégorie utilisée par au moins une compétence ne peut pas être supprimée.

#### `PoolPosition` — la Réserve
Catalogue de postes réutilisables au niveau d'une version. Design **copie, bidirectionnel** : on importe un poste de la Réserve vers une équipe, ou on renvoie une copie d'un poste d'équipe vers la Réserve. Les deux exemplaires restent indépendants.

#### `TeamTypeKeywordLimit`
Plafond par mot-clé au niveau d'une équipe (ex : *max 3 Gros Bras* pour les Renégats du Chaos).

#### `League`
| Propriété | Type | Description |
|---|---|---|
| GameId, RulesVersionId | int | Référence aux règles utilisées |
| Format | LeagueFormat | `RoundRobin`, `RoundRobinAvecPlayoffs`, `Libre`, `LibreAvecPlayoffs`, `Open` |
| Statut | LeagueStatus | Voir cycle de vie |
| BudgetDepart | int | Budget initial des équipes |
| NombreEquipesPlayoff | int | Équipes qualifiées |
| ModeBrouillard | bool | Masque les rosters adverses avant le match |
| Reglement | string | Texte markdown affiché aux participants |
| XpPar… | int | Barème d'XP **copié** à la création — modifier les règles ne rétro-agit pas |

> ⚠️ `LeagueFormat` est persisté en `int` : **ne jamais réordonner l'enum**, toute nouvelle valeur s'ajoute à la fin.

#### `LeagueCommissioner`
Délégation de la gestion d'une ligue à un utilisateur tiers (rôle `CommissaireDeLigue` *par ligue*). C'est cette table — et non `League.CommissaireId` — que consulte `EstCommissaireDeLigueAsync`.

#### `TeamPlayer`
| Propriété | Type | Description |
|---|---|---|
| PointsStarPlayer | int | PSP cumulés |
| ValeurActuelle | int | Valeur actualisée |
| ModMouvement, ModForce... | int | Modificateurs (blessures, améliorations) |
| EstMort, EstRetraite | bool | |
| ManqueSuivantMatch | bool | Indisponible au prochain match |

#### `PlayerImprovement`
Trace de chaque amélioration (compétence choisie, hausse de carac) avec son palier et le match d'origine.

#### `AppConfig`
Table clé/valeur pour les paramètres runtime (SMTP, URL externe). Gérée via `SettingsService` — pas de redémarrage requis après modification.

---

## Rôles et permissions

Trois rôles Identity sont seedés — `Admin`, `GrandCommissaire`, `Coach` — auxquels s'ajoute la fonction **Commissaire de Ligue**, qui n'est pas un rôle global mais une **entrée dans `LeagueCommissioners`** pour une ligue donnée. Tout est centralisé dans `IAuthorizationService` :

| Niveau | Portée | Permissions |
|---|---|---|
| **Admin** | Global | Tout : gérer les utilisateurs, éditer les données de jeu, gérer les paramètres SMTP/URL, gérer n'importe quelle ligue |
| **Grand Commissaire** | Global | Éditer les données de jeu, gérer n'importe quelle ligue |
| **Commissaire de Ligue** | Une ligue | Gérer **sa** ligue : lancer la saison, composer le calendrier, générer les playoffs, corriger une feuille |
| **Coach** | Ses équipes | Rejoindre une ligue, gérer son roster, saisir et confirmer les feuilles, faire son après-match |

L'inscription crée un compte avec le rôle `Coach`. Les rôles globaux s'attribuent depuis `Admin > Utilisateurs` ; la fonction Commissaire de Ligue se délègue depuis la page de détail d'une ligue (**Promouvoir un coach**).

**API d'autorisation** (`IAuthorizationService`) :

```csharp
Task<bool> EstAdminAsync(string userId);
Task<bool> EstGrandCommissaireAsync(string userId);
Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId);
Task<bool> PeutGererLigueAsync(string userId, int ligueId);   // Admin || GrandCommissaire || CommissaireDeLigue
Task<bool> PeutEditerDonneesAsync(string userId);              // Admin || GrandCommissaire
Task<bool> PeutGererSettingsAsync(string userId);              // Admin
```

---

## Guide administrateur

### Créer une ligue

`Ligues > Créer` — nom, jeu, version de règles, format, budget de départ, nombre d'équipes pour les playoffs.

### Cycle de vie d'une ligue

```
Creation → Inscription → EnCours → PlayOffs → Termine
```

- **Démarrer les inscriptions** — les coaches peuvent rejoindre la ligue.
- **Lancer la saison** — génère le calendrier (formats Round Robin) ou ouvre la composition manuelle des rondes (formats Libre).
- **Générer les playoffs** — sélectionne les meilleures équipes au classement.
- **Clôturer la ligue** — fige les statistiques, permet d'attribuer les titres (`LeagueAward`).

### Corriger un match

`Matchs > {match} > Validation`. Ces actions sont **correctives et optionnelles** — un match se joue et se clôture entièrement entre les deux coaches, sans intervention :

- **Éditer la feuille** — corriger des statistiques mal saisies.
- **Forcer la clôture** — uniquement si un coach ne peut pas faire son après-match.
- **Corriger l'XP** d'un joueur (`XpCorrection`, tracé).

### Déléguer une ligue

Dans la page de détail d'une ligue, section **Commissaires de Ligue** : promouvoir un coach pour cette ligue spécifiquement.

### Gestion globale

| Page | Accès | Contenu |
|---|---|---|
| `/admin` (onglet Utilisateurs) | Admin | Liste des comptes, dropdown de rôle global |
| `/admin` (onglet Paramètres) | Admin | URL externe pour QR codes, config SMTP Gmail |
| `/admin/donnees` | Admin, Grand Commissaire | Édition CRUD : versions, types d'équipe, postes, compétences et leurs catégories, Réserve, staff, limites par mot-clé |

---

## Guide coach

### Rejoindre une ligue

Depuis la page de détail d'une ligue en statut `Inscription`, bouton **Rejoindre la ligue** → formulaire de création d'équipe (nom, race/collège, roster initial, staff). Limites par poste et par mot-clé validées côté serveur.

### Consulter son roster

`Equipes > Ma Feuille` :
- Caractéristiques (MOV / FOR / AGI / CP / ARM) et leurs modificateurs.
- Compétences de départ et acquises.
- PSP, palier d'amélioration, mots-clés.
- Blessures et état (mort, retraite, indisponibilité).
- Valeur Estimée de l'Équipe (VEA).
- Export PDF A4 avec QR code vers le prochain match.

### Jouer un match, de bout en bout

1. **Saisir la feuille** — un des deux coaches remplit score, performances individuelles (TD, passes, interceptions, éliminations, MVP), blessures et gains.
2. **Confirmer** — l'adversaire relit et confirme. Le match passe en *Après-match*.
3. **Après-match** — chaque coach dépense les XP de ses joueurs, recrute, achète des relances.
4. **Clôture automatique** — dès que les **deux** coaches ont validé leur après-match, le match passe à `Termine` et le classement se met à jour.

> Aucune validation par un commissaire n'est requise à aucune étape.

### Améliorer un joueur

Lorsqu'un joueur atteint un palier de PSP (`ImprovementThresholds`), **le coach choisit lui-même** son amélioration — compétence dans ses catégories accessibles, ou hausse de caractéristique — depuis la page *Après-match*.

---

## Règles métier

### Cycle de vie d'un match

```
Programme → AJouer → FeuilleEnSaisie → ValidationCompetences → Termine
```

> ⚠️ **`ValidationCompetences` porte un nom trompeur** : ce n'est pas une attente de validation par un commissaire, mais la **phase d'après-match des deux coaches** (l'interface l'affiche d'ailleurs « Après-match »). La clôture est automatique dès que les deux ont validé.

### Points Star Player (PSP)

Calculés à la saisie de la feuille. Le barème est **copié dans la ligue** à sa création (`XpParTouchdown`, `XpParPasse`…) : modifier les règles n'affecte pas une saison en cours.

| Action | PSP (défaut Blood Bowl) |
|---|---:|
| Touchdown | +3 |
| Passe | +1 |
| Interception | +2 |
| Elimination infligée | +2 |
| MVP | +4 |

> En Dungeon Bowl, le touchdown vaut 5 par défaut (`XpBareme.DeLigue`).

Les paliers sont définis dans `ImprovementThresholds`. Chaque palier ouvre droit à une amélioration, choisie par le coach pendant son après-match.

### Limites par mot-clé

Plutôt que de hard-coder « max 3 Gros Bras », chaque poste porte une liste de mots-clés et chaque type d'équipe peut définir des limites globales :

```
Renégats du Chaos:
  - Poste "Troll Renégat"     → MotsCles: "Gros Bras,Troll"
  - Poste "Ogre Renégat"      → MotsCles: "Gros Bras,Ogre"
  - Poste "Minotaure Renégat" → MotsCles: "Gros Bras,Minotaure"
  - Limite : MotCle="Gros Bras", Max=3
```

`TeamService.CreerEquipeAsync` et `RecruterJoueurAsync` valident ces plafonds côté serveur.

### Compétences

| Type | Description |
|---|---|
| Compétence de départ | Liée au poste (`PlayerPosition.CompetencesDepart`), `EstCompetenceDepart = true` |
| Compétence acquise | Attribuée en cours de saison après amélioration, `EstCompetenceDepart = false` |

Les compétences de départ ne sont pas dupliquées dans `TeamPlayerSkill` — `GetEquipeAsync` les remonte uniquement via `PlayerPosition.CompetencesDepart`.

### Valeur Estimée de l'Équipe (VEA)

`TeamService.CalculerVEA(equipe)` = somme des joueurs actifs + relances + staff détenu (`TeamStaff`, aux prix de la ligue).

### Blessures

| Type | Effet |
|---|---|
| ManqueSuivant | Joueur absent au prochain match |
| BlessurePersistante | Réduction d'une caractéristique (`AffectedStat`) |
| RetraiteTemporaire | Mis en retraite, ne joue plus |
| Mort | Joueur retiré définitivement |

### Mode brouillard

Quand `ModeBrouillard` est actif sur une ligue, les rosters adverses sont masqués tant que le match n'est pas joué (`BrouillardHelpers`) — pour éviter de préparer sa composition en fonction de celle de l'adversaire.

---

## Compte et données personnelles

Tout se passe sur **`/profil`** — il n'y a pas d'espace « Mon compte » séparé.

| Action | Détail |
|---|---|
| Changer son pseudo | Immédiat |
| Changer son e-mail | Lien de confirmation envoyé à la **nouvelle** adresse ; rien ne change tant qu'il n'est pas cliqué |
| Changer son mot de passe | Ancien mot de passe requis |
| **Exporter ses données** | JSON complet : compte, équipes et leurs joueurs, matchs, ligues gérées, feuilles saisies |
| **Supprimer son compte** | Page `/compte/supprimer`, confirmation par mot de passe + saisie du mot « SUPPRIMER » |

### Suppression de compte : deux comportements

Quatre clés étrangères pointent vers `ApplicationUser` en `Restrict` (`Team.CoachId`, `League.CommissaireId`, `MatchSheet.SaisiParId`, `LeagueCommissioner.UserId`). Détruire la ligne d'un coach ayant joué casserait l'historique sportif **des autres**. D'où :

- **Compte sans aucune trace** → suppression réelle de la ligne.
- **Compte rattaché à des données** → **anonymisation** : e-mail remplacé par une adresse `@local.invalid`, pseudo « Coach supprimé », mot de passe effacé, rôles retirés, connexion définitivement bloquée. Les équipes restent dans leurs ligues, les classements sont intacts.

L'écran annonce précisément lequel des deux s'appliquera **avant** toute confirmation.

> Une ligue dont le commissaire supprime son compte **continue de fonctionner** : les coaches jouent et clôturent leurs matchs normalement. Un Admin ou Grand Commissaire peut reprendre la ligue et promouvoir un nouveau commissaire.

⚠️ **Pour les développeurs** : toute nouvelle FK vers `ApplicationUser` doit être *nullable + SetNull*, ou ajoutée au comptage de `EvaluerSuppressionAsync` — et au `PersonalDataExportService`.

---

## Services

Tous les services sont enregistrés en `Scoped` dans `Program.cs`.

| Service | Rôle |
|---|---|
| **LeagueService** | Cycle de vie ligue : création → inscriptions → saison → playoffs → suppression atomique, titres de fin de saison |
| **TeamService** | Roster : création d'équipe, recrutement, validation des limites poste/mot-clé, calcul VEA, améliorations et corrections d'XP |
| **MatchService** | Saisie et confirmation de feuille, calcul PSP/blessures/gains, après-match et clôture automatique |
| **CalendrierService** | Génération de fichiers iCalendar (`.ics`) conformes RFC 5545 pour les matchs programmés |
| **StaffService** | Staff d'équipe (fans, relances, assistants, cheerleaders, apothicaire), avec prix copiés par ligue |
| **MarkdownService** | Rendu des règlements de ligue — Markdig sans HTML brut + filtrage des URL `javascript:` / `data:` |
| **AuthorizationService** | Centralise toutes les vérifications de rôle (`IAuthorizationService`) |
| **DataEditService** | CRUD des données de jeu + clonage transactionnel d'une version + Réserve |
| **PdfService** | Génération PDF (feuille d'équipe A4, QR code vers la fiche match) |
| **LeagueExportService** | Export/import JSON d'une ligue complète, références résolues par nom |
| **GameDataExportService** | Export/import des données de jeu d'une version (types d'équipe, postes, compétences, Réserve) |
| **PersonalDataExportService** | Export RGPD du dossier complet d'un coach |
| **UserAccountService** | Suppression de compte : verdict, suppression dure ou anonymisation |
| **SettingsService** | Lecture/écriture `AppConfig` (URL externe, identifiants SMTP) — pas de redémarrage requis |
| **GmailEmailSender** | `IEmailSender<ApplicationUser>` via SMTP Gmail (StartTls 587), credentials lus à chaque envoi |

---

## Édition des données du jeu

Page `/admin/donnees` (Admin, Grand Commissaire) — interface CRUD sur les données seedées :

- **Versions de règles** : créer une nouvelle version par clonage transactionnel d'une existante.
- **Types d'équipe** : nom, catégorie, coût de relance, règles spéciales.
- **Postes** : caractéristiques, catégories de compétences accessibles, mots-clés, compétences de départ.
- **Compétences** : nom, catégorie, description — avec gestion des catégories elles-mêmes (nom + code de 1-2 caractères, uniques par version).
- **Réserve** : postes réutilisables importables dans n'importe quelle équipe de la version.
- **Staff** : types de staff et leurs prix.
- **Limites par mot-clé** : plafonds globaux pour un type d'équipe.

Toutes les opérations sont validées (pas de suppression d'une entité référencée). Le clonage d'une `RulesVersion` recopie compétences, catégories, types d'équipe, postes, Réserve, staff et limites dans une transaction atomique.

---

## Export / Import

| Portée | Où | Contenu |
|---|---|---|
| **Une ligue** | Liste des ligues | Équipes, joueurs, compétences, matchs et résultats |
| **Données de jeu** | `/admin/donnees` | Types d'équipe, postes, compétences, catégories, Réserve d'une version |
| **Réserve seule** | `/admin/donnees` → onglet Réserve | Import en mode *append*, compétences résolues par nom |
| **Ses données perso** | `/profil` | Dossier RGPD complet du coach connecté |

Les JSON résolvent les références par **nom** (Game, TeamType, PlayerPosition, Skill) pour rester portables entre instances.

---

## Configuration

### Variables d'environnement (Docker)

| Variable | Description |
|---|---|
| `BolDeSang__AdminEmail` | Email du premier compte Admin |
| `BolDeSang__AdminPassword` | Mot de passe (≥ 8 chars, ≥ 1 chiffre) |
| `BolDeSang__AdminPseudo` | Pseudo affiché du compte Admin |
| `BolDeSang__UrlExterne` | URL publique (QR codes des PDFs) |
| `ConnectionStrings__DefaultConnection` | Chemin de la base SQLite |
| `DataProtection__KeysPath` | Dossier des clés de chiffrement des sessions — **à persister**, sinon tout le monde est déconnecté à chaque redémarrage |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Paramètres runtime (Admin → Paramètres)

Stockés dans `AppConfig`, modifiables sans redémarrer :

| Clé | Description |
|---|---|
| `UrlExterne` | URL publique de l'app (pour les QR codes dans les PDFs) |
| `EmailExpediteur` | Adresse Gmail de l'expéditeur |
| `EmailNomExpediteur` | Nom affiché dans la boîte de réception |
| `EmailMotDePasse` | **Mot de passe d'application** Gmail (16 caractères) — pas le mot de passe du compte. Généré sur [myaccount.google.com](https://myaccount.google.com) |

Un bouton **Tester** envoie un email de vérification à l'adresse saisie.

---

## Déploiement en production

Méthode recommandée : **Docker** — voir **[DOCKER.md](DOCKER.md)** pour le guide complet.

```bash
docker compose up -d
```

### Checklist

- [ ] Personnaliser `BolDeSang__AdminEmail` et `BolDeSang__AdminPassword`
- [ ] Reverse proxy HTTPS (nginx, Traefik, Caddy) devant le conteneur
- [ ] Vérifier que `DataProtection__KeysPath` pointe dans le volume persistant
- [ ] Renseigner `UrlExterne` dans Admin → Paramètres (utilisée pour les QR codes)
- [ ] Configurer SMTP Gmail si vous voulez les notifications par email
- [ ] Sauvegarder le volume Docker contenant la base SQLite

> L'application lit les en-têtes `X-Forwarded-For` / `X-Forwarded-Proto` : un reverse proxy terminant le TLS est correctement pris en compte, y compris pour les liens absolus des e-mails.

---

## Licence

Ce projet est distribué sous licence **[GNU Affero General Public License v3.0](LICENSE)** (AGPL-3.0).

Tu peux librement consulter, modifier et redistribuer le code, à condition que toute version modifiée — y compris déployée comme service réseau — soit également publiée sous la même licence.

### Avis légal — Propriété intellectuelle

> Ce projet est un outil **non-officiel** développé par des fans. Il n'est **ni affilié à**, **ni approuvé par**, **ni sponsorisé par** Games Workshop Ltd.
>
> **Blood Bowl**, **Dungeon Bowl**, ainsi que tous les noms, races, marques, logos, illustrations et univers associés sont la propriété exclusive de **Games Workshop Ltd.**
>
> Seul le code source de l'application est couvert par la licence AGPL-3.0. Les règles et l'univers du jeu restent la propriété intellectuelle de leurs détenteurs respectifs.

La page **`/a-propos`** de l'application affiche ces mentions en permanence dans le menu de navigation.
