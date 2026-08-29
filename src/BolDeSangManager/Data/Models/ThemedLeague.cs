using System.ComponentModel.DataAnnotations.Schema;

namespace BolDeSangManager.Data.Models;

/// <summary>
/// Ligue thématique (« Old World Classic », « Badlands Brawl »…) : catalogue
/// éditable rattaché à une version de règles.
///
/// Remplace la saisie en texte libre qui existait sur la fiche de race : une
/// faute de frappe d'un côté rendait un star player introuvable sans la
/// moindre explication. Avec un catalogue, on coche dans une liste — plus
/// rapide à saisir et sans divergence possible entre les deux écrans.
/// </summary>
public class ThemedLeague
{
    public int Id { get; set; }

    public int RulesVersionId { get; set; }
    public RulesVersion RulesVersion { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;

    /// <summary>Ordre d'affichage, comme les autres catalogues.</summary>
    public int Ordre { get; set; }

    public ICollection<TeamTypeThemedLeague> Equipes { get; set; } = [];
    public ICollection<StarPlayerThemedLeague> StarPlayers { get; set; } = [];
}

/// <summary>Rattachement d'une race à une ligue thématique.</summary>
public class TeamTypeThemedLeague
{
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;

    public int ThemedLeagueId { get; set; }
    public ThemedLeague ThemedLeague { get; set; } = null!;
}

/// <summary>Ligue donnant accès à un star player.</summary>
public class StarPlayerThemedLeague
{
    public int StarPlayerId { get; set; }
    public StarPlayer StarPlayer { get; set; } = null!;

    public int ThemedLeagueId { get; set; }
    public ThemedLeague ThemedLeague { get; set; } = null!;
}
