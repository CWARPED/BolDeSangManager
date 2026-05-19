# Refonte de la base BolDeSangManager : alignement Blood Bowl S3 / Dungeon Bowl

**Date** : 2026-05-18
**Statut** : Design validé en brainstorming, prêt pour implémentation
**Sources de référence** : `docs/regles/bloodbowl.md`, `docs/regles/dungeonbowl.md`

---

## 1. Objectif

Aligner le modèle de données et la logique métier de l'application sur les règles **Blood Bowl LRB Saison 3** (BB) et **Dungeon Bowl Edition 2022** (DB), telles qu'extraites dans `docs/regles/`. Préparer une base propre qui supporte le cycle de vie complet d'une ligue : inscription → saison régulière → phase de repos → playoffs → fin, avec après-match (XP, achats, relances), tracking des RPM, et classements transverses (joueurs / coachs / récompenses).

## 2. Hors-scope

- Anciennes saisons LRB S1/S2 : les PDFs extraits ne contiennent que des renvois ponctuels, pas de rosters/skills complets. `RulesVersion` reste un champ informatif sur `League`, sans influence logique.
- Implémentation des règles de plateau (mouvement, blocage, dés) — l'app gère uniquement la ligue, pas le jeu sur table.
- Multi-saisons d'une même ligue (pas demandé pour l'instant).

## 3. État actuel & écarts

Voir `CLAUDE.md` pour la structure existante. Écarts identifiés et résolus dans ce design :

| # | Écart actuel | Résolution |
|---|---|---|
| 1 | Pas de "match de repos" avant playoffs ; `ManqueSuivantMatch` jamais reset auto | Nouvelle phase `PhaseDeRepos` qui reset les RPM + permet un après-match |
| 2 | `CalculerPSP` ignore `gameType` (TD=3 partout) | TD = 5 en DungeonBowl, 3 en BloodBowl |
| 3 | Améliorations PSP : "-6 par compétence" en boucle (incorrect) | Nouvelle table `PlayerImprovement` + paliers LRB S3 (6/16/31/51/76/176) |
| 4 | `TeamType.Categorie` int 1-4 sans sémantique | Enum `TeamCategory { Bashy, Staller, Agile, Specialist }` |
| 5 | Pas de classements joueur/coach/récompenses | Nouvelles méthodes service + entité `LeagueAward` |
| 6 | Gains identiques pour BB et DB | Saisie libre conservée — les coachs lancent les dés eux-mêmes et entrent les valeurs. Méthode `MatchService.CalculerGains` (estimation indicative) inchangée. |
| 7 | ~85 skills incomplets, manque les 4 spécifiques DB | Reseed complet ~115 skills + 4 DB-only |
| 8 | Rosters approximatifs vs PDFs | Reseed fidèle au LRB S3 / DB Edition 2022 |

## 4. Modifications du modèle de données

### 4.1 Nouveaux enums

```csharp
// Enums.cs
public enum LeagueStatus
{
    Creation,
    Inscription,
    EnCours,
    PhaseDeRepos,   // NOUVEAU : entre saison régulière et playoffs
    PlayOffs,
    Termine
}

public enum TeamCategory
{
    Bashy,        // Nains, OrquesNoirs, ChaosDwarfs, etc.
    Staller,      // Élus du Chaos, Norses, Khorne
    Agile,        // Elfes (tous), Skavens, Amazones, Lizardmen Skinks
    Specialist    // Halflings, Snotlings, Ogres, Gobelins, Bas-fonds, Vampires
}

public enum ImprovementType
{
    AleaPrimaire,           // Tirage D6 sur catégorie primaire
    SelectionPrimaire,      // Choix dans la catégorie primaire
    AleaSecondaire,         // Tirage D6 sur catégorie secondaire
    SelectionSecondaire,    // Choix dans la catégorie secondaire
    AmeliorationCarac,      // +1 sur M, AG, CP (selon D8)
    AmeliorationForceArmure // +1 F ou +1 AR
}

public enum AwardType
{
    Champion,            // Vainqueur ligue
    MVP,                 // Meilleur joueur ligue
    MeilleurMarqueur,    // Plus de TDs
    MeilleurDefenseur,   // Plus d'éliminations
    MeilleurPasseur,     // Plus de completions+interceptions
    MeilleurCoach        // Plus de victoires / points
}
```

### 4.2 Nouvelles entités

