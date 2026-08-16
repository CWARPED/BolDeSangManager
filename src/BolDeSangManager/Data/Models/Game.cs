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

    public ICollection<PoolPosition> PoolPositions { get; set; } = [];
}
