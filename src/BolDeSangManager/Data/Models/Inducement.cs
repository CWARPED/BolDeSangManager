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
    /// Ligues qui donnent accès à ce star player, en CSV
    /// (« BadlandsBrawl, UnderworldChallenge »).
    ///
    /// Recoupé avec <see cref="TeamType.ReglesSpecialesLigue"/>, le champ
    /// « Règles ligues thématiques » DÉJÀ présent sur la fiche de race et
    /// renseigné pour la plupart d'entre elles : un star player n'apparaît sur
    /// la feuille d'une équipe que si au moins une ligue est commune aux deux.
    ///
    /// VIDE = accessible à TOUTES les équipes (choix produit) : un oubli de
    /// saisie rend le star player visible plutôt qu'introuvable sans raison.
    /// </summary>
    public string Ligues { get; set; } = string.Empty;
    public int Ordre { get; set; }

    /// <summary>Compétences éclatées pour l'affichage.</summary>
    [NotMapped]
    public string[] CompetencesListe => Decouper(Competences);

    /// <summary>Ligues d'accès éclatées. Vide = ouvert à tous.</summary>
    [NotMapped]
    public string[] LiguesListe => Decouper(Ligues);

    private static string[] Decouper(string? csv) =>
        (csv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

    /// <summary>
    /// Ce star player est-il accessible à une équipe inscrite dans ces ligues ?
    /// Comparaison insensible à la casse et aux espaces, la saisie étant
    /// manuelle des deux côtés — une majuscule ne doit pas rendre un joueur
    /// introuvable sans explication.
    /// </summary>
    public bool EstAccessible(IEnumerable<string> liguesDeLEquipe)
    {
        var requises = LiguesListe;
        if (requises.Length == 0) return true;   // ouvert à tous

        return requises.Any(r =>
            liguesDeLEquipe.Any(e => string.Equals(e.Trim(), r, StringComparison.OrdinalIgnoreCase)));
    }
}