```csharp
// Models/PlayerImprovement.cs
public class PlayerImprovement
{
    public int Id { get; set; }
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;
    public int Palier { get; set; }              // 1..6 (6, 16, 31, 51, 76, 176 PSP)
    public ImprovementType Type { get; set; }
    public int? SkillId { get; set; }            // si Aléa* ou Selection*
    public Skill? Skill { get; set; }
    public AffectedStat? StatAmelioree { get; set; } // si Amelioration*
    public int ValeurHausse { get; set; }        // kpo ajoutés à ValeurActuelle
    public DateTime AppliqueLe { get; set; } = DateTime.UtcNow;
    public bool EnAttenteValidation { get; set; } = false;
    public int? MatchSheetId { get; set; }       // après-match d'origine (nullable pour phase de repos)
}

// Models/PhaseDeReposValidation.cs
public class PhaseDeReposValidation
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTime ValideLe { get; set; } = DateTime.UtcNow;
}

// Models/LeagueAward.cs
public class LeagueAward
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public AwardType Type { get; set; }
    public int? TeamPlayerId { get; set; }      // pour MVP, MeilleurMarqueur, etc.
    public TeamPlayer? TeamPlayer { get; set; }
    public int? TeamId { get; set; }            // pour Champion
    public Team? Team { get; set; }
    public string? CoachId { get; set; }        // pour MeilleurCoach
    public ApplicationUser? Coach { get; set; }
    public DateTime AttribueLe { get; set; } = DateTime.UtcNow;
}
```

### 4.3 Modifications d'entités existantes

```csharp
// TeamType.cs
public class TeamType
{
    // ...
    public TeamCategory Categorie { get; set; } = TeamCategory.Specialist; // était int
    public string ReglesSpecialesLigue { get; set; } = string.Empty; // CSV : "OldWorldClassic,BadlandsBrawl"
}

// Skill.cs
public class Skill
{
    // ...
    public GameType? GameSpecifique { get; set; } // null = universel ; sinon limité à un jeu
}

// TeamPlayer.cs — le champ `NombreAmeliorations` devient un compteur dérivé de PlayerImprovement.Count.
//                On le SUPPRIME du modèle pour éviter la double source de vérité.
//                Le code qui le lisait passera par `Improvements.Count` (chargement Include obligatoire).

// League.cs — ajouts
public ICollection<PhaseDeReposValidation> ValidationsRepos { get; set; } = [];
public ICollection<LeagueAward> Awards { get; set; } = [];
```

### 4.4 Migrations EF Core

Comme on est en SQLite local dev et qu'on a validé un reset, séquence simple :

1. **Supprimer** `Data/boldesang.db`.
2. **Supprimer toutes les migrations applicatives** ET leur designer.cs (20260512_InitialCreate, 20260514_AddAppConfig, 20260515_AddPlayerPositionRole, 20260516_AddApresMatchValidation, 20260517_FixDungeonBowlRoleLimits).
3. **Supprimer `ApplicationDbContextModelSnapshot.cs`** (sera régénéré par EF).
4. **Après refonte du modèle**, générer une **migration unique consolidée** via `dotnet ef migrations add InitialSchemaV2`.

> **Note** : on conserve les migrations Identity (`00000000000000_CreateIdentitySchema.cs`). On consolide uniquement les migrations applicatives. La nouvelle migration unique remplace toutes les anciennes.

## 5. Modifications logique métier

### 5.1 PSP & Améliorations

```csharp
// MatchService.cs
private static int CalculerPSP(MatchPlayerRecord record, GameType gameType)
{
    int psp = 0;
    psp += record.Touchdowns * (gameType == GameType.DungeonBowl ? 5 : 3);
    psp += record.Completions * 1;
    psp += record.Interceptions * 2;
    psp += record.EliminationsInfligees * 2;
    if (record.EstMVP) psp += 4;
    return psp;
}
```

**Paliers d'amélioration** (constantes) :
```csharp
public static class ImprovementThresholds
{
    public static readonly int[] PspParPalier = [6, 16, 31, 51, 76, 176];
    // Palier 1 = 6 PSP, Palier 2 = 16 PSP cumulés, etc.

    public static int PalierAtteint(int pspCumules) =>
        PspParPalier.Count(seuil => pspCumules >= seuil);
}
```

**Hausse de valeur par type d'amélioration** (LRB S3) :

