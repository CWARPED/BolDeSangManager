# Page d'édition de données + versioning par RulesVersion

**Date** : 2026-05-19
**Statut** : Design validé, prêt pour plan d'implémentation
**Prérequis** : Spec `2026-05-19-roles-hierarchy-design.md` (rôle GrandCommissaire requis pour accéder à la page)

---

## 1. Objectif

Donner aux Grands Commissaires le pouvoir de maintenir les données de jeu **sans intervention d'un développeur** :

- Corriger / équilibrer les règles existantes (stats, coûts, compétences)
- Ajouter de nouvelles équipes
- Ajouter / éditer / supprimer des compétences
- Créer une nouvelle version de règles (ex : Blood Bowl Saison 4 quand elle sortira)

## 2. Hors-scope

- Édition des Star Players (table non encore implémentée).
- Édition des Coups de Pouce / Inducements (table non encore implémentée).
- Édition des règles du livre (texte explicatif) — uniquement les données structurées.
- Import / export JSON d'une version complète (à ajouter ultérieurement).

## 3. Modèle de données : versioning

### 3.1 Changement de modèle

**Actuellement** :
- `TeamType.GameId` → lie à un Game (BB ou DB) sans distinction de version
- `Skill.GameSpecifique : GameType?` → flag pour les skills DB-only

