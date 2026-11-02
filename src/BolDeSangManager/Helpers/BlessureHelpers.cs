using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Helpers;

/// <summary>
/// Vocabulaire et règles d'affichage des blessures (Tableau d'Élimination,
/// LRB p.42). Seul endroit où les libellés sont écrits : feuille de match,
/// correction commissaire, détail du match et service s'y réfèrent.
/// </summary>
public static class BlessureHelpers
{
    /// <summary>Types proposés à la saisie, dans l'ordre du tableau.</summary>
    public static readonly InjuryType[] TypesSaisissables =
    [
        InjuryType.ManqueSuivant,
        InjuryType.BlessurePersistante,
        InjuryType.Sequelle,
        InjuryType.RetraiteTemporaire,
        InjuryType.Mort,
    ];

    public static string Libelle(InjuryType type) => type switch
    {
        InjuryType.ManqueSuivant       => "Amoché",
        InjuryType.BlessurePersistante => "Blessure sérieuse",
        InjuryType.Sequelle            => "Séquelle",
        InjuryType.RetraiteTemporaire  => "Retraite temporaire",
        InjuryType.Mort                => "Mort",
        _                              => type.ToString()
    };

    /// <summary>Effet rappelé à côté du libellé dans le menu de saisie.</summary>
    public static string Effet(InjuryType type) => type switch
    {
        InjuryType.ManqueSuivant       => "RPM",
        InjuryType.BlessurePersistante => "BP + RPM",
        InjuryType.Sequelle            => "−1 carac + RPM",
        _                              => ""
    };

    public static string LibelleMenu(InjuryType type) =>
        Effet(type) is { Length: > 0 } e ? $"{Libelle(type)} ({e})" : Libelle(type);

    public static string AbregeStat(AffectedStat stat) => stat switch
    {
        AffectedStat.Mouvement     => "M",
        AffectedStat.Force         => "F",
        AffectedStat.Agilite       => "AG",
        AffectedStat.CapacitePasse => "CP",
        AffectedStat.Armure        => "AR",
        _                          => stat.ToString()
    };

    public static string LibelleStat(AffectedStat stat) => stat switch
    {
        AffectedStat.Mouvement     => "Mouvement",
        AffectedStat.Force         => "Force",
        AffectedStat.Agilite       => "Agilité",
        AffectedStat.CapacitePasse => "Capacité de passe",
        AffectedStat.Armure        => "Armure",
        _                          => stat.ToString()
    };

    /// <summary>Résumé court d'une blessure subie (détail du match).</summary>
    public static string Resume(InjuryType type, AffectedStat? stat) => type switch
    {
        InjuryType.Sequelle            => $"Séquelle : −1 {(stat is { } s ? AbregeStat(s) : "?")}",
        InjuryType.BlessurePersistante => "Blessure sérieuse : BP",
        _                              => Libelle(type)
    };

    /// <summary>Nombre de Blessures Persistantes (chacune : +1 au jet d'élimination).</summary>
    public static int NombreBP(TeamPlayer j) =>
        j.Blessures.Count(b => b.Type == InjuryType.BlessurePersistante);

    public static bool ASequelle(TeamPlayer j) =>
        j.Blessures.Any(b => b.Type == InjuryType.Sequelle);

    /// <summary>« BP » ou « BP×2 » ; vide sans BP.</summary>
    public static string MarqueBP(int nombre) => nombre switch
    {
        <= 0 => "",
        1    => "BP",
        _    => $"BP×{nombre}"
    };

    /// <summary>
    /// Relit une blessure issue d'un export. Avant #26, une « BlessurePersistante »
    /// portant une caractéristique était en réalité une Séquelle (c'est le seul
    /// effet qu'elle avait). Hors Séquelle, aucune caractéristique n'a de sens.
    /// </summary>
    public static (InjuryType type, AffectedStat? stat) NormaliserImport(InjuryType type, AffectedStat? stat) =>
        type switch
        {
            InjuryType.BlessurePersistante when stat.HasValue => (InjuryType.Sequelle, stat),
            InjuryType.Sequelle                               => (type, stat),
            _                                                 => (type, null)
        };
}
