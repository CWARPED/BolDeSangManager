namespace BolDeSangManager.Services;

public interface IAuthorizationService
{
    /// <summary>Returns true si l'utilisateur a le rôle Identity "Admin".</summary>
    Task<bool> EstAdminAsync(string userId);

    /// <summary>Returns true si l'utilisateur a le rôle Identity "GrandCommissaire".</summary>
    Task<bool> EstGrandCommissaireAsync(string userId);

    /// <summary>Returns true si l'utilisateur est un LeagueCommissioner de la ligue donnée.</summary>
    Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId);

    /// <summary>Returns true si l'utilisateur peut gérer cette ligue (Admin OU GrandCommissaire OU CommissaireDeLigue de cette ligue).</summary>
    Task<bool> PeutGererLigueAsync(string userId, int ligueId);

    /// <summary>Returns true si l'utilisateur peut éditer les données de jeu (Admin OU GrandCommissaire).</summary>
    Task<bool> PeutEditerDonneesAsync(string userId);

    /// <summary>Returns true si l'utilisateur peut modifier les Paramètres système (Admin uniquement).</summary>
    Task<bool> PeutGererSettingsAsync(string userId);
}
