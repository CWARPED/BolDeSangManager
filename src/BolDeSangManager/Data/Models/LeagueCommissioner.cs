namespace BolDeSangManager.Data.Models;

/// <summary>
/// Relation many-to-many : un coach promu commissaire d'une ligue donnée.
/// Plusieurs commissaires possibles par ligue, un coach peut être commissaire de plusieurs ligues.
/// </summary>
public class LeagueCommissioner
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime AssigneLe { get; set; } = DateTime.UtcNow;
    public string? AssignePar { get; set; } // UserId de l'Admin/GC qui a promu
}