| Type | Hausse VEA |
|------|-----------|
| AleaPrimaire | 10 000 |
| SelectionPrimaire | 20 000 |
| AleaSecondaire | 20 000 |
| SelectionSecondaire | 40 000 |
| AmeliorationCarac (+1 M ou AG ou CP) | 30 000 |
| AmeliorationForceArmure (+1 F) | 80 000 |
| AmeliorationForceArmure (+1 AR) | 40 000 |

> ⚠️ Ces valeurs sont à recroiser avec `bloodbowl.md §11` ; en cas de doute, vérification dans le PDF source.

**Méthode `TeamService.AppliquerAmeliorationAsync`** (remplace `AttributerCompetenceAsync`) :
- Vérifie que le joueur a atteint un palier non encore consommé.
- Crée l'entité `PlayerImprovement` correspondante.
- Si skill : crée `TeamPlayerSkill` lié.
- Si carac : incrémente le `Mod*` correspondant sur `TeamPlayer`.
- Incrémente `TeamPlayer.ValeurActuelle` du montant approprié.
- Ne décrémente PAS le PSP (les PSP cumulés restent dans `TeamPlayer.PointsStarPlayer`, le palier est calculé via `Improvements.Count`).

### 5.2 Phase de repos

**`LeagueService.LancerPhaseDeReposAsync(int ligueId)`** :
1. Vérifie que tous les matchs `Ronde < 100` (saison régulière) sont `Termine`.
2. Reset : `UPDATE TeamPlayer SET ManqueSuivantMatch = false WHERE TeamId IN (équipes de la ligue)`.
3. Change `League.Statut` → `PhaseDeRepos`.

**`MatchService.ValiderApresMatchReposAsync(int ligueId, int teamId, ...)`** :
- Crée une `PhaseDeReposValidation` au lieu de marquer une feuille de match.
- Permet aux coaches d'attribuer améliorations, recruter joueurs, acheter relances.
- Sans `MatchSheet` : `MatchSheetId = null` sur les `PlayerImprovement` créées.

**`LeagueService.GenererPlayoffsAsync`** : ajoute une **précondition** que la ligue soit en `PhaseDeRepos` (ou `EnCours` pour rétrocompat ?). Demande validation utilisateur si certaines équipes n'ont pas validé leur repos — option "Forcer".

### 5.3 Gains après-match

**Pas de changement de comportement** : les coachs continuent de saisir leurs gains manuellement dans la feuille de match (`MatchSheet.GainsDomicile` / `GainsExterieur`), après avoir lancé les dés eux-mêmes (2D6 pour BB, 2D6 pour DB, etc.).

La méthode existante `MatchService.CalculerGains` est conservée comme **estimation indicative** affichée à côté du champ de saisie — pas appliquée automatiquement. Elle peut être enrichie d'une variante DB ultérieurement si besoin, mais ce n'est pas un objectif de cette refonte.

### 5.4 Classements et awards

Nouvelles méthodes dans `LeagueService` (pures requêtes, pas de migration) :

```csharp
public async Task<List<TeamPlayer>> GetTopJoueursParPspAsync(int ligueId, int limit = 10);
public async Task<List<TeamPlayer>> GetTopMarqueursAsync(int ligueId, int limit = 10);
public async Task<List<TeamPlayer>> GetTopElimineursAsync(int ligueId, int limit = 10);
public async Task<List<TeamPlayer>> GetTopPasseursAsync(int ligueId, int limit = 10);
public async Task<List<(ApplicationUser coach, int pointsLigue, int victoires)>> GetTopCoachsAsync(int ligueId);

// Awards
public async Task AttribuerAwardAsync(int ligueId, AwardType type, int? teamPlayerId, int? teamId, string? coachId);
public async Task<List<LeagueAward>> GetAwardsAsync(int ligueId);
```

UI : la page Détail Ligue affichera un nouvel onglet "Classements" et un panneau "Récompenses" (visible en phase `Termine`).

## 6. Refonte du DbSeeder

### 6.1 Skills (priorité fidélité PDF)

Reseed complet ~115 skills depuis `bloodbowl.md §9` :

