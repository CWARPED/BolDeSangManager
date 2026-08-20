using BolDeSangManager.Data.Enums;
using MudBlazor;

namespace BolDeSangManager.Helpers;

public static class DisplayHelpers
{
    public static Color LeagueColor(LeagueStatus s) => s switch
    {
        LeagueStatus.EnCours     => Color.Success,
        LeagueStatus.PlayOffs    => Color.Warning,
        LeagueStatus.Termine     => Color.Default,
        LeagueStatus.Inscription => Color.Info,
        _                        => Color.Primary
    };

    public static string LeagueLabel(LeagueStatus s) => s switch
    {
        LeagueStatus.Creation    => "En création",
        LeagueStatus.Inscription => "Inscriptions",
        LeagueStatus.EnCours     => "En cours",
        LeagueStatus.PlayOffs    => "Play-offs",
        LeagueStatus.Termine     => "Terminée",
        _                        => s.ToString()
    };

    public static string LeagueFormatLabel(LeagueFormat f) => f switch
    {
        LeagueFormat.RoundRobin             => "Round Robin",
        LeagueFormat.RoundRobinAvecPlayoffs => "RR + Play-offs",
        LeagueFormat.Libre                  => "Libre",
        LeagueFormat.LibreAvecPlayoffs      => "Libre + Play-offs",
        _                                   => f.ToString()
    };

    /// <summary>
    /// Le calendrier est-il composé à la main par le commissaire ?
    /// Centralisé ici pour que le test « est-ce un format libre » ne se
    /// disperse pas en comparaisons d'enum à travers l'application.
    /// </summary>
    public static bool EstFormatLibre(LeagueFormat f) =>
        f is LeagueFormat.Libre or LeagueFormat.LibreAvecPlayoffs;

    /// <summary>Le format prévoit-il une phase de play-offs ?</summary>
    public static bool AvecPlayoffs(LeagueFormat f) =>
        f is LeagueFormat.RoundRobinAvecPlayoffs or LeagueFormat.LibreAvecPlayoffs;

    public static Color MatchColor(MatchStatus s) => s switch
    {
        MatchStatus.Termine               => Color.Success,
        MatchStatus.ValidationCompetences => Color.Info,
        MatchStatus.FeuilleEnSaisie       => Color.Warning,
        _                                 => Color.Default
    };

    public static string MatchLabel(MatchStatus s) => s switch
    {
        MatchStatus.Programme             => "À jouer",
        MatchStatus.AJouer               => "À jouer",
        MatchStatus.FeuilleEnSaisie       => "À confirmer",
        MatchStatus.ValidationCompetences => "Après-match",
        MatchStatus.Termine               => "Terminé",
        MatchStatus.Concede               => "Concédé",
        _                                 => s.ToString()
    };

    public static string MatchBorderStyle(MatchStatus s) => s switch
    {
        MatchStatus.Termine               => "border-left:4px solid #4caf50;",
        MatchStatus.ValidationCompetences => "border-left:4px solid #2196f3;",
        MatchStatus.FeuilleEnSaisie       => "border-left:4px solid #ff9800;",
        _                                 => ""
    };
}
