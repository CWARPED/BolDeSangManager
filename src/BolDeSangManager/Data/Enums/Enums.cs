namespace BolDeSangManager.Data.Enums;

public enum GameType { BloodBowl, DungeonBowl }

public enum LeagueStatus
{
    Creation,        // Le commissaire configure la ligue
    Inscription,     // Les coaches rejoignent et créent leurs équipes
    EnCours,         // Saison régulière en cours
    PhaseDeRepos,    // Entre saison régulière et playoffs : après-match sans rencontre
                     // (et levée des « rate le prochain match » encore en cours)
    PlayOffs,        // Phase de playoffs
    Termine          // Ligue terminée
}

public enum LeagueFormat
{
    // ⚠️ NE JAMAIS RÉORDONNER : ces valeurs sont persistées en int en base.
    // Toute nouvelle entrée s'ajoute À LA FIN.
    RoundRobin,                  // Chacun joue contre tous
    RoundRobinAvecPlayoffs,      // Saison régulière + playoffs
    Libre,                       // Le commissaire compose lui-même les rondes
    LibreAvecPlayoffs,           // Rondes composées à la main + playoffs
    Open                         // Sans fin : ni rondes ni calendrier, inscriptions
                                 // toujours ouvertes, rencontres proposées librement
}

public enum MatchStatus
{
    Programme,                   // Match planifié, pas encore joué
    AJouer,                      // À jouer maintenant
    FeuilleEnSaisie,             // Feuille de match en cours de saisie
    ValidationCompetences,       // Après-match : les deux coaches dépensent l'XP et recrutent
    Termine,                     // Match terminé et validé
    Concede                      // Une équipe a concédé
}

public enum SkillCategory
{
    Agilite,    // A
    Force,      // F
    Generale,   // G
    Mutation,   // M
    Passe,      // P
    Scelerate   // S
}

/// <summary>
/// Résultat d'un jet d'élimination (Tableau d'Élimination, LRB p.42).
/// ⚠️ Persisté en int ET exporté par NOM (JsonStringEnumConverter) : ne jamais
/// renommer ni réordonner ; toute nouvelle valeur s'ajoute à la fin.
/// Libellés affichés : <see cref="BolDeSangManager.Helpers.BlessureHelpers"/>.
/// </summary>
public enum InjuryType
{
    ManqueSuivant,         // « Amoché » : rate le prochain match (RPM)
    BlessurePersistante,   // « Blessure sérieuse » : une BP (+1 aux futurs jets d'élimination) + RPM
    RetraiteTemporaire,    // Hors tableau : retire le joueur de l'effectif (non conforme au LRB, sujet séparé)
    Mort,                  // Joueur mort
    Sequelle               // « Séquelle » : −1 sur une caractéristique (StatAffectee) + RPM — ajoutée par #26
}

public enum AffectedStat
{
    Mouvement,
    Force,
    Agilite,
    CapacitePasse,
    Armure
}

public enum StaffType
{
    CoachAssistant,
    Cheerleader
}

/// <summary>
/// OBSOLÈTE — ancien « style de jeu » maison, absent du livre de règles.
/// Remplacé par <c>TeamType.Categorie</c> (catégorie officielle LRB 1 à 4).
/// Conservé uniquement parce que la colonne existe en base : ne plus lire,
/// ne plus écrire, ne JAMAIS réordonner (EF persiste ces valeurs en int).
/// </summary>
public enum TeamCategory
{
    Bashy,        // Nains, Orques Noirs, Khorne, Chaos Dwarfs, Nurgle…
    Staller,      // Élus du Chaos, Nordiques, Renégats du Chaos, Bretonniens…
    Agile,        // Elfes (tous), Skavens, Amazones, Hommes-lézards…
    Specialist    // Halflings, Snotlings, Ogres, Gobelins, Bas-fonds, Vampires…
}

public enum ImprovementType
{
    AleaPrimaire,            // Tirage D6/D6 sur catégorie primaire
    SelectionPrimaire,       // Choix dans la catégorie primaire
    AleaSecondaire,          // Tirage D6/D6 sur catégorie secondaire
    SelectionSecondaire,     // Choix dans la catégorie secondaire
    AmeliorationCarac,       // +1 M, AG ou CP
    AmeliorationForceArmure  // +1 F ou +1 AR
}

public enum AwardType
{
    Champion,            // Vainqueur de la ligue (rattaché à Team)
    MVP,                 // Meilleur joueur (rattaché à TeamPlayer)
    MeilleurMarqueur,    // Plus de TDs
    MeilleurDefenseur,   // Plus d'éliminations
    MeilleurPasseur,     // Plus de completions+interceptions
    MeilleurCoach        // Plus de victoires / points (rattaché à ApplicationUser)
}
