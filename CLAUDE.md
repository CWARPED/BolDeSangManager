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

Blazor Server (.NET 9) avec Identity, MudBlazor 8.6.0, EF Core 9 + SQLite, QuestPDF, QRCoder, Markdig.

```
src/BolDeSangManager/
├── Data/
│   ├── Models/         # Entités EF Core (Game, League, Team, Match, Skill, AppConfig…)
│   ├── Enums/          # Enums.cs — tous les enums du domaine
│   ├── Seeding/        # Seed BB + DB, seuils d'XP, catégories standard
│   ├── ApplicationDbContext.cs
│   ├── ApplicationUser.cs   # IdentityUser + PseudoCoach + EstSupprime
│   └── DbSeeder.cs     # Seed idempotent + migrations auto au démarrage
├── Helpers/            # DisplayHelpers (couleurs/labels), BrouillardHelpers, accès compétences
├── Services/           # Logique métier (tous Scoped)
│   ├── LeagueService       # Lifecycle ligue : créer → inscriptions → saison → playoffs → supprimer
│   ├── TeamService         # Roster : créer équipe, recruter, améliorer, VEA, corrections d'XP
│   ├── MatchService        # Saisir/confirmer feuille, après-match, clôture auto, PSP/blessures/gains
│   ├── CalendrierService   # Export iCalendar (.ics) RFC 5545 des matchs programmés
│   ├── StaffService        # Staff d'équipe, prix copiés par ligue (StaffType → LeagueStaffType → TeamStaff)
│   ├── MarkdownService     # Rendu des règlements (Markdig sans HTML brut + filtrage javascript:/data:)
│   ├── AuthorizationService# IAuthorizationService — toutes les vérifs de rôle
│   ├── DataEditService     # CRUD données de jeu + clonage de version + Réserve
│   ├── PdfService          # Export QuestPDF (feuille d'équipe A4 + QR code match)
│   ├── LeagueExportService # Export/import JSON d'une ligue complète
│   ├── GameDataExportService     # Export/import des données d'une RulesVersion
│   ├── PersonalDataExportService # Export RGPD du dossier d'un coach
│   ├── UserAccountService  # Suppression de compte : verdict, suppression dure ou anonymisation
│   ├── GmailEmailSender    # IEmailSender<ApplicationUser> via Gmail SMTP (MailKit)
│   └── SettingsService     # Clés/valeurs persistées en DB (AppConfig)
├── Components/
│   ├── Layout/             # MainLayout (dark MudBlazor), AccountLayout, NavMenu, MudProviders
│   ├── Account/Pages/      # Parcours NON connecté uniquement (Login, Register, mot de passe
│   │                       # oublié, confirmation d'e-mail) + SupprimerCompte — SSR
│   └── Pages/
│       ├── Admin/                  # Index (Utilisateurs, Paramètres), Donnees (CRUD + Réserve)
│       ├── Ligues/                 # Index, Creer (= création ET édition), Detail, CalendrierLibre, PhaseRepos
│       ├── Equipes/                # MaFeuille, Rejoindre, Detail
│       ├── Matchs/                 # Index, Feuille, Detail, Validation, ApresMatch
│       └── Profil/Index.razor      # /profil — pseudo, e-mail, mot de passe, RGPD, suppression
└── wwwroot/app.css     # Dark mode custom CSS pour pages Account (SSR)
```

⚠️ **Les pages Identity sont en SSR** : aucun composant MudBlazor *interactif* (`MudTextField`, `MudSelect`, `MudCheckBox`…) n'y fonctionne. Utiliser `InputText` + les classes `.account-*` de `app.css`, comme sur `Login.razor`. Les `MudButton` / `MudAlert` non interactifs passent, eux, sans problème. Ne jamais s'appuyer sur des classes **Bootstrap** (`row`, `col-*`, `btn`, `alert`, `form-floating`) : elles n'existent pas dans ce projet et le HTML tombe sans aucun style.

## Points clés du domaine

**Édition des paramètres d'une ligue** (`LeagueService.ModifierLigueAsync`, écran
`Ligues/Creer.razor` servi aussi sur `/ligues/{id}/modifier`) : **deux verrous distincts**,
centralisés dans `DisplayHelpers.ParametresLigueEditables` / `ParametresStructurantsEditables`.

