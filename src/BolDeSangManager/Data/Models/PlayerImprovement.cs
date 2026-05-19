using BolDeSangManager.Data.Enums;

namespace BolDeSangManager.Data.Models;

public class PlayerImprovement
{
    public int Id { get; set; }
    public int TeamPlayerId { get; set; }
    public TeamPlayer TeamPlayer { get; set; } = null!;

    public int Palier { get; set; }              // 1..6 (correspond aux seuils 6/16/31/51/76/176 PSP)
    public ImprovementType Type { get; set; }

    // Skill acquise (si Type = AleaPrimaire/SelectionPrimaire/AleaSecondaire/SelectionSecondaire)
    public int? SkillId { get; set; }
    public Skill? Skill { get; set; }

    // Caractéristique améliorée (si Type = AmeliorationCarac ou AmeliorationForceArmure)
    public AffectedStat? StatAmelioree { get; set; }

    public int ValeurHausse { get; set; }        // kpo ajoutés à TeamPlayer.ValeurActuelle
    public DateTime AppliqueLe { get; set; } = DateTime.UtcNow;
    public bool EnAttenteValidation { get; set; } = false;

    // Traçabilité : null si appliqué pendant la phase de repos
    public int? MatchSheetId { get; set; }
    public MatchSheet? MatchSheet { get; set; }
}
