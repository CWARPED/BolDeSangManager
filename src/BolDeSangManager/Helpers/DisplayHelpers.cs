using BolDeSangManager.Data.Enums;
using MudBlazor;

namespace BolDeSangManager.Helpers;

public static class DisplayHelpers
{
    /// <summary>
    /// Libellé du meilleur joueur d'un match : « JPV » (Joueur le Plus Valeureux),
    /// terme français employé par l'association plutôt que l'anglais « MVP ».
    ///
    /// Centralisé ici — et non recopié dans chaque vue — pour rester le seul
    /// point à modifier, ce qui prépare l'internationalisation de l'interface.
    /// Le code, lui, conserve les identifiants d'origine (`EstMVP`,
    /// `AwardType.MVP`, `BonusMvp`) : ce sont des noms techniques, pas de
    /// l'affichage, et les renommer toucherait la base et les tests sans gain.
    /// </summary>
    public const string LabelMvp = "JPV";

    /// <summary>Forme longue, pour les infobulles et les textes d'aide.</summary>
    public const string LabelMvpLong = "JPV (Joueur le Plus Valeureux)";

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
        // Manquait au switch : l'état était inatteignable faute d'écran pour le
        // déclencher, et s'affichait donc « PhaseDeRepos » brut.
        LeagueStatus.PhaseDeRepos => "Phase de repos",
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
        LeagueFormat.Open                   => "Open (sans fin)",
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

    /// <summary>
    /// Le format se passe-t-il totalement de calendrier ?
    /// En Open il n'y a ni ronde ni pool de matchs : les rencontres sont créées
    /// à la volée. À ne pas confondre avec le format Libre, qui a bien des
    /// rondes — simplement composées à la main.
    /// </summary>
    public static bool SansCalendrier(LeagueFormat f) => f is LeagueFormat.Open;

    /// <summary>
    /// Peut-on encore inscrire une équipe ?
    ///
    /// Le format Open est « sans fin » : la ligue est simultanément en cours et
    /// ouverte aux inscriptions, un état que <see cref="LeagueStatus"/> ne sait
    /// pas exprimer. Plutôt qu'un statut supplémentaire à traiter dans chaque
    /// switch, la règle est portée ici — comme <see cref="EstFormatLibre"/>.
    /// Une ligue Open clôturée (Termine) n'accepte évidemment plus personne.
    /// </summary>
    public static bool InscriptionOuverte(LeagueStatus statut, LeagueFormat format) =>
        statut == LeagueStatus.Inscription
        || (format == LeagueFormat.Open && statut is LeagueStatus.Creation or LeagueStatus.EnCours);

    /// <summary>
    /// L'écran calendrier est-il accessible ?
    ///
    /// Avant le lancement (Creation / Inscription) le commissaire prépare les
    /// DATES de ronde, quel que soit le format : dater son planning à l'avance a
    /// du sens aussi en Round Robin, où le calendrier sera généré ensuite.
    /// Une fois la saison lancée, seul le format Libre garde un intérêt : c'est
    /// là qu'on compose les rencontres à la main.
    /// Jamais en Open : ce format n'a aucune ronde à dater.
    /// </summary>
    public static bool CalendrierEditable(LeagueStatus statut, LeagueFormat format) =>
        !SansCalendrier(format)
        && (statut < LeagueStatus.EnCours || (statut == LeagueStatus.EnCours && EstFormatLibre(format)));

    /// <summary>
    /// Les rencontres d'une ronde sont-elles composées à la main sur cet écran ?
    /// Réservé au format Libre et une fois la saison lancée : avant, les équipes
    /// ne sont pas toutes inscrites, il n'y a personne à apparier.
    /// </summary>
    public static bool AppariementsEditables(LeagueStatus statut, LeagueFormat format) =>
        statut == LeagueStatus.EnCours && EstFormatLibre(format);

    /// <summary>
    /// Libellé d'une ronde. Centralisé ici plutôt que répété dans chaque
    /// composant : trois conventions cohabitent sur la colonne Ronde.
    /// 0 = hors ronde (format Open, qui n'a pas de calendrier),
    /// >= 100 = tour de play-off, le reste = numéro de ronde classique.
    /// </summary>
    public static string RondeLabel(int ronde) => ronde switch
    {
        0                 => "Rencontre libre",
        >= 100            => $"Play-off — Tour {ronde - 99}",
        _                 => $"Ronde {ronde}"
    };

    /// <summary>Variante courte du libellé de ronde (bandeaux, listes denses).</summary>
    public static string RondeLabelCourt(int ronde) => ronde switch
    {
        0                 => "Libre",
        >= 100            => $"Play-off T{ronde - 99}",
        _                 => $"Ronde {ronde}"
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
