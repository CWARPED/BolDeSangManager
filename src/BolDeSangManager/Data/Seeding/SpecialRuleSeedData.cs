using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Règles spéciales d'équipe du livre de règles (LRB p.93-94), et leur
/// rattachement aux fiches d'équipe de Blood Bowl.
///
/// Les textes sont ceux du livre. Le rattachement reprend ce qui figurait déjà
/// dans le champ texte libre <c>TeamType.ReglesSpeciales</c> du seed
/// (« Favoris de Khorne, Bagarreurs Brutaux »…), désormais structuré.
///
/// ⚠️ Seule « Favori de… » porte un <c>Code</c> à ce stade : c'est la seule qui
/// déclenche un comportement (le choix d'une divinité). Toutes les autres sont
/// DESCRIPTIVES — elles s'affichent sur la feuille d'équipe et se jouent à la
/// table. Les brancher (Trois-quarts à Vil Prix sur la VEA, Bagarreurs Brutaux
/// sur les PSP, Capitaine…) est un chantier distinct.
/// </summary>
public static class SpecialRuleSeedData
{
    /// <summary>Le catalogue lui-même. Ordre = ordre d'affichage.</summary>
    public static IEnumerable<SpecialRule> GetRegles(int versionId) =>
    [
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 1,
            Nom = "Bagarreurs Brutaux",
            Description = "En Jeu en Ligue, les joueurs de cette équipe gagnent 3 PSP au lieu de 2 pour avoir infligé une Élimination, et seulement 2 PSP au lieu de 3 pour avoir marqué un Touchdown."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 2,
            Nom = "Chantage et Corruption",
            Description = "Une fois par match, quand l'équipe obtient un 1 pour Contester la Décision, elle peut relancer le D6."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 3,
            Nom = "Favori de…",
            Code = SpecialRuleCodes.FavoriDe,
            Description = "L'équipe rend hommage à un Dieu du Chaos. Certaines équipes ont un alignement automatique, d'autres ont le choix. Le choix est définitif. Certains Star Players et Coups de Pouce exigent d'être Favori d'un dieu précis."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 4,
            Nom = "Trois-quarts à Vil Prix",
            Description = "En Jeu en Ligue, quand l'équipe calcule sa Valeur d'Équipe Actuelle, les Coûts d'Embauche de ses joueurs Trois-quarts comptent pour 0 pièce d'or. Toute augmentation de valeur de ces joueurs est incluse normalement."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 5,
            Nom = "Maîtres de la Non-Vie",
            Description = "Une fois par match, si un joueur adverse de Force 4 ou moins et sans le Trait Minus subit un résultat Mort, l'équipe peut Relever le Mort : elle ajoute immédiatement un joueur Trois-quart de sa Fiche d'Équipe à son Box des Réserves, pouvant temporairement dépasser 16 joueurs. À l'Après-match, ce joueur peut être embauché gratuitement et définitivement si la liste ne compte pas déjà 16 joueurs ; il ajoute quand même sa valeur à la Valeur d'Équipe."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 6,
            Nom = "Déferlement",
            Description = "Pendant la Séquence de Début de Phase, après que les deux équipes ont placé leurs joueurs, l'équipe peut placer sur le terrain D3 joueurs Trois-quart supplémentaires depuis son Box des Réserves, dépassant ainsi le maximum habituel de 11 joueurs sur le terrain."
        },
        new SpecialRule
        {
            RulesVersionId = versionId, Ordre = 7,
            Nom = "Capitaine",
            Description = "À la création de la liste, un joueur de la liste de départ (hors Gros Bras) est désigné Capitaine : il gagne immédiatement la Compétence Pro sans augmenter son coût. Si le Capitaine est sur le terrain, chaque Relance d'Équipe utilisée permet de jeter un D6 : sur un 6 naturel, la relance est gratuite. Le Capitaine doit être aligné si possible, et ne peut être renvoyé que s'il a subi une blessure réduisant une caractéristique."
        },
    ];

    /// <summary>
    /// Rattachement règle → fiches d'équipe, avec les options de divinité pour
    /// « Favori de… ». Le nom d'équipe doit correspondre à
    /// <c>BloodBowlTeamSeedData</c>.
    ///
    /// Les six alignements possibles quand une équipe a le libre choix, d'après
    /// le LRB p.93 : Hashut, Khorne, Nurgle, Slaanesh, Tzeentch, Chaos Universel.
    /// </summary>
    public const string TousAlignements = "Hashut,Khorne,Nurgle,Slaanesh,Tzeentch,Chaos Universel";

    /// <summary>(nom de la règle, nom de l'équipe, options de choix éventuelles).</summary>
    public static IEnumerable<(string Regle, string Equipe, string Options)> GetRattachements() =>
    [
        // ── Favori de… ───────────────────────────────────────────────────────
        // Alignement libre (« Favoris de… (au choix) » sur la fiche) :
        ("Favori de…", "Élus du Chaos", TousAlignements),
        ("Favori de…", "Renégats du Chaos", TousAlignements),
        // Alignement imposé par la fiche :
        ("Favori de…", "Khorne", "Khorne"),
        ("Favori de…", "Nains du Chaos", "Hashut"),
        ("Favori de…", "Nurgle", "Nurgle"),
        // Nordiques : le LRB conditionne Khorne au choix de la ligue Clash du
        // Chaos. L'application ne modélise pas encore ce choix de ligue, donc
        // Khorne est simplement proposé — décision produit assumée.
        ("Favori de…", "Nordiques", "Khorne"),

        // ── Bagarreurs Brutaux ───────────────────────────────────────────────
        ("Bagarreurs Brutaux", "Khorne", ""),
        ("Bagarreurs Brutaux", "Nains", ""),
        ("Bagarreurs Brutaux", "Nurgle", ""),
        ("Bagarreurs Brutaux", "Ogres", ""),
        ("Bagarreurs Brutaux", "Orques", ""),
        ("Bagarreurs Brutaux", "Orques Noirs", ""),

        // ── Chantage et Corruption ───────────────────────────────────────────
        ("Chantage et Corruption", "Bas-fonds", ""),
        ("Chantage et Corruption", "Gobelins", ""),
        ("Chantage et Corruption", "Nains", ""),
        ("Chantage et Corruption", "Orques Noirs", ""),
        ("Chantage et Corruption", "Snotlings", ""),

        // ── Trois-quarts à Vil Prix ──────────────────────────────────────────
        ("Trois-quarts à Vil Prix", "Ogres", ""),
        ("Trois-quarts à Vil Prix", "Snotlings", ""),

        // ── Maîtres de la Non-Vie ────────────────────────────────────────────
        ("Maîtres de la Non-Vie", "Horreurs Nécromantiques", ""),
        ("Maîtres de la Non-Vie", "Morts-Ambulants", ""),
        ("Maîtres de la Non-Vie", "Rois des Tombes", ""),
        ("Maîtres de la Non-Vie", "Vampires", ""),

        // ── Déferlement ──────────────────────────────────────────────────────
        ("Déferlement", "Snotlings", ""),

        // ── Capitaine ────────────────────────────────────────────────────────
        ("Capitaine", "Humains", ""),
        ("Capitaine", "Orques", ""),
    ];
}