**Après** :
- `TeamType.RulesVersionId` (FK requis) → chaque TeamType appartient à une version précise
- `Skill.RulesVersionId` (FK requis) → chaque Skill appartient à une version précise
- Suppression de `Skill.GameSpecifique` (l'info passe par la version, qui elle-même est liée à un Game)

### 3.2 Conséquences

- Les skills universels (~116) sont **dupliqués** entre Blood Bowl S3 et Dungeon Bowl Edition 2022. Total après migration : ~232 skills universels + 4 skills DungeonBowl uniquement = **~236 skills**.
- Une ligue (`League.RulesVersionId`) référence une version précise et n'utilise que les TeamTypes/Skills de cette version.
- Quand un GC crée une nouvelle version, il peut :
  - **Cloner** une version existante (recommandé) → tous les TeamTypes, PlayerPositions, PlayerPositionSkills, TeamTypeKeywordLimits, Skills sont dupliqués vers la nouvelle version
  - **Partir vide** → la version est créée sans données ; le GC doit tout ajouter

### 3.3 Migration EF

Migration `AddRulesVersionToTeamTypeAndSkill` :
- `ALTER TABLE TeamTypes ADD COLUMN RulesVersionId INTEGER NOT NULL DEFAULT 0`
- `ALTER TABLE Skills ADD COLUMN RulesVersionId INTEGER NOT NULL DEFAULT 0`
- Migration data : pour chaque TeamType, set `RulesVersionId` au `EstActive=true` de son Game (S3 pour BB, Edition2022 pour DB)
- Pour les skills universels : **dupliquer** chaque skill (une copie par version active) ; le skill original reste sur la version primaire (S3) ; les nouvelles copies sont sur Edition2022 et liées aux PlayerPositionSkills DB.
- Pour les skills DB-only : assigner à Edition2022
- `DROP COLUMN GameSpecifique`
- Add FK `TeamTypes.RulesVersionId → RulesVersions.Id` (Restrict)
- Add FK `Skills.RulesVersionId → RulesVersions.Id` (Restrict)

> ⚠️ La duplication des skills nécessite de re-mapper les `PlayerPositionSkill` et `TeamPlayerSkill` qui pointaient vers le skill original. Migration data détaillée à l'implémentation.

**Stratégie recommandée en dev** (SQLite local, pas de données utilisateur sensibles) : reset complet. Supprimer `boldesang.db`, ajouter la migration EF schema-only, et ré-exécuter le `DbSeeder` qui produira directement la structure correcte. C'est l'approche pragmatique vu qu'on est encore en pré-production.

**Si données prod existent** : migration data scriptée à écrire (à reporter quand le cas se présentera).

## 4. Page d'édition `/admin/donnees`

### 4.1 Accès
- Visible aux rôles : `Admin`, `GrandCommissaire`
- Lien dans le menu principal pour ces rôles

### 4.2 Layout

```
┌─────────────────────────────────────────────────┐
│  Page : Édition des données                     │
│  [Jeu ▼] [Version ▼]    [+ Créer nouvelle vers] │
├─────────────────────────────────────────────────┤
│  Onglets : [Équipes]  [Compétences]  [Versions] │
└─────────────────────────────────────────────────┘
```

### 4.3 Sélecteurs Game + Version

- Game : combobox listant les Games existants (BB, DB, futurs ajouts éventuels).
- Version : combobox listant les `RulesVersion` du Game sélectionné.

### 4.4 Onglet "Équipes"

Table des TeamTypes pour (Game, Version) :

| Nom | Catégorie | Nb postes | Nb limites mot-clé | Actions |
|---|---|---|---|---|
| Humains | Staller | 5 | 0 | [Éditer] [Supprimer] |
| Renégats du Chaos | Staller | 10 | 1 | [Éditer] [Supprimer] |
| ... |

Boutons : **+ Ajouter une équipe**

### 4.5 Onglet "Compétences"

Table des Skills pour (Game, Version) :

| Nom | Catégorie | Trait/Élite | Actions |
|---|---|---|---|
| Esquive | Agilité | Élite | [Éditer] [Supprimer] |
| Blocage | Générale | Élite | [Éditer] [Supprimer] |
| ... |

Filtre : par catégorie (A/F/G/M/P/S).

Boutons : **+ Ajouter une compétence**

### 4.6 Onglet "Versions"

Liste des `RulesVersion` du Game sélectionné :

| Nom | Ordre | Active | Actions |
|---|---|---|---|
| Saison 1 | 1 | non | [Voir données] |
| Saison 2 | 2 | non | [Voir données] |
| Saison 3 | 3 | **oui** | [Voir données] |
| ... |

Bouton : **+ Créer une nouvelle version**

### 4.7 Modale "Créer nouvelle version"

Form :
- **Nom** (text, ex: "Saison 4")
- **Ordre** (number)
- **Active** (checkbox — désactive l'active actuelle si coché)
- **Cloner depuis** (dropdown : versions existantes du même Game, ou "Vide")

Submit :
- Si "Vide" → crée la version avec aucun TeamType/Skill associé
- Si "Cloner depuis X" → crée la version + duplique tous les TeamTypes, PlayerPositions, PlayerPositionSkills, TeamTypeKeywordLimits, Skills de X vers la nouvelle version
- Re-map les FK correctement (skills clonés → PlayerPositionSkills clonés)

## 5. Édition TeamType

Page `/admin/donnees/equipes/{id}` ou modale large.

### 5.1 Champs équipe
- Nom (text, unique par Game+Version)
- Catégorie (enum Bashy/Staller/Agile/Specialist)
- CoutRelance (number)
- ReglesSpeciales (text libre)
- ReglesSpecialesLigue (CSV)

### 5.2 Section "Postes"
Tableau des `PlayerPosition` :

| Nom | Quota | Rôle (qty/max) | Coût | Stats | Skills départ | Actions |
|---|---|---|---|---|---|---|
| Lineman | 16 | — | 50k | 6/3/3+/4+/9+ | Blocage | [Éditer] [Supprimer] |
| Blitzer | 4 | — | 85k | 7/3/3+/4+/9+ | Blocage | [Éditer] [Supprimer] |

Clic sur ligne ou bouton Éditer → **modale d'édition poste**.

Bouton **+ Ajouter un poste** en bas.

### 5.3 Modale "Édition poste"
Tous les attributs :
- Nom
- Quota (QuantiteMax)
- RoleNom (text optionnel)
- RoleQuantiteMax (number, default = Quota)
- Coût
- Mouvement, Force (numbers)
- Agilité, CapacitePasse, Armure (text "3+", "9+", "-")
- CompetencesPrincipales (chips A/F/G/M/P/S, multi-select)
- CompetencesSecondaires (chips, multi-select)
- MotsCles (chips ajoutables)
- Skills de départ (multi-select sur les Skills de la version courante)
- EstGrosBras (checkbox)
- DescriptionRole (text optionnel)

### 5.4 Section "Limites mots-clés"
Liste :
| Mot-clé | Max | Actions |
|---|---|---|
| Gros Bras | 3 | [Supprimer] |

Bouton **+ Ajouter une limite**

### 5.5 Validation
- Nom unique par (Game, Version)
- Au moins 1 poste par TeamType (warning, pas bloquant)
- Stats numériques cohérentes (M 1-9, F 1-8, AR 6-11)
- Skills de départ doivent appartenir à la même Version

### 5.6 Suppression TeamType
Bouton "Supprimer cette équipe" — confirmation modale.

Backend check : `db.Teams.Any(t => t.TeamTypeId == id)` → si oui, refus avec message **"X équipe(s) utilisent ce type. Supprimer les équipes d'abord."** (X = compte).

## 6. Édition Skill

Modale ou page `/admin/donnees/skills/{id}` :
- Nom (text, unique par Game+Version)
- Catégorie (enum SkillCategory)
- Description (textarea)
- EstElite (checkbox)
- EstTrait (checkbox)

### 6.1 Suppression Skill
Backend check :
- `db.TeamPlayerSkills.Any(s => s.SkillId == id)` → refus si oui
- `db.PlayerImprovements.Any(i => i.SkillId == id)` → refus si oui
- `db.PlayerPositionSkills.Any(p => p.SkillId == id)` → refus si oui

Messages détaillés ("3 joueurs ont cette compétence...").

## 7. Service `DataEditService`

```csharp
public class DataEditService(ApplicationDbContext db, ILogger<DataEditService> logger)
{
    // RulesVersion CRUD
    public Task<RulesVersion> CreerVersionAsync(int gameId, string nom, int ordre, bool estActive, int? cloneFromVersionId);
    public Task<List<RulesVersion>> GetVersionsAsync(int gameId);

    // TeamType CRUD
    public Task<TeamType> CreerTeamTypeAsync(int versionId, TeamType data);
    public Task<TeamType> ModifierTeamTypeAsync(int id, TeamType data);
    public Task SupprimerTeamTypeAsync(int id); // throws si Teams existent

    // PlayerPosition CRUD
    public Task<PlayerPosition> AjouterPosteAsync(int teamTypeId, PlayerPosition data);
    public Task<PlayerPosition> ModifierPosteAsync(int id, PlayerPosition data, IEnumerable<int> skillsIds);
    public Task SupprimerPosteAsync(int id); // throws si TeamPlayers existent

    // Skill CRUD
    public Task<Skill> CreerSkillAsync(int versionId, Skill data);
    public Task<Skill> ModifierSkillAsync(int id, Skill data);
    public Task SupprimerSkillAsync(int id); // throws si TeamPlayerSkill/PlayerImprovement/PlayerPositionSkill existent

    // KeywordLimit CRUD
    public Task<TeamTypeKeywordLimit> AjouterLimiteAsync(int teamTypeId, string motCle, int max);
    public Task SupprimerLimiteAsync(int id);
}
```

Auth gate : chaque méthode publique vérifie `IAuthorizationService.PeutEditerDonneesAsync(currentUserId)` avant action (ou laisse cette vérif au layer UI/controller).

## 8. Clonage de version (algorithme)

```
1. Charger source = (RulesVersion + tous ses TeamTypes/Skills + relations)
2. Créer dest = new RulesVersion { Nom, Ordre, GameId, EstActive }
3. Mapper skills : oldSkillId → newSkill
   3a. Pour chaque source.Skill → créer copie avec RulesVersionId = dest.Id ; mémoriser map
4. Mapper teamtypes : oldTeamTypeId → newTeamType
   4a. Pour chaque source.TeamType → créer copie avec RulesVersionId = dest.Id
   4b. Copier les LimitesMotsCles
5. Mapper positions : oldPositionId → newPosition
   5a. Pour chaque PlayerPosition de l'ancien TeamType → créer copie avec TeamTypeId = mapped
   5b. Pour chaque PlayerPositionSkill → créer copie avec PositionId = mapped + SkillId = mapped (via skill map)
6. SaveChanges() (transaction unique pour atomicité)
```

Le clonage doit être **transactionnel**.

## 9. Tests

### 9.1 DataEditService unit/integration tests

- `CreerVersion_AvecClonage_DupliqueToutesLesDonnees`
- `CreerVersion_Vide_AucuneDonneeAssociee`
- `SupprimerTeamType_AvecEquipesExistantes_LeveException`
- `SupprimerPoste_AvecJoueurs_LeveException`
- `SupprimerSkill_UtiliseParJoueur_LeveException`
- `ModifierPoste_NomDeja Pris_LeveException`

### 9.2 Page UI (tests manuels documentés)
- Smoke test : naviguer la page, sélectionner Game+Version, voir liste équipes
- Smoke test : éditer un poste, sauvegarder, vérifier en base
- Smoke test : créer une nouvelle version par clonage de S3 → vérifier données dupliquées

## 10. Plan de migration

1. Migration EF `AddRulesVersionToTeamTypeAndSkill` + data migration pour assigner les RulesVersionId existants.
2. Suppression du champ `Skill.GameSpecifique` + nettoyage code (les usages dans seed / services).
3. `DataEditService` + tests.
4. Page `/admin/donnees` (sélecteurs + onglets).
5. UI éditeur équipe + modale poste.
6. UI éditeur skill.
7. UI éditeur version (création + clonage).
8. UI gestion limites mot-clé.
9. Tests d'intégration + smoke tests UI.

## 11. Ouvertures (à clarifier plus tard)

- Export JSON d'une version pour partage entre instances (le `LeagueExportService` existant ne couvre pas ça).
- Audit log des modifications (qui a édité quoi, quand).
- Validation supplémentaire des stats (cohérence avec les bornes LRB).
- Versionnement des descriptions de skill ("Esquive S3" vs "Esquive S4" pourraient avoir des descriptions différentes).
- Édition des Star Players, Coups de Pouce, Prières à Nuffle — pour quand ces tables seront créées.

## 12. Validation utilisateur

- ✅ Versioning : édition par RulesVersion (chaque version a son propre set de TeamTypes/Skills)
- ✅ Cloner depuis version existante OU partir vide
- ✅ Ajouter de nouvelles équipes possible
- ✅ Suppression : bloquer si dépendances avec message clair
- ✅ Accès : Admin + GrandCommissaire (pas Coach, pas CommissaireDeLigue)
