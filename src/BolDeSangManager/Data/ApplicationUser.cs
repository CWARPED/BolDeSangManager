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

    /// <summary>
    /// Secret de l'URL d'abonnement iCalendar (« S'abonner à mes matchs »).
    ///
    /// Nullable et généré À LA DEMANDE : un compte qui n'a jamais demandé
    /// d'abonnement n'expose aucune adresse. Le jeton tient lieu de mot de passe
    /// (le flux est servi sans authentification, un agenda tiers n'ayant pas de
    /// cookie) — il est donc tiré de RandomNumberGenerator, jamais d'un Guid.
    /// Le régénérer invalide immédiatement l'ancien lien.
    /// </summary>
    public string? JetonCalendrier { get; set; }

    public ICollection<Team> Equipes { get; set; } = [];
    public ICollection<League> LiguesCommissaireees { get; set; } = [];
}