- **Agilité** (~16) : Balle Collante, Bondissant, Crampon, Défieur, Délestage, Esquive, Filou, Jongleur, Pas Chassé, Prise Sûre, Protection du Ballon, Réflexes, Saut, Poids Plume, Minus, Tacle Plongeant, Réception Plongeante…
- **Force** (~16) : Bagarreur, Cerveau Lent, Châtaigne, Crâne Épais, Gros Bras, Gros Débile, Lancer de Coéquipier, Massacre, Peau de Fer, Stabilité, Toujours Affamé, Intrépide, Accablant, Prendre Racine, Esquive en Force, Clé de Bras, Marteau-pilon, Bras Musclé, Blocage Multiple, Garde, Projection, Juggernaut…
- **Générale** (~22) : Arracher le Ballon, Blocage, Choc, Chef, Désengagement, Filer, Glissade Contrôlée, Interception, Lutte, Passe en Course, Plaquage, Pro, Poursuite, Réception, Sprint, Tacle, Rétablissement, Défenseur, Frappe et Cours, Nerfs d'Acier, Sur le Ballon, Joueur Déloyal, Parade, Frénésie…
- **Passe** (~12) : Botter de Coéquipier, En Avant, Flèche, Passe, Passe Assurée, Passe dans la Course, Passe Longue Portée, Portée, Précision, Fumblerooskie, Chef (passe)…
- **Mutation** (~17) : Bec Acéré, Bras Tentaculaire, Deux Têtes, Doigts de Poulpe, Griffes, Jambes Fléchies, Langue Visqueuse, Membres Supplémentaires, Tentacules, Queue Préhensile, Cornes, Animosité, Gerbe de Vomi, Régénération, Décomposition, Contagieux, Sauvagerie Animale, Solitaire, Regard Hypnotique, Soif de Sang, Présence Perturbante, Poivrot, Microbe, Furie Débridée, Timmm-ber, Bras Supplémentaire, Très Longues Jambes, Grande Gueule…
- **Scélérate** (~10) : Blocage Sournois, Coup de Poing Vicieux, Croc-en-jambe, Meurtre Prémédité, Poignard, Poignard Sournois, Coup de Pied Sournois, Sournois, Vraiment Sournois, Joueur Déloyal…
- **Spécifiques DungeonBowl** (4, `GameSpecifique = DungeonBowl`) :
  - Navigateur de Portail (Générale)
  - Transmission dans la Course (Passe)
  - Passe par un Portail (Passe)
  - Lancer contre un Mur (Passe)

### 6.2 Rosters Blood Bowl (LRB S3)

Reseed des **30 équipes** depuis `bloodbowl.md §12`, par ordre alphabétique :
Alliance du Vieux Monde, Amazones, Bas-fonds, Bretonniens, Elfes Noirs, Elfes Sylvains, Élus du Chaos, Gnomes, Gobelins, Halflings, Hauts Elfes, Hommes-lézards, Horreurs Nécromantiques, Humains, Khorne, Morts-Ambulants, Nains, Nains du Chaos, Noblesse Impériale, Nordiques, Nurgle, Ogres, Orques, Orques Noirs, Renégats du Chaos, Rois des Tombes, Skavens, Snotlings, Union Elfique, Vampires.

Pour chaque équipe : Catégorie (TeamCategory), CoutRelance, ReglesSpecialesLigue, postes avec stats exactes + compétences de départ + accès Principal/Secondaire.

### 6.3 Rosters Dungeon Bowl (Edition 2022)

Reseed des **8 collèges** depuis `dungeonbowl.md §2` :
Cieux, Feu, Ombre, Lumière, Vie, Métal, Mort, Bêtes.

Pour chaque collège : `RoleNom`/`RoleQuantiteMax` par catégorie (Lineman 0-16, Blitzeur 0-X, Lanceur 0-X, etc.) — conformes au PDF.

Toutes les équipes DB ont `CoutRelance = 50 000` (règle uniforme).

### 6.4 Structure du DbSeeder

Le DbSeeder restera idempotent (`if (!db.Games.Any())`). On externalise les données dans des fichiers C# dédiés pour la lisibilité :

```
Data/Seeding/
├── DbSeeder.cs                  (orchestrateur)
├── SkillSeedData.cs             (~115 skills)
├── BloodBowlTeamSeedData.cs     (30 équipes BB)
└── DungeonBowlTeamSeedData.cs   (8 collèges DB)
```

Chaque fichier expose `public static IEnumerable<Skill> GetSkills()` ou similaire. Le DbSeeder principal coordonne l'insertion.