1. Le **lancement de la saison** (`Statut >= EnCours`) ferme tout : le calendrier généré
   dépend du format, les feuilles de match déjà saisies du barème d'XP.
2. La **première équipe inscrite** ferme en plus les paramètres *structurants* — jeu,
   version des règles, budget de départ, staff de ligue — parce que des lignes déjà
   écrites en dépendent :
   - `Team.TeamTypeId` et `TeamPlayer.PlayerPositionId` pointent vers des lignes **propres
     à une `RulesVersion`** ; changer de version rendrait les équipes orphelines (race et
     postes inexistants).
   - `Team.Tresorerie` est calculée **une seule fois** à la création de l'équipe
     (`BudgetDepart − roster − staff`) puis **stockée** : modifier le budget ou un coût de
     staff après coup laisserait des trésoreries fausses et autoriserait des équipes
     hors budget. Le recalcul rétroactif a été explicitement **écarté** (option non retenue).

⚠️ Il n'y a plus d'écran `StaffLigue.razor` : il permettait de changer les coûts de staff
**à tout moment**, y compris ligue lancée, ce qui faussait exactement de la même façon la
trésorerie déjà figée. Le staff se règle depuis le formulaire de ligue, sous le même verrou.

⚠️ `Reglement` et `ModeBrouillard` ne passent **pas** par ce formulaire : ils ont leur
propre commande sur `Ligues/Detail.razor` et restent modifiables en cours de saison.
`ModifierLigueAsync` n'y touche jamais — un test le verrouille.

⚠️ La route `/ligues/creer` n'a plus l'attribut `[Authorize(Roles = "Admin,GrandCommissaire")]`
(un commissaire doit pouvoir éditer **sa** ligue). La garde de rôle en création est donc
**dans le code** d'`OnInitializedAsync` : ne pas la retirer en croyant l'attribut suffisant.

**Suppression de compte** (`UserAccountService`) : quatre FK pointent vers `ApplicationUser` en **Restrict** — `Team.CoachId`, `League.CommissaireId`, `MatchSheet.SaisiParId`, `LeagueCommissioner.UserId`. Supprimer la ligne d'un coach ayant joué échouerait, et passer ces FK en `Cascade` détruirait l'historique sportif **d'autres** coaches (une feuille de match est validée par deux personnes). D'où la règle : **suppression dure seulement si le compte n'a aucune trace, anonymisation sinon**. L'anonymisation garde la ligne mais écrase les données personnelles (email en `@local.invalid`, pseudo « Coach supprimé », `PasswordHash = null`, rôles retirés, `LockoutEnd` au maximum) — conforme au droit à l'effacement, sans casser les classements. L'ancienne adresse redevient libre pour une réinscription, qui repart d'un compte neuf.

⚠️ **Consigne pour le futur** : toute **nouvelle FK vers `ApplicationUser`** doit être soit *nullable + SetNull*, soit ajoutée au comptage de `EvaluerSuppressionAsync` — sinon la suppression dure d'un compte considéré comme vierge échouera au `SaveChanges`. **Ajouter aussi le nouveau lien à `PersonalDataExportService`** : ce qui rattache un compte à des données le rend exportable au titre du droit d'accès.

**Espace « Mon compte » : un seul écran, `/profil`.** Les pages générées par le squelette Identity (`/Account/Manage/*`) ont été **supprimées** : elles faisaient doublon avec `/profil` (pseudo, e-mail, mot de passe y sont déjà, en mieux) et étaient rendues en Bootstrap alors que le projet est en MudBlazor. Restent sous `/Account/` les seules pages du parcours *non connecté* (Login, Register, ForgotPassword, ResetPassword, ConfirmEmail…) plus `/compte/supprimer`. La **double authentification a été retirée** (pages de gestion et écrans de connexion `LoginWith2fa` / `LoginWithRecoveryCode`) : aucun compte ne l'utilisait et plus rien ne permettait de l'activer. `PasswordSignInAsync` ne traite donc plus `RequiresTwoFactor` — le réintroduire suppose de restaurer ces écrans.

