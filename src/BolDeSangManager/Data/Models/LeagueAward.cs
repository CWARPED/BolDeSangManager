using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class LeagueAward
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public AwardType Type { get; set; }

    // Au moins une de ces FK est non-null selon le type d'award
    public int? TeamPlayerId { get; set; }
    public TeamPlayer? TeamPlayer { get; set; }
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
    public string? CoachId { get; set; }
    public ApplicationUser? Coach { get; set; }

    public DateTime AttribueLe { get; set; } = DateTime.UtcNow;
}
