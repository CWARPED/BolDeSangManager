# BolDeSang Manager

[![Docker Hub](https://img.shields.io/docker/v/cwarp/boldesangmanager?label=Docker%20Hub&logo=docker)](https://hub.docker.com/r/cwarp/boldesangmanager)
[![Architectures](https://img.shields.io/badge/arch-amd64%20%7C%20arm64-blue)](https://hub.docker.com/r/cwarp/boldesangmanager)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Application web de gestion de ligues **Blood Bowl** et **Dungeon Bowl** — création de ligues, inscriptions, saison régulière, playoffs, feuilles de match, gestion des compétences et export PDF.

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
10. [Services](#services)
11. [Édition des données du jeu](#édition-des-données-du-jeu)
12. [Export / Import](#export--import)
13. [Configuration](#configuration)
14. [Déploiement en production](#déploiement-en-production)
15. [Licence](#licence)

---

## Aperçu des fonctionnalités

| Domaine | Fonctionnalités |
|---|---|
| **Ligues** | Création, inscription des équipes, génération du calendrier Round Robin, playoffs configurables, export/import JSON |
| **Équipes** | Toutes les races Blood Bowl + 8 collèges Dungeon Bowl (édition 2022), 250+ postes seedés avec mots-clés canoniques, limites par mot-clé (ex : max 3 Gros Bras) |
| **Matchs** | Saisie de feuille (scores, performances individuelles, blessures), calcul automatique des PSP et des gains, validation par commissaire |
| **Joueurs** | Améliorations (skill aléatoire ou choisi, hausse de carac), blessures et retraite, valeur estimée actualisée |
| **PDF & QR** | Export feuille d'équipe A4 avec QR code vers la fiche match en ligne |
| **Données** | Système de versions de règles (LRB S3, Death Zone…), édition CRUD via interface admin, clonage transactionnel d'une version |
| **Email** | Notifications via SMTP Gmail (mot de passe d'application) |
| **Auth** | Hiérarchie à 4 niveaux : Admin, Grand Commissaire, Commissaire de Ligue, Coach |

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

> Le `DbSeeder` peuple automatiquement la base au premier démarrage : jeux, races Blood Bowl, collèges Dungeon Bowl, ~90 compétences, mots-clés canoniques, et un compte Admin par défaut. Les migrations EF sont appliquées automatiquement.

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
| Email | MailKit (Gmail SMTP) |

### Modes de rendu

- **Pages applicatives** (`Components/Pages/`) → `InteractiveServer` via `HttpContext.AcceptsInteractiveRouting()`. Circuit Blazor actif, MudBlazor complet.
- **Pages Identity** (`Components/Account/Pages/`) → SSR statique (formulaires `EditForm method="post"`). Marquées `[ExcludeFromInteractiveRouting]`. Layout dédié `AccountLayout.razor`.

> `MudTooltip` n'est pas compatible avec ce schéma de rendu hybride. Utiliser l'attribut HTML `title` à la place.

### Arborescence

```
BolDeSangManager/
├── src/BolDeSangManager/
│   ├── Components/
│   │   ├── Layout/             # MainLayout, AccountLayout, NavMenu, MudProviders
│   │   ├── Account/Pages/      # Login, Register, gestion du compte (SSR)
│   │   └── Pages/
│   │       ├── Admin/          # Panneau admin (utilisateurs, paramètres, données)
│   │       ├── Ligues/         # Index, Creer, Detail
│   │       ├── Equipes/        # MaFeuille, Rejoindre, Detail
│   │       ├── Matchs/         # Index, Feuille, Validation, ApresMatch
│   │       └── APropos.razor   # Licence + disclaimer Games Workshop
│   ├── Data/
│   │   ├── Models/             # Entités EF Core
│   │   ├── Enums/              # Enums du domaine
│   │   ├── Seeding/            # Seed data Blood Bowl + Dungeon Bowl
│   │   ├── ApplicationDbContext.cs
│   │   ├── ApplicationUser.cs
│   │   └── DbSeeder.cs         # Seed idempotent + migration automatique
│   ├── Helpers/                # DisplayHelpers (couleurs/labels enums)
│   ├── Services/               # Logique métier (Scoped)
│   ├── Program.cs
│   └── wwwroot/                # CSS, images, favicon
├── docs/regles/                # Règles BB & Dungeon Bowl extraites des PDFs
├── tests/                      # xUnit (123 tests)
├── LICENSE                     # AGPL-3.0
├── DOCKER.md                   # Guide de déploiement
└── CLAUDE.md                   # Instructions Claude Code
```

---

## Modèle de données

### Vue d'ensemble

```
Game ─────────── RulesVersion ───┬── Skill
                                 │
                                 └── TeamType ─── PlayerPosition ─── PlayerPositionSkill ─── Skill
                                       │
                                       └── TeamTypeKeywordLimit

League ────┬── LeagueCommissioner ── ApplicationUser
           │
           ├── Division ── Match ── MatchSheet ── MatchPlayerRecord
           │                              │
           │                              └── PlayerInjury
           │
           └── Team ── TeamPlayer ──┬── TeamPlayerSkill ── Skill
                                    ├── PlayerInjury
                                    └── PlayerImprovement

AppConfig (clé/valeur runtime : SMTP, URL externe...)
```

### Entités clés

#### `RulesVersion`
Versionnement des données de jeu. Permet de cloner une version (LRB S3 → Death Zone) sans casser les ligues existantes.

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
| CompetencesPrincipales, CompetencesSecondaires | string | Accès skills (ex: "GAF") |
| MotsCles | string | CSV: "Trois-quart,Humain,Squelette,Mort-Vivant" |

#### `TeamTypeKeywordLimit`
Plafond par mot-clé au niveau d'une équipe (ex : *max 3 Gros Bras* pour les Renégats du Chaos).

#### `League`
| Propriété | Type | Description |
|---|---|---|
| GameId, RulesVersionId | int | Référence aux règles utilisées |
| Format | LeagueFormat | `RoundRobin` ou `RoundRobinAvecPlayoffs` |
| Statut | LeagueStatus | Voir cycle de vie |
| BudgetDepart | int | Budget initial des équipes |
| NombreEquipesPlayoff | int | Équipes qualifiées |

#### `LeagueCommissioner`
Délégation de la gestion d'une ligue à un utilisateur tiers (rôle `CommissaireDeLigue` *par ligue*).

#### `TeamPlayer`
| Propriété | Type | Description |
|---|---|---|
| PointsStarPlayer | int | PSP cumulés |
| ValeurActuelle | int | Valeur actualisée |
| ModMouvement, ModForce... | int | Modificateurs (blessures, améliorations) |
| EstMort, EstRetraite | bool | |
| ManqueSuivantMatch | bool | Indisponible au prochain match |

#### `PlayerImprovement`
Trace de chaque amélioration (skill aléatoire/choisi, hausse de carac) avec son palier et le match d'origine.

#### `AppConfig`
Table clé/valeur pour les paramètres runtime (SMTP, URL externe). Gérée via `SettingsService` — pas de redémarrage requis après modification.

---

## Rôles et permissions

Hiérarchie à 4 niveaux, centralisée dans `IAuthorizationService` :

| Rôle | Portée | Permissions |
|---|---|---|
| **Admin** | Global | Tout : gérer utilisateurs, éditer les données de jeu, gérer les paramètres SMTP/URL, créer des ligues, valider des matchs |
| **Grand Commissaire** | Global | Éditer les données de jeu, créer des ligues, valider tout match |
| **Commissaire de Ligue** | Une ligue | Gérer **sa** ligue (lancer la saison, valider les matchs, générer les playoffs) |
| **Coach** | Ses équipes | Rejoindre une ligue, gérer son roster, saisir les feuilles de match |

L'inscription crée un compte avec le rôle `Coach`. Les rôles supérieurs sont attribués manuellement depuis `Admin > Utilisateurs`. La fonction Commissaire de Ligue se délègue depuis la page de détail d'une ligue.

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
- **Lancer la saison** — génère le calendrier Round Robin pour chaque division.
- **Générer les playoffs** — sélectionne les meilleures équipes au classement.
- **Clôturer la ligue** — fige les statistiques.

### Valider une feuille de match

`Matchs > {match} > Validation` — relire les performances, ajouter des notes, valider. La validation :
- Active les PSP des joueurs et débloque les améliorations.
- Met à jour les stats des équipes au classement.
- Confirme blessures et gains.

### Déléguer une ligue

Dans la page de détail d'une ligue, section **Commissaires de Ligue** : promouvoir un coach en `CommissaireDeLigue` pour cette ligue spécifiquement.

### Gestion globale

| Page | Accès | Contenu |
|---|---|---|
| `/admin` (onglet Utilisateurs) | Admin | Liste des comptes, dropdown de rôle global |
| `/admin` (onglet Paramètres) | Admin | URL externe pour QR codes, config SMTP Gmail |
| `/admin/donnees` | Admin, Grand Commissaire | Édition CRUD des `RulesVersion`, `TeamType`, `PlayerPosition`, `Skill`, `TeamTypeKeywordLimit` |

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

### Saisir une feuille de match

`Matchs` — sélectionner un match `AJouer`. Saisir :
- Score, eliminations, inducements utilisés.
- Performances individuelles : TD, completions, interceptions, eliminations, MVP.
- Blessures subies, gains.

La soumission calcule automatiquement les PSP et marque les joueurs ayant atteint le seuil de compétence.

### Améliorer un joueur

Lorsqu'un joueur atteint un palier de PSP, le coach propose une amélioration (compétence aléatoire/choisie, hausse de caractéristique) depuis la page *Après-match* ; le commissaire valide.

---

## Règles métier

### Cycle de vie d'un match

```
Programme → AJouer → FeuilleEnSaisie → ValidationCompetences → Termine
```

### Points Star Player (PSP)

Calculés à la saisie de la feuille :

| Action | PSP |
|---|---:|
| Touchdown | +3 |
| Passe | +1 |
| Interception | +2 |
| Elimination infligée | +2 |
| MVP | +4 |

Les paliers d'amélioration sont définis dans `ImprovementThresholds`. Chaque palier débloque le droit à une amélioration (skill aléatoire, choisi, hausse de carac…), validée par le commissaire.

### Limites par mot-clé

Plutôt que de hard-coder « max 3 Gros Bras », chaque poste porte une liste de mots-clés et chaque type d'équipe peut définir des limites globales :

```
Renégats du Chaos:
  - Poste "Troll Renégat"   → MotsCles: "Gros Bras,Troll"
  - Poste "Ogre Renégat"    → MotsCles: "Gros Bras,Ogre"
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

`TeamService.CalculerVEA(equipe)` = somme des joueurs actifs + relances + apothicaire + staff (assistants, cheerleaders) + fans dévoués.

### Blessures

| Type | Effet |
|---|---|
| ManqueSuivant | Joueur absent au prochain match |
| BlessurePersistante | Réduction d'une caractéristique (`AffectedStat`) |
| RetraiteTemporaire | Mis en retraite, ne joue plus |
| Mort | Joueur retiré définitivement |

---

## Services

Tous les services sont enregistrés en `Scoped` dans `Program.cs`.

| Service | Rôle |
|---|---|
| **LeagueService** | Cycle de vie ligue : création → inscriptions → saison → playoffs → suppression atomique |
| **TeamService** | Roster : création d'équipe, recrutement, validation des limites poste/mot-clé, calcul VEA, amélioration de joueur |
| **MatchService** | Saisie feuille, calcul PSP/blessures/gains, validation par commissaire |
| **AuthorizationService** | Centralise toutes les vérifications de rôle (`IAuthorizationService`) |
| **DataEditService** | CRUD des données de jeu (`RulesVersion`, `TeamType`, `PlayerPosition`, `Skill`, `TeamTypeKeywordLimit`) + clonage transactionnel d'une version |
| **PdfService** | Génération PDF (feuille d'équipe A4, QR code vers la fiche match) |
| **LeagueExportService** | Export/import JSON d'une ligue complète, références résolues par nom |
| **SettingsService** | Lecture/écriture `AppConfig` (URL externe, identifiants SMTP) — pas de redémarrage requis |
| **GmailEmailSender** | `IEmailSender<ApplicationUser>` via SMTP Gmail (StartTls 587), credentials lus à chaque envoi via SettingsService |

---

## Édition des données du jeu

Page `/admin/donnees` (Admin, Grand Commissaire) — interface CRUD sur les données seedées :

- **Versions de règles** : créer une nouvelle version par clonage transactionnel d'une existante.
- **Types d'équipe** : nom, catégorie (Agile, Bashy, Specialist…), coût de relance, règles spéciales.
- **Postes (PlayerPosition)** : caractéristiques, accès aux skills, mots-clés, compétences de départ.
- **Compétences** : nom, catégorie, description, traits/élite.
- **Limites par mot-clé** : plafonds globaux pour un type d'équipe.

Toutes les opérations sont validées (pas de suppression d'une entité référencée par une équipe / un joueur). Le clonage d'une `RulesVersion` recopie skills, team types, positions et limites dans une transaction atomique.

---

## Export / Import

Le JSON exporté résout les références par **nom** (Game, TeamType, PlayerPosition, Skill) pour rester portable entre instances. L'import recrée la ligue complète avec ses équipes, joueurs, compétences et résultats.

Accessible via la liste des ligues — boutons **Exporter** / **Importer**.

---

## Configuration

### Variables d'environnement (Docker)

| Variable | Description |
|---|---|
| `BolDeSang__AdminEmail` | Email du premier compte Admin |
| `BolDeSang__AdminPassword` | Mot de passe (≥ 8 chars, ≥ 1 chiffre) |
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
- [ ] Renseigner `UrlExterne` dans Admin → Paramètres (utilisée pour les QR codes)
- [ ] Configurer SMTP Gmail si vous voulez les notifications par email
- [ ] Sauvegarder le volume Docker contenant la base SQLite

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
