using System.ComponentModel.DataAnnotations.Schema;
using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class TeamType
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public string ReglesSpeciales { get; set; } = string.Empty;
    public int CoutRelance { get; set; } = 50000;

    /// <summary>
    /// OBSOLÈTE — ancien « style de jeu » maison (Bashy/Staller/Agile/Specialist),
    /// qui n'existe pas dans le LRB. La colonne SQL garde son nom d'origine
    /// (<c>Categorie</c>) pour ne casser aucune base existante, mais elle n'est
    /// plus ni écrite, ni affichée, ni clonée, ni exportée.
    /// Ne pas lire dans du code nouveau : voir <see cref="Categorie"/>.
    /// </summary>
    [Column("Categorie")]
    public TeamCategory StyleJeuObsolete { get; set; } = TeamCategory.Specialist;

    /// <summary>
    /// Catégorie officielle du LRB (p.94) : <b>1</b> = équipes les plus
    /// performantes, celles qui pardonnent le mieux les erreurs … <b>4</b> = les
    /// plus faibles (souvent les équipes de « Minus »). <b>0</b> = non renseignée.
    ///
    /// Purement <b>informative</b> : elle oriente le choix d'un nouveau coach mais
    /// ne déclenche aucune mécanique. Dans le LRB elle sert aussi au Jeu Égal
    /// (les catégories basses reçoivent plus de points de compétence), que
    /// l'application ne gère pas.
    ///
    /// Renseignée à la main par les commissaires depuis l'Admin — il n'y a
    /// volontairement ni valeur de seed ni backfill.
    /// </summary>
    [Column("CategorieLrb")]
    public int Categorie { get; set; } = 0;

    // CSV des règles spéciales d'éligibilité aux ligues thématiques.
    // Ex: "OldWorldClassic,BadlandsBrawl". Vide = aucune règle spéciale.
    public string ReglesSpecialesLigue { get; set; } = string.Empty;

    public ICollection<PlayerPosition> Postes { get; set; } = [];
    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<TeamTypeKeywordLimit> LimitesMotsCles { get; set; } = [];

    /// <summary>
    /// Règles spéciales du LRB (p.93-94) rattachées à cette fiche d'équipe.
    /// Remplace le texte libre <see cref="ReglesSpeciales"/>, conservé en base
    /// mais qui n'est plus la source de vérité.
    /// </summary>
    public ICollection<TeamTypeSpecialRule> ReglesSpecialesListe { get; set; } = [];
}

public class PlayerPosition
{
    public int Id { get; set; }
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public int QuantiteMax { get; set; }
    public int Cout { get; set; }
    public int Mouvement { get; set; }
    public int Force { get; set; }
    public string Agilite { get; set; } = "3+";     // "2+", "3+", "4+", "5+", "6+", "-"
    public string CapacitePasse { get; set; } = "-"; // idem
    public string Armure { get; set; } = "9+";       // "6+", "7+", ..., "11+"

    /// <summary>
    /// Accès aux catégories de compétence (principal / secondaire).
    /// Remplace les anciennes chaînes de lettres : voir <see cref="PlayerPositionCategoryAccess"/>.
    /// </summary>
    public ICollection<PlayerPositionCategoryAccess> AccesCategories { get; set; } = [];

    /// <summary>Codes d'accès principaux au format seed ("GAF"). Non persisté — cf. DbSeeder.</summary>
    [NotMapped]
    public string CompetencesPrincipales { get; set; } = "G";

    /// <summary>Codes d'accès secondaires au format seed ("AS"). Non persisté — cf. DbSeeder.</summary>
    [NotMapped]
    public string CompetencesSecondaires { get; set; } = string.Empty;

    // CSV des mots-clés du poste (ex: "Trois-quart,Humain,Squelette,Mort-Vivant").
    // Utilisés par les compétences/traits qui ciblent des keywords (ex: Haine (X)).
    public string MotsCles { get; set; } = string.Empty;

    [NotMapped]
    public string _StartingSkillsTemp { get; set; } = string.Empty;

    public ICollection<PlayerPositionSkill> CompetencesDepart { get; set; } = [];
    public ICollection<TeamPlayer> Joueurs { get; set; } = [];
}

public class PlayerPositionSkill
{
    public int PlayerPositionId { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
