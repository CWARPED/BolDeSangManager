using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class Match
{
    public int Id { get; set; }
    public int? DivisionId { get; set; }
    public Division? Division { get; set; }
    public int Ronde { get; set; }
    public bool EstPlayoff { get; set; } = false;

    public int EquipeDomicileId { get; set; }
    public Team EquipeDomicile { get; set; } = null!;
    public int EquipeExterieurId { get; set; }
    public Team EquipeExterieur { get; set; } = null!;

    public MatchStatus Statut { get; set; } = MatchStatus.Programme;
    public DateTime? DateProgrammee { get; set; }
    public DateTime? DateJouee { get; set; }

    public int? ScoreDomicile { get; set; }
    public int? ScoreExterieur { get; set; }

    public MatchSheet? Feuille { get; set; }
    public ICollection<PlayerInjury> Blessures { get; set; } = [];
}

public class MatchSheet
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public string SaisiParId { get; set; } = string.Empty;
    public ApplicationUser SaisiPar { get; set; } = null!;
    public DateTime SaisiLe { get; set; } = DateTime.UtcNow;

    // Résultats
    public int TouchdownsDomicile { get; set; }
    public int TouchdownsExterieur { get; set; }
    public int EliminationsDomicile { get; set; }
    public int EliminationsExterieur { get; set; }

    // Gains post-match (calculés selon règles)
    public int GainsDomicile { get; set; }
    public int GainsExterieur { get; set; }
    public int VariationFansDomicile { get; set; }
    public int VariationFansExterieur { get; set; }

    // Inducements pré-match (JSON simple: {"entrainement": 2, "potDeVin": 1})
    public string InducementsDomicile { get; set; } = "{}";
    public string InducementsExterieur { get; set; } = "{}";

    public bool ValideParCommissaire { get; set; } = false;
    public string NotesCommissaire { get; set; } = string.Empty;

    public bool ApresMatchDomicileValide { get; set; } = false;
    public bool ApresMatchExterieurValide { get; set; } = false;

    public ICollection<MatchPlayerRecord> RecordsJoueurs { get; set; } = [];
}

public class MatchPlayerRecord
{
    public int Id { get; set; }
    public int MatchSheetId { get; set; }
    public MatchSheet MatchSheet { get; set; } = null!;
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;
    public bool EstCoteDomicile { get; set; }

    // Actions du match
    public int Touchdowns { get; set; } = 0;
    public int Completions { get; set; } = 0;      // Passes réussies
    public int Interceptions { get; set; } = 0;
    public int EliminationsInfligees { get; set; } = 0;
    public bool EstMVP { get; set; } = false;

    // PSP gagnés ce match
    public int PspGagnes { get; set; } = 0;

    // Blessure subie (si applicable)
    public InjuryType? Blessure { get; set; }
    public AffectedStat? StatAffectee { get; set; }
    public bool AManqueLeMatch { get; set; } = false; // Absent avant le match
}