**Export RGPD** (`PersonalDataExportService`, endpoint `POST /Account/ExporterMesDonnees`, bouton dans `/profil`) : l'export du squelette Identity ne sortait que les propriétés marquées `[PersonalData]`, soit l'e-mail et deux drapeaux techniques — sans le pseudo, les équipes ni les matchs, c'est-à-dire sans ce que l'association détient réellement sur la personne. Le service produit un JSON indenté, en français, accents non échappés : compte (pseudo, rôles, date d'inscription), équipes avec leurs joueurs, matchs **du point de vue du demandeur** (`MonEquipe` / `Adversaire` / `MonScore` sont inversés selon qu'il joue à domicile ou à l'extérieur), ligues gérées, feuilles saisies. Limite volontaire : les autres coaches n'apparaissent que par le **nom public** de leur équipe — leurs données leur appartiennent.

Affichage : utiliser `DisplayHelpers.NomCoach(user)` et **jamais** `PseudoCoach` directement, sinon un identifiant technique (`compte-supprime-a1b2c3d4`) s'affiche aux autres coaches.

**Catégorie d'équipe** (`TeamType.Categorie`, colonne SQL `CategorieLrb`) : catégorie officielle du LRB (p.94), de **1** (équipes les plus performantes, qui pardonnent le mieux les erreurs) à **4** (les plus faibles, souvent les équipes de « Minus »). **0 = non renseignée.** Purement **informative** : elle oriente le choix d'un nouveau coach et s'affiche sur l'écran de choix de race, la feuille d'équipe et le PDF, mais ne déclenche **aucune mécanique**. Dans le LRB elle sert aussi au Jeu Égal (les catégories basses reçoivent plus de points de compétence), que l'application ne gère pas — l'y brancher serait un chantier distinct.

⚠️ Il n'y a **ni valeur de seed ni backfill** : c'est un choix produit, les commissaires font la passe de saisie depuis l'Admin. Une base fraîchement migrée affiche donc « à renseigner » partout, ce qui est l'état attendu et non un bug.

⚠️ **Ne pas confondre avec `TeamType.StyleJeuObsolete`** (enum `TeamCategory` : Bashy/Staller/Agile/Specialist), un « style de jeu » maison absent du livre de règles. Sa colonne SQL garde son nom d'origine `Categorie` — d'où le `[Column]` explicite sur les deux propriétés — pour que la migration reste purement additive. Elle n'est plus ni écrite, ni affichée, ni clonée, ni exportée. Ne jamais la lire, ne jamais réordonner l'enum (EF persiste ces valeurs en `int`).

⚠️ L'export JSON d'une version nomme le champ **`CategorieLrb`**, volontairement différent de l'ancien `Categorie`. Réutiliser le même nom aurait fait relire un vieil export où `Categorie: 2` valait « Agile » comme « catégorie LRB 2 » — une donnée fausse et silencieuse. Un fichier antérieur s'importe donc avec `Categorie = 0`, ce qui est exact.

**Règles spéciales d'équipe** (`SpecialRule` + `TeamTypeSpecialRule`) : catalogue des règles du LRB (p.93-94) porté par la `RulesVersion`, comme les compétences et le staff, avec une liaison N-N vers les fiches d'équipe. **Liste ouverte, éditable depuis l'Admin** (onglet « Règles spéciales » de `/admin/donnees`) ; le rattachement à une race se fait depuis sa fiche.

Principe central : **une règle sans `Code` est purement DESCRIPTIVE** — elle s'affiche sur la feuille d'équipe et le PDF, et se joue à la table. C'est le cas par défaut, et celui de toute règle qu'une future édition apportera : l'association peut donc suivre une nouvelle saison sans développement. Un `Code` (voir `SpecialRuleCodes`) branche un comportement écrit une fois dans le code. **Une seule règle est branchée aujourd'hui** : « Favori de… ». Un test verrouille ce fait pour qu'ajouter un `Code` reste un geste conscient.

⚠️ `TeamType.ReglesSpeciales` (texte libre) reste en base et sert de **repli d'affichage** tant qu'une race n'a aucune règle rattachée. Il n'est plus la source de vérité.

**« Favori de… »** (`Team.DiviniteChoisie`) : les divinités permises sont définies **sur la fiche de race** (`TeamTypeSpecialRule.OptionsChoix`, CSV) ; le **commissaire** choisit celle de chaque équipe dans cette liste depuis la fiche d'équipe. Le coach ne saisit rien — le LRB rend le choix définitif. Une seule option = divinité **imposée**, assignée automatiquement à la création (Pestiférés → Nurgle) ; plusieurs options = l'équipe naît sans divinité plutôt qu'avec un choix arbitraire. `DefinirDiviniteAsync` valide côté service contre les options de la race et enregistre la forme canonique de la liste, pas la saisie brute.

⚠️ Un cas particulier (ex. Nordiques, dont le LRB conditionne Khorne au choix de la ligue thématique — non modélisé) se règle en **élargissant les options de la race**, jamais en rattachant une règle à une équipe isolée : il n'existe pas de règle spéciale par équipe, et en introduire une obligerait à fusionner deux sources partout où les règles sont lues.

**Rôles** : `Commissaire` et `Coach` (ASP.NET Identity). Le commissaire crée les ligues et administre ; les coaches créent des équipes, saisissent les feuilles et **font tourner le match de bout en bout sans lui**.

**Cycle de vie d'une ligue** :
`Creation` → `Inscription` → `EnCours` → `PlayOffs` → `Termine`

**Cycle de vie d'un match** :
`Programme` → `AJouer` → `FeuilleEnSaisie` → `ValidationCompetences` → `Termine`

⚠️ **`ValidationCompetences` est mal nommé** : ce n'est PAS une attente de validation par le commissaire, c'est la **phase d'après-match des deux coaches** (c'est d'ailleurs le libellé affiché par `DisplayHelpers` : « Après-match »). Chaque coach y dépense ses XP, recrute et achète ses relances via `ValiderApresMatchAsync` ; **quand les deux ont validé, le match passe à `Termine` tout seul** (auto-clôture dans `MatchService`). Le commissaire n'est sur le chemin de personne.

**Le commissaire n'est jamais requis pour jouer.** Ses actions sur un match sont toutes *optionnelles et correctives* : éditer une feuille erronée, et « Forcer la clôture » — explicitement décrit dans l'UI comme réservé au cas où un coach ne peut pas faire son après-match. Le bouton « Valider les XP » de `Matchs/Detail.razor` ouvre cette page corrective ; **il ne conditionne aucune progression**. Conséquence vérifiée en conditions réelles : une ligue dont le commissaire supprime son compte **continue normalement** — saisie, confirmation, après-match, classement, tout fonctionne entre coaches.

**PSP (Points Star Player)** : calculés à la saisie de la feuille (TD×3, Completion×1, Interception×2, Élim×2, MVP+4). Les seuils ouvrent droit à une amélioration que **le coach choisit lui-même** pendant son après-match (`AppliquerAmeliorationAsync`) — aucune approbation d'un tiers n'est requise.

**Gains de match** : saisis manuellement par le coach sur la feuille de match. `MatchService.CalculerGains` fournit une estimation (affluence × 10k × 0,5 + TDs × 10k) affichée en indication, mais la valeur saisie dans les champs `GainsDomicile` / `GainsExterieur` est celle persistée.

**Compétences** : distinguer `EstCompetenceDepart` (liée au poste, chargée via `PlayerPosition.CompetencesDepart`) des compétences acquises (`TeamPlayerSkill.EstCompetenceDepart = false`). `GetEquipeAsync` filtre `Competences.Where(c => !c.EstCompetenceDepart)` — les compétences de départ passent uniquement par `PlayerPosition`.

**Réserve (pool de joueurs socle)** : `PoolPosition` est un poste réutilisable défini au niveau d'une `RulesVersion` (jumeau de `PlayerPosition`, avec ses `PoolPositionSkill` de départ). Design **catalogue/copie**, **bidirectionnel** :
- **Réserve → équipe** : depuis l'édition d'une équipe (race BB / collège DB), le bouton « Importer depuis la Réserve » **copie** les postes choisis en `PlayerPosition` du `TeamType` (`ImporterReserveVersTeamTypeAsync`, sélection multiple).
- **Équipe → Réserve** : dans la colonne *Actions* de la table des postes, l'icône « inventaire » renvoie une **copie** du poste dans la Réserve de sa version (`ExporterPosteVersReserveAsync`, un poste à la fois). Le poste **reste** dans le `TeamType`. Si un poste de réserve porte **déjà le même nom** dans cette version (comparaison insensible à la casse), l'opération est **refusée** avec un message — pas d'écrasement ni de renommage automatique.

Dans les deux sens la copie est **indépendante** (modifier ou supprimer l'un n'affecte pas l'autre) et les compétences de départ suivent. Aucun impact sur le recrutement / la VEA / le PDF : un poste importé est un `PlayerPosition` normal. La Réserve est **par version** : clonée avec la version (`DataEditService.ClonerVersionAsync`) et incluse dans l'export/import complet (`GameDataExportService`, champ `Reserve` optionnel = rétrocompat). En plus, export/import de la **Réserve seule** (`ExportReserveAsync` / `ImportReserveAsync`, import en mode *append*, skills résolus par nom). CRUD via `DataEditService` (`GetReserveAsync`/`AjouterReserveAsync`/`ModifierReserveAsync`/`SupprimerReserveAsync`/`ImporterReserveVersTeamTypeAsync`/`ExporterPosteVersReserveAsync`) et UI dans Admin → Données → onglet **Réserve**. Pool **vide** à l'installation (aucun seed).

**Catégories de compétence** : `SkillCategoryDef` est une **table** portée par une `RulesVersion` (comme la Réserve) — l'ancien enum `SkillCategory` n'est plus la source de vérité. Chaque catégorie a un `Nom` (libellé complet) et un `Code` de **1 à 2 caractères** (étiquette d'affichage uniquement), tous deux **uniques par version** (comparaison insensible à la casse, code normalisé en majuscules). CRUD via `DataEditService` (`GetCategoriesAsync`/`CreerCategorieAsync`/`ModifierCategorieAsync`/`SupprimerCategorieAsync`) et UI dans Admin → Données → onglet **Compétences**, panneau dépliable « Catégories » en tête de l'onglet (replié par défaut ; pas d'onglet dédié). Pas de champ d'ordre : les catégories sont triées **par nom**, les compétences par nom de catégorie puis par nom. Règles : **édition toujours autorisée** (les `Skill` pointent vers `SkillCategoryDefId`, pas vers la lettre) ; **suppression refusée** si au moins une compétence l'utilise (message indiquant le nombre). Les catégories sont clonées avec la version et incluses dans l'export JSON (champ `Categories` optionnel ; `SkillGdDto.CategorieNom` optionnel = rétrocompat, repli sur l'ancien enum via `StandardSkillCategories`). Migration `AddSkillCategories` : matérialise les 6 catégories standard pour chaque version existante et rattache les compétences (backfill SQL).

**Cycle de vie d'une version de règles** (`DataEditService`) — invariants à ne pas casser :
- **Une version active par JEU** (pas une globale). `EstActive` est la version présélectionnée dans l'admin et proposée par défaut à la création d'une ligue (`Ligues/Creer.razor`). `ActiverVersionAsync` bascule le statut en filtrant sur `v.GameId` : désactiver « toutes les autres » sans ce filtre casserait le jeu voisin. Deux versions actives simultanément est donc l'état **normal** avec Blood Bowl + Dungeon Bowl. Bouton dédié (icône ✓) dans Admin → Données → Versions, désactivé sur la version déjà active.
- **Créer une version ne demande que le nom** (+ clonage optionnel). `Ordre` et `EstActive` ne sont plus saisis : l'ordre est calculé automatiquement (max du jeu + 1) et la version n'est active que si c'est la **première du jeu** — sinon on bascule explicitement via le bouton ✓. La colonne `Ordre` reste en base (elle sert au tri de `GetVersionsAsync` / `LeagueService`) mais n'est plus affichée. Le nom est validé à la création comme au renommage (non vide, ≤ 100 caractères, unique par jeu).
- **Renommer est toujours autorisé** (`RenommerVersionAsync`, icône crayon) : tout est lié par id, jamais par libellé — une version active ou utilisée par des ligues se renomme sans risque. Seules contraintes : nom non vide, ≤ 100 caractères, et **unique au sein du même jeu** (insensible à la casse) ; le même nom reste permis dans l'autre jeu. Le nom est `Trim()` avant enregistrement.
- `CreerVersionAsync` ouvre **une seule transaction** englobant la création ET le clonage ; `ClonerVersionAsync` n'ouvre donc **pas** la sienne (transactions imbriquées interdites sur SQLite). Sans cela, un clonage en échec laissait une version vide résiduelle à chaque erreur.
- Le clonage rattache chaque `Skill` à la catégorie clonée ; si la source pointe vers une catégorie d'une **autre version** (donnée incohérente), repli **par nom** sur la catégorie standard homonyme, sinon exception explicite. Recopier l'id étranger provoquait `FOREIGN KEY constraint failed`.
- `CreerSkillAsync` / `ModifierSkillAsync` refusent une catégorie n'appartenant pas à la version de la compétence (`VerifierCategorieDeLaVersionAsync`) — garde-fou qui empêche la corruption de réapparaître.
- `SupprimerVersionAsync` supprime la **Réserve avant** les compétences et les catégories (`PoolPositionSkill → Skill` et `PoolPositionCategoryAccess → SkillCategoryDef` sont en `Restrict`, FK non nullable), et **refuse** la suppression si une `League` référence la version (`League → RulesVersion` est en cascade par convention : sans ce garde-fou, supprimer une version effaçait silencieusement les ligues et leurs matchs).
- Migration `ReparerCategoriesOrphelines` : réparation de **données** uniquement (aucun changement de schéma), idempotente — recrée les catégories standard manquantes par version et réaffecte les seules compétences dont la catégorie est hors de leur version.

✅ **R2b fait** : les accès de compétence d'un poste passent par la table **`PlayerPositionCategoryAccess`** (jumelle `PoolPositionCategoryAccess` pour la Réserve) — un lien vers `SkillCategoryDef` + un booléen principal/secondaire. Les codes de catégorie à 2 lettres sont donc pleinement utilisables. Lire les accès via `CategoryAccessHelpers` / `AccesCompetencesHelpers`, jamais en découpant une chaîne. Les colonnes `CompetencesPrincipales` / `CompetencesSecondaires` (`TeamType`, `PoolPosition`) subsistent en base par compatibilité mais **ne sont plus la source de vérité** : ne pas les réintroduire dans du code neuf.

**Accès aux compétences à l'après-match** (`AccesCompetencesHelpers`, R7) : on ne propose que les catégories accessibles au poste ; principal/secondaire est **déduit** de l'accès (et pilote la hausse de valeur +20k / +40k) au lieu d'être coché à la main ; les compétences Élite sont masquées par défaut ; une case « hors accès » réaffiche tout, garde-fou franchissable en connaissance de cause pour les règles maison.

**Export JSON** (`LeagueExportService`) : résout les références par nom (Game, TeamType, PlayerPosition, Skill) pour être portable entre instances.

**QR code dans le PDF** : `PdfService.GenererFeuilleEquipe` accepte `matchProchain` et `urlExterne`. Si les deux sont fournis, un bloc QR code est inséré dans la feuille d'équipe, pointant vers `{urlExterne}/matchs/{matchId}/feuille`. L'URL externe se configure dans Admin > Paramètres via `SettingsService` (`CleUrlExterne = "UrlExterne"`).

**AppConfig** : table clé/valeur en base pour stocker les paramètres runtime de l'application (ex. URL externe). Gérée via `SettingsService.GetAsync` / `SetAsync`.

## Conventions importantes

**Pages InteractiveServer vs SSR** : les pages sous `Components/Account/` sont SSR (`EditForm method="post"` + `[SupplyParameterFromForm]`). `MudTextField` ne génère pas d'attribut `name` → utiliser `InputText` avec CSS custom pour ces pages.

**MudBlazor** : ne pas utiliser `MudTooltip` dans les pages InteractiveServer — casse le circuit Blazor (nécessite `MudPopoverProvider` dans le même arbre interactif). Utiliser l'attribut HTML `title` à la place (ou, pour un usage tactile, un overlay custom au clic — cf. page de création d'équipe).

**Page création d'équipe (`Equipes/Rejoindre.razor`)** : barre de budget **collante** en haut (`.budget-sticky` dans `app.css`, fond en jauge `restant/départ` via les propriétés `_budgetGauge`/`_budgetPct`, `top` calé sur la hauteur de la `MudAppBar`, 64px). Les postes dont un **mot-clé est sous limite** (`TeamType.LimitesMotsCles`) sont regroupés en cadres colorés (un par mot-clé, couleur cyclique) via `RosterDisplayHelpers.GroupePostesParMotCleLimite` ([Helpers/RosterDisplayHelpers.cs](src/BolDeSangManager/Helpers/RosterDisplayHelpers.cs)) ; les autres restent en cartes classiques. Le détail d'une **compétence** s'ouvre en **overlay au clic** (`.skill-overlay-*`) — pas de `title`/hover, pour rester utilisable au doigt. La logique de regroupement est pure et testée ([RosterDisplayHelpersTests.cs](tests/BolDeSangManager.Tests/RosterDisplayHelpersTests.cs)).

**DbSeeder** : s'exécute à chaque démarrage via `DbSeeder.SeedAsync(app.Services)`. Il est idempotent (vérifie `!db.Games.Any()` avant d'insérer). Appelle aussi `db.Database.MigrateAsync()` — les nouvelles migrations sont donc appliquées automatiquement au démarrage.

**DisplayHelpers** : `Helpers/DisplayHelpers.cs` centralise les méthodes `LeagueColor`, `LeagueLabel`, `LeagueFormatLabel`, `EstFormatLibre`, `AvecPlayoffs`, `MatchColor`, `MatchLabel`, `MatchBorderStyle`, ainsi que les constantes `LabelMvp` / `LabelMvpLong`. Importées globalement via `@using static BolDeSangManager.Helpers.DisplayHelpers` dans `Components/_Imports.razor` — appeler directement sans qualificateur dans tous les composants.

⚠️ **Vocabulaire : on dit « JPV » (Joueur le Plus Valeureux), pas « MVP ».** Le libellé affiché vient de `DisplayHelpers.LabelMvp` — ne jamais réécrire « MVP » en dur dans une vue. Les identifiants du code (`EstMVP`, `AwardType.MVP`, `BonusMvp`, `XpBonusMvp`) gardent en revanche leur nom : ce sont des noms techniques, persistés en base pour certains. Plus généralement, tout nouveau libellé destiné à être traduit un jour passe par un helper plutôt que par du texte en dur (cf. ticket #7, i18n).

**Formats de ligue** (`LeagueFormat`) : `RoundRobin`, `RoundRobinAvecPlayoffs`, `Libre`, `LibreAvecPlayoffs`, `Open`. ⚠️ Ces valeurs sont **persistées en int** : ne jamais réordonner l'enum, toute nouvelle entrée s'ajoute **à la fin** (un test le verrouille). Ne pas comparer les formats à la main dans les composants : utiliser `DisplayHelpers.EstFormatLibre(...)` et `DisplayHelpers.AvecPlayoffs(...)`.

Les formats **Libre** délèguent la composition du calendrier au commissaire : `LancerSaisonAsync` crée bien la division par défaut (nécessaire pour rattacher les matchs) mais **n'appelle pas** `GenererPoolMatchsAsync` — la ligue démarre avec un calendrier vide. Le commissaire compose ensuite les rondes depuis `Components/Pages/Ligues/CalendrierLibre.razor` (`/ligues/{id}/calendrier`), qui s'appuie sur `DefinirRondeAsync` (créer/remplacer une ronde) et `SupprimerRondeAsync`. Règles : une équipe ne joue qu'un match par ronde ; une équipe non citée est **au repos** (autorisé) ; une même paire peut se rejouer dans une autre ronde (aller-retour voulu) ; une ronde dont un match est déjà joué est **verrouillée** côté service *et* côté UI. `NbRondes` n'est pas stocké — il se déduit de `max(Ronde)`, d'où **aucune migration** pour cette fonctionnalité.

⚠️ `GetLigueAsync` charge `l.Equipes` **directement**, en plus de `l.Divisions.Equipes` : en format Libre les équipes n'ont pas encore de division au moment de composer le calendrier, et ne passer que par les divisions les rendait invisibles (bug rencontré et corrigé).

**Échéance de ronde** (`EcheanceRonde`, table dédiée, migration `AjoutEcheanceRonde` purement additive) : date **indicative** à laquelle les matchs d'une ronde devraient être joués — rien n'est bloqué ni clôturé automatiquement. Unicité `(LeagueId, Ronde)`, cascade depuis `League`, supprimée avec sa ronde. Table plutôt qu'une colonne sur `Match` : une ronde n'est pas une entité en base (juste un numéro), et dupliquer la date sur chaque match la rendrait incohérente.

⚠️ **Piège de l'écran de composition** : les rondes ajoutées n'existent en base qu'après clic sur « Enregistrer ». `ChargerRondes()` doit donc **préserver les rondes non enregistrées** (`RondeVm.JamaisEnregistree`), et supprimer une telle ronde se fait **localement**, sans appel serveur ni rechargement — sinon enregistrer ou supprimer UNE ronde efface toutes les autres en attente (bug signalé : « supprimer la dernière ronde supprime tout »). Pour la même raison, `ProposerAppariementsAsync` accepte un paramètre `dejaComposees` : sans lui, deux rondes créées d'affilée proposent les mêmes rencontres puisque rien n'est encore en base.

L'appariement de « Compléter » n'est pas un simple parcours dans l'ordre : il choisit, parmi les équipes libres, celles qui se sont le moins souvent affrontées (puis les moins servies), et inverse domicile/extérieur par rapport à la dernière confrontation.

**Mode brouillard — périmètre exact** (`BrouillardHelpers`) : le secret porte sur **l'appariement**, pas sur les effectifs. Les fiches d'équipe sont **publiques par choix** (cf. #5, fermé comme comportement voulu) ; en mode brouillard on masque donc uniquement (a) le calendrier à venir au-delà du prochain match (`FiltrerVisibles` / `EstVisible`, accès direct à une page de match compris) et (b) la **fiche du prochain adversaire** (`PeutVoirFicheEquipe`, contrôle au chargement de `Equipes/Detail.razor`, pas un masquage visuel). Restent visibles : ses propres équipes, les adversaires déjà affrontés, ceux des rondes ultérieures, et tout pour un commissaire.

**GmailEmailSender** : implémente `IEmailSender<ApplicationUser>` (Scoped). Lit les credentials à chaque envoi depuis `SettingsService` — pas besoin de redémarrer après modification en admin. Si `EmailExpediteur` ou `EmailMotDePasse` manque, log un warning et ignore silencieusement. Nécessite un **Mot de passe d'application** Gmail (16 chars, généré sur myaccount.google.com) — pas le mot de passe du compte. SMTP : `smtp.gmail.com:587 StartTls`. Clés `SettingsService` :
- `SettingsService.CleEmailExpediteur`    → adresse Gmail de l'expéditeur
- `SettingsService.CleEmailNomExpediteur` → nom affiché dans la boîte de réception
- `SettingsService.CleEmailMotDePasse`    → mot de passe d'application Gmail
- `SettingsService.CleUrlExterne`         → URL publique de l'app (QR codes PDF)

Configuration via **Admin → Paramètres → Email**. Un bouton "Tester" envoie un email de test à l'adresse saisie.

**JS helpers** (dans `App.razor` inline script) :
- `blazorDownloadBase64File(filename, mimeType, base64)` — téléchargement navigateur
- `clickElement(id)` — déclenche `.click()` sur un élément (utilisé pour `InputFile` caché)

**Jeton antiforgery périmé** (`Program.cs`, middleware placé **après** `UseAntiforgery`) : un POST au jeton absent ou expiré produisait un 400 en texte brut anglais (« A valid antiforgery token was not provided… ») — page blanche pour le coach. Cas typique : onglet mobile laissé ouvert, purgé par le système, formulaire restauré depuis le cache avec un jeton orphelin. Le middleware redirige désormais vers `/Account/Login?expire=1` (`Login.razor` affiche « Votre session a expiré »).

⚠️ L'ordre est essentiel : `UseAntiforgery` **ne coupe pas** le pipeline, il range son verdict dans `IAntiforgeryValidationFeature` et c'est l'endpoint, plus bas, qui renvoie le 400. Un middleware placé **avant** ne voit donc jamais rien. Le `catch` complémentaire couvre les endpoints Identity (`Logout`, `LinkExternalLogin`) qui lient le formulaire eux-mêmes et lèvent `AntiforgeryValidationException`.

**Clés DataProtection** : `DataProtection__KeysPath` (défini dans le `Dockerfile` sur `/data/DataProtection-Keys`) doit rester **dans le volume persistant**. Hors volume, les clés sont régénérées à chaque redémarrage : tous les utilisateurs sont déconnectés et les formulaires ouverts échouent — même symptôme que ci-dessus, mais généralisé.
