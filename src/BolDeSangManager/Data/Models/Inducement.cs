using System.ComponentModel.DataAnnotations.Schema;

namespace BolDeSangManager.Data.Models;

/// <summary>
/// « Coup de pouce » (Inducement) : option payante achetée avant un match pour
/// compenser un écart de valeur d'équipe.
///
/// Purement INFORMATIF, comme la catégorie d'équipe : l'application les affiche
/// sur la feuille pour que les coaches comparent les VEA et choisissent à la
/// table. Aucune mécanique n'est déclenchée, rien n'est débité.
///
/// Rattaché à une version de règles (choix produit) : une nouvelle édition
/// s'ajoute par clonage de version, sans toucher aux ligues en cours.
/// </summary>
public class Inducement
{
    public int Id { get; set; }

    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;

    /// <summary>Effet du coup de pouce, rappelé au coach sur sa feuille.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Coût en pièces d'or. 0 = variable ou non chiffré.</summary>
    public int Cout { get; set; }

    /// <summary>
    /// Nombre maximal d'exemplaires achetables par match (le « 3 » de « 0-3 »).
    /// 0 = non précisé.
    /// </summary>
    public int QuantiteMax { get; set; }

    /// <summary>
    /// Équipes concernées quand le coup de pouce est restreint
    /// (« Maîtres de la Non-vie seulement »). Vide = accessible à toutes.
    /// Informatif : aucune vérification n'est faite.
    /// </summary>
    public string Restriction { get; set; } = string.Empty;

    /// <summary>Ordre d'affichage, comme pour les règles spéciales.</summary>
    public int Ordre { get; set; }
}

/// <summary>
/// Star player : mercenaire que l'on peut louer pour un match via les coups de
/// pouce. Il ne rejoint JAMAIS un effectif — d'où l'absence de tout lien avec
/// <see cref="Team"/> ou <see cref="TeamPlayer"/>.
///
/// Purement informatif : la fiche sert au coach à comparer sa VEA à celle de
/// l'adversaire et à décider à la table. Nom, stats, compétences, coût et
/// conditions d'accès, rien de plus.
/// </summary>
public class StarPlayer
{
    public int Id { get; set; }

    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;
    public int Cout { get; set; }

    // Stats, au même format que PlayerPosition pour un affichage homogène.
    public int Mouvement { get; set; }
    public int Force { get; set; }
    public string Agilite { get; set; } = "3+";
    public string CapacitePasse { get; set; } = "-";
    public string Armure { get; set; } = "9+";

    /// <summary>
    /// Compétences en TEXTE LIBRE, séparées par des virgules.
    ///
    /// Volontairement non rattachées au catalogue de compétences : un star
    /// player porte souvent des capacités qui lui sont propres, et comme la
    /// fiche est informative, aucun calcul ne dépend de ce lien. Exiger une
    /// correspondance bloquerait la saisie sans rien apporter.
    /// </summary>
    public string Competences { get; set; } = string.Empty;

    /// <summary>
    /// Règles spéciales propres au star player (« Loner (4+) », « Jeu de
    /// Bagarre », « Ne peut pas être engagé par… »), en TEXTE LIBRE.
    ///
    /// Distinct des compétences : ce sont des clauses de mercenaire, souvent
    /// rédigées en une phrase. Aucun lien avec le catalogue de règles
    /// spéciales d'équipe, qui porte lui des comportements automatiques —
    /// ici tout est informatif.
    /// </summary>
    public string ReglesSpeciales { get; set; } = string.Empty;

    /// <summary>
    /// Ligues qui donnent accès à ce star player. Rattachement par CATALOGUE
    /// (voir <see cref="ThemedLeague"/>) et non par texte libre : une faute de
    /// frappe rendait auparavant un star player introuvable sans explication.
    ///
    /// AUCUNE ligue rattachée = accessible à TOUTES les équipes (choix
    /// produit) : un oubli de saisie le rend visible plutôt qu'invisible.
    /// </summary>
    public ICollection<StarPlayerThemedLeague> Ligues { get; set; } = [];

    public int Ordre { get; set; }

    /// <summary>Compétences éclatées pour l'affichage.</summary>
    [NotMapped]
    public string[] CompetencesListe => Decouper(Competences);

    private static string[] Decouper(string? csv) =>
        (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

    /// <summary>
    /// Ce star player est-il accessible à une équipe inscrite dans ces ligues ?
    /// Comparaison sur les identifiants du catalogue : plus de divergence
    /// possible entre deux saisies manuelles.
    /// </summary>
    public bool EstAccessible(IEnumerable<int> liguesDeLEquipe)
    {
        if (Ligues.Count == 0) return true;   // ouvert à tous

        var requises = Ligues.Select(l => l.ThemedLeagueId).ToHashSet();
        return liguesDeLEquipe.Any(requises.Contains);
    }
}
