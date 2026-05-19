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
        _                                   => f.ToString()
    };

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
