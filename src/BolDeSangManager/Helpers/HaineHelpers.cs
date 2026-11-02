namespace BolDeSangManager.Helpers;

/// <summary>
/// #28 — Haines d'un joueur (« Prendre sa revanche », LRB S3 p.42). Stockées en
/// TEXTE sur le joueur (<c>TeamPlayer.Haines</c> = « Orque,Nain »), sans lien
/// avec le catalogue de règles : la Haine doit exister en Dungeon Bowl comme en
/// Blood Bowl, que la version contienne ou non des Traits « Haine (…) ».
/// Le catalogue ne sert qu'à SUGGÉRER des cibles et à fournir une description.
/// </summary>
public static class HaineHelpers
{
    public const int LongueurMax = 40;

    /// <summary>
    /// Mots-clés de poste exclus par le livre (« Il ne peut pas s'agir des
    /// Mots-clés suivants… ») — retirés des suggestions, pas interdits à la saisie.
    /// Les variantes au pluriel figurent dans les données réelles.
    /// </summary>
    public static readonly HashSet<string> MotsClesDePoste = new(StringComparer.OrdinalIgnoreCase)
    {
        "Gros Bras", "Blitzer", "Blitzers", "Bloqueur", "Bloqueuse", "Receveur", "Receveuse",
        "Trois-quart", "Coureur", "Coureuse", "Runners", "Spécial", "Lanceur", "Lanceuse"
    };

    public static string Libelle(string cible) => $"Haine ({cible})";

    /// <summary>« Orque,Nain » → ["Orque", "Nain"].</summary>
    public static List<string> Liste(string? haines) =>
        (haines ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static string Joindre(IEnumerable<string> cibles) => string.Join(",", cibles);

    public static bool Contient(string? haines, string cible) =>
        Liste(haines).Any(h => string.Equals(h, cible, StringComparison.OrdinalIgnoreCase));

    public static string Ajouter(string? haines, string cible) => Joindre(Liste(haines).Append(cible));

    /// <summary>Retire UNE occurrence (insensible à la casse).</summary>
    public static string Retirer(string? haines, string cible)
    {
        var liste = Liste(haines);
        var i = liste.FindIndex(h => string.Equals(h, cible, StringComparison.OrdinalIgnoreCase));
        if (i >= 0) liste.RemoveAt(i);
        return Joindre(liste);
    }

    /// <summary>
    /// Nettoie une cible saisie : espaces, « Haine (Orque) » tapé en entier →
    /// « Orque ». Vide → null. Refuse la virgule (séparateur de stockage) et
    /// les textes trop longs.
    /// </summary>
    public static string? Normaliser(string? saisie)
    {
        var t = string.Join(' ', (saisie ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (t.StartsWith("Haine (", StringComparison.OrdinalIgnoreCase) && t.EndsWith(')'))
            t = t[7..^1].Trim();
        if (t.Length == 0) return null;
        if (t.Contains(',') || t.Length > LongueurMax)
            throw new InvalidOperationException(
                $"Haine : un seul mot-clé ({LongueurMax} caractères maximum, sans virgule).");
        return t;
    }
}
