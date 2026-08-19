# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build src/BolDeSangManager/BolDeSangManager.csproj

# Run (dev, port 5129)
cd src/BolDeSangManager && dotnet run

# EF Core migrations
cd src/BolDeSangManager
dotnet ef migrations add <NomMigration>
dotnet ef database update

# Reset DB (dev only — supprime et recrée)
rm Data/boldesang.db && dotnet run   # le DbSeeder repeuple automatiquement

# Tests
dotnet test tests/BolDeSangManager.Tests/BolDeSangManager.Tests.csproj
```

## Architecture

Blazor Server (.NET 9) avec Identity, MudBlazor 8.6.0, EF Core 9 + SQLite, QuestPDF, QRCoder.

```
src/BolDeSangManager/
├── Data/
│   ├── Models/         # Entités EF Core (Game, League, Team, Match, Skill, AppConfig…)
│   ├── Enums/          # Enums.cs — tous les enums du domaine
│   ├── ApplicationDbContext.cs
│   ├── ApplicationUser.cs   # IdentityUser + PseudoCoach
│   └── DbSeeder.cs     # Seed au démarrage : jeux, races BB, collèges DB, ~80 compétences
├── Helpers/
│   └── DisplayHelpers.cs   # Méthodes statiques partagées (couleurs/labels des enums)
├── Services/           # Logique métier (tous Scoped)
│   ├── LeagueService       # Lifecycle ligue : créer → inscriptions → saison → playoffs → supprimer
│   ├── TeamService         # Roster : créer équipe, recruter joueur, attribuer compétence, VEA
│   ├── MatchService        # Saisir feuille, valider, calculer PSP/blessures/gains
│   ├── PdfService          # Export QuestPDF (feuille d'équipe A4 + QR code match)
│   ├── GmailEmailSender    # IEmailSender<ApplicationUser> via Gmail SMTP (MailKit)
│   ├── SettingsService     # Clés/valeurs persistées en DB (AppConfig)
│   └── LeagueExportService # Export/import JSON d'une ligue complète
├── Components/
│   ├── Layout/             # MainLayout (dark MudBlazor), NavMenu
│   ├── Account/Pages/      # Login, Register — SSR (EditForm method="post"), CSS custom
│   └── Pages/
│       ├── Admin/Index.razor       # Tabs : Utilisateurs, Paramètres (URL externe)
│       ├── Ligues/                 # Index, Creer, Detail (classement + pool matchs + équipes)
│       ├── Equipes/                # MaFeuille, Creer, Detail
│       └── Matchs/                 # Index, Feuille (saisie), Validation
└── wwwroot/app.css     # Dark mode custom CSS pour pages Account (SSR)
```

## Points clés du domaine

**Rôles** : `Commissaire` et `Coach` (ASP.NET Identity). Le commissaire crée les ligues et valide les matchs. Les coaches créent des équipes et saisissent les feuilles.

**Cycle de vie d'une ligue** :
`Creation` → `Inscription` → `EnCours` → `PlayOffs` → `Termine`

**Cycle de vie d'un match** :
`Programme` → `AJouer` → `FeuilleEnSaisie` → `ValidationCompetences` → `Termine`

**PSP (Points Star Player)** : calculés à la saisie de la feuille (TD×3, Completion×1, Interception×2, Élim×2, MVP+4). À 6 PSP, le joueur a droit à une compétence (validée par le commissaire).

**Gains de match** : saisis manuellement par le coach sur la feuille de match. `MatchService.CalculerGains` fournit une estimation (affluence × 10k × 0,5 + TDs × 10k) affichée en indication, mais la valeur saisie dans les champs `GainsDomicile` / `GainsExterieur` est celle persistée.

**Compétences** : distinguer `EstCompetenceDepart` (liée au poste, chargée via `PlayerPosition.CompetencesDepart`) des compétences acquises (`TeamPlayerSkill.EstCompetenceDepart = false`). `GetEquipeAsync` filtre `Competences.Where(c => !c.EstCompetenceDepart)` — les compétences de départ passent uniquement par `PlayerPosition`.

**Réserve (pool de joueurs socle)** : `PoolPosition` est un poste réutilisable défini au niveau d'une `RulesVersion` (jumeau de `PlayerPosition`, avec ses `PoolPositionSkill` de départ). Design **catalogue/copie**, **bidirectionnel** :
- **Réserve → équipe** : depuis l'édition d'une équipe (race BB / collège DB), le bouton « Importer depuis la Réserve » **copie** les postes choisis en `PlayerPosition` du `TeamType` (`ImporterReserveVersTeamTypeAsync`, sélection multiple).
- **Équipe → Réserve** : dans la colonne *Actions* de la table des postes, l'icône « inventaire » renvoie une **copie** du poste dans la Réserve de sa version (`ExporterPosteVersReserveAsync`, un poste à la fois). Le poste **reste** dans le `TeamType`. Si un poste de réserve porte **déjà le même nom** dans cette version (comparaison insensible à la casse), l'opération est **refusée** avec un message — pas d'écrasement ni de renommage automatique.

Dans les deux sens la copie est **indépendante** (modifier ou supprimer l'un n'affecte pas l'autre) et les compétences de départ suivent. Aucun impact sur le recrutement / la VEA / le PDF : un poste importé est un `PlayerPosition` normal. La Réserve est **par version** : clonée avec la version (`DataEditService.ClonerVersionAsync`) et incluse dans l'export/import complet (`GameDataExportService`, champ `Reserve` optionnel = rétrocompat). En plus, export/import de la **Réserve seule** (`ExportReserveAsync` / `ImportReserveAsync`, import en mode *append*, skills résolus par nom). CRUD via `DataEditService` (`GetReserveAsync`/`AjouterReserveAsync`/`ModifierReserveAsync`/`SupprimerReserveAsync`/`ImporterReserveVersTeamTypeAsync`/`ExporterPosteVersReserveAsync`) et UI dans Admin → Données → onglet **Réserve**. Pool **vide** à l'installation (aucun seed).

**Export JSON** (`LeagueExportService`) : résout les références par nom (Game, TeamType, PlayerPosition, Skill) pour être portable entre instances.

**QR code dans le PDF** : `PdfService.GenererFeuilleEquipe` accepte `matchProchain` et `urlExterne`. Si les deux sont fournis, un bloc QR code est inséré dans la feuille d'équipe, pointant vers `{urlExterne}/matchs/{matchId}/feuille`. L'URL externe se configure dans Admin > Paramètres via `SettingsService` (`CleUrlExterne = "UrlExterne"`).

**AppConfig** : table clé/valeur en base pour stocker les paramètres runtime de l'application (ex. URL externe). Gérée via `SettingsService.GetAsync` / `SetAsync`.

## Conventions importantes

**Pages InteractiveServer vs SSR** : les pages sous `Components/Account/` sont SSR (`EditForm method="post"` + `[SupplyParameterFromForm]`). `MudTextField` ne génère pas d'attribut `name` → utiliser `InputText` avec CSS custom pour ces pages.

**MudBlazor** : ne pas utiliser `MudTooltip` dans les pages InteractiveServer — casse le circuit Blazor (nécessite `MudPopoverProvider` dans le même arbre interactif). Utiliser l'attribut HTML `title` à la place (ou, pour un usage tactile, un overlay custom au clic — cf. page de création d'équipe).

**Page création d'équipe (`Equipes/Rejoindre.razor`)** : barre de budget **collante** en haut (`.budget-sticky` dans `app.css`, fond en jauge `restant/départ` via les propriétés `_budgetGauge`/`_budgetPct`, `top` calé sur la hauteur de la `MudAppBar`, 64px). Les postes dont un **mot-clé est sous limite** (`TeamType.LimitesMotsCles`) sont regroupés en cadres colorés (un par mot-clé, couleur cyclique) via `RosterDisplayHelpers.GroupePostesParMotCleLimite` ([Helpers/RosterDisplayHelpers.cs](src/BolDeSangManager/Helpers/RosterDisplayHelpers.cs)) ; les autres restent en cartes classiques. Le détail d'une **compétence** s'ouvre en **overlay au clic** (`.skill-overlay-*`) — pas de `title`/hover, pour rester utilisable au doigt. La logique de regroupement est pure et testée ([RosterDisplayHelpersTests.cs](tests/BolDeSangManager.Tests/RosterDisplayHelpersTests.cs)).

**DbSeeder** : s'exécute à chaque démarrage via `DbSeeder.SeedAsync(app.Services)`. Il est idempotent (vérifie `!db.Games.Any()` avant d'insérer). Appelle aussi `db.Database.MigrateAsync()` — les nouvelles migrations sont donc appliquées automatiquement au démarrage.

**DisplayHelpers** : `Helpers/DisplayHelpers.cs` centralise les méthodes `LeagueColor`, `LeagueLabel`, `LeagueFormatLabel`, `MatchColor`, `MatchLabel`, `MatchBorderStyle`. Importées globalement via `@using static BolDeSangManager.Helpers.DisplayHelpers` dans `Components/_Imports.razor` — appeler directement sans qualificateur dans tous les composants.

**GmailEmailSender** : implémente `IEmailSender<ApplicationUser>` (Scoped). Lit les credentials à chaque envoi depuis `SettingsService` — pas besoin de redémarrer après modification en admin. Si `EmailExpediteur` ou `EmailMotDePasse` manque, log un warning et ignore silencieusement. Nécessite un **Mot de passe d'application** Gmail (16 chars, généré sur myaccount.google.com) — pas le mot de passe du compte. SMTP : `smtp.gmail.com:587 StartTls`. Clés `SettingsService` :
- `SettingsService.CleEmailExpediteur`    → adresse Gmail de l'expéditeur
- `SettingsService.CleEmailNomExpediteur` → nom affiché dans la boîte de réception
- `SettingsService.CleEmailMotDePasse`    → mot de passe d'application Gmail
- `SettingsService.CleUrlExterne`         → URL publique de l'app (QR codes PDF)

Configuration via **Admin → Paramètres → Email**. Un bouton "Tester" envoie un email de test à l'adresse saisie.

**JS helpers** (dans `App.razor` inline script) :
- `blazorDownloadBase64File(filename, mimeType, base64)` — téléchargement navigateur
- `clickElement(id)` — déclenche `.click()` sur un élément (utilisé pour `InputFile` caché)
