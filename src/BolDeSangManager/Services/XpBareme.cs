using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Services;

/// <summary>
/// Barème de calcul de l'XP (PSP) gagnée sur un match.
///
/// Isolé ici — plutôt qu'en constantes en dur dans <see cref="MatchService"/> — pour
/// servir de point d'extension : la carte « Barème d'XP configurable à la création
/// d'une ligue/tournoi » n'aura qu'à fournir une autre source de valeurs
/// (par ligue) sans toucher au reste du calcul.
///
/// Les valeurs par défaut sont celles du LRB Saison 3 / Dungeon Bowl Edition 2022.
/// </summary>
public class XpBareme
{
    /// <summary>XP par touchdown. 5 en Dungeon Bowl, 3 sinon.</summary>
    public int ParTouchdown { get; init; } = 3;

    /// <summary>XP par passe complétée.</summary>
    public int ParPasse { get; init; } = 1;

    /// <summary>XP par interception.</summary>
    public int ParInterception { get; init; } = 2;

    /// <summary>XP par élimination infligée.</summary>
    public int ParElimination { get; init; } = 2;

    /// <summary>XP bonus pour le joueur désigné MVP.</summary>
    public int BonusMvp { get; init; } = 4;

    /// <summary>Barème par défaut pour un type de jeu donné.</summary>
    public static XpBareme ParDefaut(GameType gameType) => new()
    {
        ParTouchdown = gameType == GameType.DungeonBowl ? 5 : 3
    };

    /// <summary>
    /// XP proposée pour la performance d'un joueur sur un match.
    /// C'est une <b>valeur par défaut</b> : depuis R4, le coach peut la modifier
    /// sur la feuille de match et c'est sa saisie qui est persistée.
    /// </summary>
    public int Calculer(MatchPlayerRecord record)
    {
        int xp = 0;
        xp += record.Touchdowns * ParTouchdown;
        xp += record.Passes * ParPasse;
        xp += record.Interceptions * ParInterception;
        xp += record.EliminationsInfligees * ParElimination;
        if (record.EstMVP) xp += BonusMvp;
        return xp;
    }

    /// <summary>Variante pour les écrans de saisie, qui manipulent des stats détachées.</summary>
    public int Calculer(int touchdowns, int passes, int interceptions, int eliminations, bool estMvp)
    {
        int xp = 0;
        xp += touchdowns * ParTouchdown;
        xp += passes * ParPasse;
        xp += interceptions * ParInterception;
        xp += eliminations * ParElimination;
        if (estMvp) xp += BonusMvp;
        return xp;
    }
}
