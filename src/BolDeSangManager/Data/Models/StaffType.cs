namespace BolDeSangManager.Data.Models;

/// <summary>
/// Définition d'un élément de staff (fans dévoués, relances, coachs assistants,
/// cheerleaders, apothicaire… et tout ce que l'association ajoutera elle-même),
/// portée par une <see cref="RulesVersion"/> — comme la Réserve et les
/// catégories de compétence.
///
/// C'est ici que vivent les valeurs livrées AVEC les règles. Une ligue en prend
/// une copie à sa création (<see cref="LeagueStaffType"/>) que le commissaire
/// peut ajuster pour son format, exactement comme le barème d'XP : baisser un
/// prix dans les règles ne doit pas rétro-modifier la VEA d'une ligue en cours.
///
/// Liste OUVERTE : l'association crée un staff inédit depuis l'Admin, sans dev.
/// </summary>
public class StaffDefinition
{
    public int Id { get; set; }
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;

    /// <summary>
    /// Effet de jeu rappelé au coach au moment de l'achat
    /// (ex. « permet de relancer un jet de blessure une fois par match »).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ordre d'affichage dans les écrans d'achat.</summary>
    public int Ordre { get; set; }

    /// <summary>Un staff désactivé n'est plus proposé, sans perdre sa définition.</summary>
    public bool EstActif { get; set; } = true;

    /// <summary>Prix unitaire en po. Ignoré si <see cref="CoutDepuisTypeEquipe"/>.</summary>
    public int Cout { get; set; }

    /// <summary>
    /// Le prix vient de la race/collège (<see cref="TeamType.CoutRelance"/>) et
    /// non du champ <see cref="Cout"/> : c'est le cas des relances, qui coûtent
    /// plus cher aux Nains qu'aux Elfes. Généralise la règle à tout staff que
    /// l'association voudrait tarifer par race.
    /// </summary>
    public bool CoutDepuisTypeEquipe { get; set; }

    /// <summary>Quantité minimale imposée à la création d'une équipe.</summary>
    public int MinCreation { get; set; }

    /// <summary>Quantité maximale autorisée à la création d'une équipe.</summary>
    public int MaxCreation { get; set; }

    /// <summary>
    /// Plafond en cours de ligue. <c>null</c> = aucun plafond.
    /// Plafond DUR : il s'applique aux achats comme aux gains obtenus par les
    /// résultats de match (variation de fans après une rencontre).
    /// </summary>
    public int? MaxLigue { get; set; }

    /// <summary>
    /// Ce staff entre-t-il dans la Valeur d'Équipe Actuelle (VEA) ?
    ///
    /// Faux pour les <b>Fans dévoués</b> : ils représentent le public, pas la
    /// puissance de l'équipe sur le terrain, et les compter gonflerait la VEA
    /// — donc fausserait les coups de pouce accordés à l'équipe la plus faible.
    /// Ils restent PAYANTS : ce drapeau ne concerne que la VEA, jamais le budget.
    ///
    /// Réglable en Admin plutôt qu'écrit en dur, pour qu'une édition future ou
    /// un staff inventé par l'association se règle sans dev (principe #2).
    /// </summary>
    public bool CompteDansVea { get; set; } = true;
}

/// <summary>
/// Copie d'un <see cref="StaffType"/> prise à la création d'une ligue. Le
/// commissaire ajuste prix et bornes pour SA ligue sans toucher aux règles.
/// C'est cette copie que référencent les équipes.
/// </summary>
public class LeagueStaffType
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;

    /// <summary>
    /// Origine dans les règles. Nullable : un staff ajouté directement dans une
    /// ligue, ou dont la définition d'origine a été supprimée, reste valide.
    /// </summary>
    public int? StaffTypeId { get; set; }
    public StaffDefinition? StaffDefinition { get; set; }

    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public bool EstActif { get; set; } = true;
    public int Cout { get; set; }
    public bool CoutDepuisTypeEquipe { get; set; }
    public int MinCreation { get; set; }
    public int MaxCreation { get; set; }
    public int? MaxLigue { get; set; }

    /// <summary>
    /// Copie du drapeau des règles (<see cref="StaffDefinition.CompteDansVea"/>),
    /// ajustable par le commissaire pour SA ligue.
    /// </summary>
    public bool CompteDansVea { get; set; } = true;

    public ICollection<TeamStaff> Achats { get; set; } = [];
}

/// <summary>
/// Quantité d'un staff détenue par une équipe. Remplace les colonnes dédiées
/// de <see cref="Team"/> (FansDevoues, NombreCheerleaders…), qui restent en
/// base — la migration est purement additive — mais ne sont plus lues.
/// </summary>
public class TeamStaff
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int LeagueStaffTypeId { get; set; }
    public LeagueStaffType LeagueStaffType { get; set; } = null!;

    public int Quantite { get; set; }
}