## 7. Plan de migration (ordre des modifications)

1. **Préparation** : créer `docs/superpowers/specs/...-design.md` (ce document) + commit.
2. **Suppression migrations applicatives** : `Data/Migrations/2026*.cs` (sauf Identity).
3. **Modifications enums + entités** : Enums.cs, TeamType.cs, Skill.cs, League.cs + nouvelles entités.
4. **ApplicationDbContext** : ajouter `DbSet<PlayerImprovement>`, `DbSet<PhaseDeReposValidation>`, `DbSet<LeagueAward>`.
5. **Refonte DbSeeder** : split en fichiers, recoder depuis les markdowns.
6. **Services** : `MatchService.CalculerPSP` (gameType), `TeamService.AppliquerAmeliorationAsync`, `LeagueService.LancerPhaseDeReposAsync`, `LeagueService.GetTop*Async`, `LeagueService.AttribuerAwardAsync`. Gains : pas de modification (saisie libre).
7. **Suppression `Data/boldesang.db`** (reset).
8. **Génération migration consolidée** : `dotnet ef migrations add InitialSchemaV2`.
9. **Build + run** : la DB est recréée et seedée propre au lancement.
10. **Adaptation UI** : les pages existantes (Ligues/Detail, Equipes/Detail, Matchs/Feuille) doivent être adaptées pour :
    - Nouvelles méthodes d'amélioration (sélection type + skill/carac).
    - Phase de repos : nouvel écran "Préparation playoffs".
    - Classements : nouvel onglet sur Détail Ligue.

> L'étape 10 (UI) sera traitée séparément après validation que le modèle DB tient la route. Le présent design garantit que les données sont en place ; les pages Razor peuvent être adaptées incrémentalement.

## 8. Tests à prévoir

Conformément à la directive [[feedback_tests_et_doc]] (mémoire) : tests d'intégration pour chaque évolution :

1. `MatchServiceTests.CalculerPSP_DungeonBowl_TDs_DonnentCinqPSP`
2. `MatchServiceTests.CalculerPSP_BloodBowl_TDs_DonnentTroisPSP`
3. `TeamServiceTests.AppliquerAmelioration_PalierAtteint_CreeImprovement`
4. `TeamServiceTests.AppliquerAmelioration_PalierNonAtteint_LeveException`
5. `LeagueServiceTests.LancerPhaseDeRepos_ResetTousLesRPM`
6. `LeagueServiceTests.LancerPhaseDeRepos_ChangeStatutEnPhaseDeRepos`
7. `LeagueServiceTests.GetTopJoueursParPsp_RetourneJoueursDeLaLigueOrdoneParPSP`
8. `LeagueServiceTests.AttribuerAward_CreeLeagueAward`
9. `DbSeederTests.Seed_CreeTrentEquipesBloodBowl`
10. `DbSeederTests.Seed_CreeHuitCollegesDungeonBowl`
11. `DbSeederTests.Seed_TousLesSkillsLrbSontPresents`

## 9. Ouvertures / à clarifier ultérieurement

- **Validation simultanée race condition** : `AppliquerAmeliorationAsync` doit utiliser une transaction + vérif que le palier n'est pas déjà consommé. Concurrency token sur `TeamPlayer` recommandé (RowVersion).
- **Hausse de valeur exacte par palier** : les valeurs proposées (cf §5.1) sont indicatives, à recroiser avec le PDF source si litige.
- **Catégories d'équipe** : la classification Bashy/Staller/Agile/Specialist est issue de notre lecture du LRB ; certaines équipes peuvent être ambiguës (ex: Hommes-lézards = Bashy + Agile ?). À valider équipe par équipe à l'implémentation.
- **Awards automatiques vs manuels** : pour MVP / récompenses fin de ligue, on laisse pour l'instant le commissaire les attribuer manuellement via UI ; un calcul automatique pourra être ajouté plus tard.

---

## 10. Validation

Design validé en brainstorming avec choix utilisateur :
- ✅ Système PSP : tableau LRB S3 complet
- ✅ Versions : RulesVersion cosmétique (S3 / Edition 2022 uniquement)
- ✅ Phase de repos : pas de Match en base, après-match possible
- ✅ Classements : PSP + scoreurs + coachs + MVP awards
- ✅ Migration : reset complet de la DB
- ✅ Traçabilité phase repos : entité dédiée `PhaseDeReposValidation`
