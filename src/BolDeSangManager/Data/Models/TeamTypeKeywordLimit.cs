namespace BolDeSangManager.Data.Models;

/// <summary>
/// Cap équipe sur un mot-clé. Ex : Renégats du Chaos = max 3 joueurs avec mot-clé « Gros Bras ».
/// Si plusieurs limites s'appliquent à un même joueur (plusieurs mots-clés), chacune est testée indépendamment.
/// </summary>
public class TeamTypeKeywordLimit
{
    public int Id { get; set; }
    public int TeamTypeId { get; set; }
    public TeamType TeamType { get; set; } = null!;
    public string MotCle { get; set; } = string.Empty;
    public int Max { get; set; }
}
