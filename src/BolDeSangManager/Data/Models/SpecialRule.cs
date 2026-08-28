namespace BolDeSangManager.Data.Models;

/// <summary>
/// Règle spéciale d'équipe du livre de règles (LRB p.93-94) — « Bagarreurs
/// Brutaux », « Favori de… », « Capitaine », « Trois-quarts à Vil Prix »…
///
/// Portée par une <see cref="RulesVersion"/>, comme les compétences et le
/// staff : chaque édition a son jeu de règles, et cloner une version les
/// emporte. Liste OUVERTE — l'association crée une règle inédite depuis
/// l'Admin, sans dev.
///
/// Le rattachement aux fiches d'équipe passe par
/// <see cref="TeamTypeSpecialRule"/>.
/// </summary>
public class SpecialRule
{
    public int Id { get; set; }
    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    /// <summary>Nom affiché, tel qu'il figure dans le livre (« Capitaine »).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>Effet de la règle, rappelé au coach sur sa feuille d'équipe.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Ordre d'affichage sur la feuille d'équipe et dans l'admin.</summary>
    public int Ordre { get; set; }

    /// <summary>
    /// Code machine optionnel. <b>Vide = règle purement descriptive</b> : elle
    /// s'affiche, et c'est tout — c'est le cas par défaut, et celui de toute
    /// règle qu'une future édition amènera. Un code non vide branche un
    /// comportement écrit une fois dans le code.
    ///
    /// Codes reconnus à ce jour : voir <see cref="SpecialRuleCodes"/>.
    /// Un code inconnu n'est pas une erreur : la règle reste descriptive.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public ICollection<TeamTypeSpecialRule> TeamTypes { get; set; } = [];
}

/// <summary>
/// Codes machine reconnus par l'application. Tout le reste est descriptif.
/// </summary>
public static class SpecialRuleCodes
{
    /// <summary>
    /// « Favori de… » (LRB p.93) : l'équipe voue un culte à un Dieu du Chaos.
    /// Les divinités permises à chaque race sont listées dans
    /// <see cref="TeamTypeSpecialRule.OptionsChoix"/> ; le commissaire choisit
    /// celle de l'équipe (<c>Team.DiviniteChoisie</c>).
    /// </summary>
    public const string FavoriDe = "FavoriDe";

    /// <summary>
    /// « Trois-quarts à Vil Prix » (LRB p.93) : dans la VEA, le coût d'embauche
    /// des joueurs visés compte pour 0 po.
    ///
    /// Le MOT-CLÉ des postes concernés est lu dans
    /// <see cref="TeamTypeSpecialRule.OptionsChoix"/> (typiquement
    /// « Trois-quart ») — pas codé en dur, pour qu'une future édition visant
    /// un autre mot-clé se règle en admin, sans développement.
    /// </summary>
    public const string CoutNulParMotCle = "CoutNulParMotCle";

    /// <summary>
    /// « Maîtres de la Non-Vie » (LRB p.94) : recrutement gratuit d'un joueur
    /// en après-match. Même principe : le mot-clé des postes éligibles vient
    /// de <see cref="TeamTypeSpecialRule.OptionsChoix"/>.
    /// </summary>
    public const string RecrutementGratuitParMotCle = "RecrutementGratuitParMotCle";

    /// <summary>
    /// Découpe un CSV d'options en valeurs propres (vides et espaces retirés).
    /// Utilisé pour les divinités comme pour les mots-clés.
    /// </summary>
    public static string[] DecouperOptions(string? options) =>
        (options ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>
/// Rattachement d'une <see cref="SpecialRule"/> à une fiche d'équipe.
/// Clé composite {TeamTypeId, SpecialRuleId}.
/// </summary>
public class TeamTypeSpecialRule
{
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;

    public int SpecialRuleId { get; set; }
    public SpecialRule SpecialRule { get; set; } = null!;

    /// <summary>
    /// Options offertes à CETTE race quand la règle demande un choix, en CSV.
    ///
    /// Exemples pour « Favori de… » :
    /// <list type="bullet">
    /// <item>« Nurgle » — une seule option : la divinité est imposée (Pestiférés).</item>
    /// <item>« Hashut,Khorne,Nurgle,Slaanesh,Tzeentch,Chaos Universel » — le
    /// commissaire choisit (Renégats du Chaos).</item>
    /// <item>vide — la règle ne demande aucun choix.</item>
    /// </list>
    /// </summary>
    public string OptionsChoix { get; set; } = string.Empty;
}
