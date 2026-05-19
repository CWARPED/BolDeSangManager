using BolDeSangManager.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace BolDeSangManager.Data;

public class ApplicationUser : IdentityUser
{
    public string PseudoCoach { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<League> LiguesCommissaireees { get; set; } = [];
}
