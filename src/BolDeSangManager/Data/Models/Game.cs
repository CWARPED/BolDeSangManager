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

    public ICollection<PoolPosition> PoolPositions { get; set; } = [];
}
