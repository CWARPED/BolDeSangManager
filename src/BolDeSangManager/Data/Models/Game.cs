using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class Game
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public GameType Type { get; set; }

    public ICollection<RulesVersion> Versions { get; set; } = [];
    public ICollection<TeamType> TypesEquipes { get; set; } = [];
    public ICollection<League> Ligues { get; set; } = [];
}

public class RulesVersion
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public string Nom { get; set; } = string.Empty;
    public bool EstActive { get; set; } = true;
    public int Ordre { get; set; }

    /// <summary>
    /// Numéro de révision des données de cette version de règles (F3).
    /// Incrémenté à chaque export : c'est lui qui permet de dire quelle
    /// correction de l'asso se trouve dans quelle livraison, et d'avertir
    /// quand on réimporte un fichier plus ancien que la base.
    /// </summary>
    public int Revision { get; set; } = 0;

    /// <summary>Date du dernier export de cette version (F3), en UTC.</summary>
    public DateTime? DernierExportLe { get; set; }

    // ── Barème d'XP de référence (R6) ─────────────────────────────────────────
    // Le barème appartient aux RÈGLES : c'est ici qu'on définit ce que vaut un
    // touchdown dans cette version. Une ligue reprend ces valeurs à sa création
    // et peut ensuite les ajuster pour son propre format.
    // Valeurs par défaut = LRB Saison 3 (le touchdown vaut 5 en Dungeon Bowl,
    // ajusté au seed).

    /// <summary>XP par touchdown.</summary>
    public int XpParTouchdown { get; set; } = 3;

    /// <summary>XP par passe complétée.</summary>
    public int XpParPasse { get; set; } = 1;

    /// <summary>XP par interception.</summary>
    public int XpParInterception { get; set; } = 2;

    /// <summary>XP par élimination infligée.</summary>
    public int XpParElimination { get; set; } = 2;

    /// <summary>XP bonus pour le joueur désigné MVP.</summary>
    public int XpBonusMvp { get; set; } = 4;

    /// <summary>XP par déviation (DEV). Zéro par défaut : l'action est comptée
    /// pour le classement, pas pour la progression du joueur.</summary>
    public int XpParDeviation { get; set; } = 0;

    /// <summary>XP par agression (AGRO). Zéro par défaut, comme la déviation.</summary>
    public int XpParAgression { get; set; } = 0;

    // ── Barème de points de classement de référence ───────────────────────────
    // Ce que vaut un match au CLASSEMENT dans cette version de règles. Une ligue
    // reprend ces valeurs à sa création et peut ensuite les ajuster — y compris
    // saison lancée, avec recalcul complet. Les PALIERS (points variables selon
    // le nombre de tours joués) n'existent qu'au niveau ligue : c'est un choix de
    // format, pas une règle d'édition.
    // Valeurs par défaut = 3 / 1 / 0 sans bonus, soit exactement le calcul en dur
    // qui existait avant.

    /// <summary>Points de classement pour une victoire.</summary>
    public int PointsVictoire { get; set; } = 3;

    /// <summary>Points de classement pour un match nul.</summary>
    public int PointsNul { get; set; } = 1;

    /// <summary>Points de classement pour une défaite.</summary>
    public int PointsDefaite { get; set; } = 0;

    /// <summary>Points de classement bonus par touchdown marqué.</summary>
    public int PointsParTouchdown { get; set; } = 0;

    /// <summary>Points de classement bonus par élimination infligée.</summary>
    public int PointsParElimination { get; set; } = 0;

    /// <summary>Points de classement bonus par interception.</summary>
    public int PointsParInterception { get; set; } = 0;

    /// <summary>Points de classement bonus par passe réussie.</summary>
    public int PointsParPasse { get; set; } = 0;

    /// <summary>Points de classement bonus par déviation.</summary>
    public int PointsParDeviation { get; set; } = 0;

    /// <summary>Points de classement bonus par agression.</summary>
    public int PointsParAgression { get; set; } = 0;

    public ICollection<PoolPosition> PoolPositions { get; set; } = [];

    /// <summary>Définitions de staff livrées avec cette version de règles.</summary>
    public ICollection<StaffDefinition> StaffTypes { get; set; } = [];
}
