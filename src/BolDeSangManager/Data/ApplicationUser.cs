using BolDeSangManager.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace BolDeSangManager.Data;

public class ApplicationUser : IdentityUser
{
    public string PseudoCoach { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Compte anonymisé : les données personnelles ont été effacées mais la ligne
    /// AspNetUsers subsiste, car des équipes, ligues et feuilles de match y font
    /// référence en Restrict. Supprimer la ligne détruirait cet historique.
    /// Un compte anonymisé ne peut plus se connecter.
    /// </summary>
    public bool EstSupprime { get; set; }

    public DateTime? SupprimeLe { get; set; }

    /// <summary>Id de l'admin auteur de la suppression, ou "self" si le coach l'a demandée.</summary>
    public string? SupprimePar { get; set; }

    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<League> LiguesCommissaireees { get; set; } = [];
}
