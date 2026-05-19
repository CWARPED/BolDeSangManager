namespace BolDeSangManager.Data.Models;

public class PhaseDeReposValidation
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTime ValideLe { get; set; } = DateTime.UtcNow;
}
