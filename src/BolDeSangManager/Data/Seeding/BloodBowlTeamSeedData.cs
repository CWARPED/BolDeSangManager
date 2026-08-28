using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/bloodbowl.md §12 (LRB Saison 3, 30 équipes).
/// Ordre alphabétique conforme au markdown (§12.1 à §12.30).
/// Les noms de compétences suivent le markdown ; les écarts avec SkillSeedData.cs
/// génèreront un warning au seed mais ne bloqueront pas le démarrage.
/// </summary>
public static class BloodBowlTeamSeedData
{
    public record TeamSeed(TeamType Type, List<PlayerPosition> Positions, List<TeamTypeKeywordLimit> Limites);

    public static IEnumerable<TeamSeed> GetTeams(int bbGameId, int bbVersionId)
    {
        // === 1. Alliance du Vieux Monde ===
        // Ligue : Classique du Vieux Monde — Relances : 70k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Alliance du Vieux Monde",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "OldWorldClassic",
                ReglesSpeciales = "Alliance hétérogène d'humains, nains et halflings."
            },
            [
                Pos("Trois-quart Humain",           16, 50_000,  6, 3, "3+", "4+",  "9+",  "G",   "A,F",   motsCles: "Trois-quart,Humain"),
                Pos("Aspirant Halfling",              3, 30_000,  5, 2, "3+", "4+",  "7+",  "A",   "G,F",   skills: "Minus,Esquive,Poids Plume",                                                                    motsCles: "Trois-quart,Halfling"),
                Pos("Receveur Humain",                1, 75_000,  8, 3, "3+", "4+",  "8+",  "G,A", "P,F",   skills: "Esquive,Réception",                                                                            motsCles: "Receveur,Humain"),
                Pos("Trois-quart Nain",               3, 70_000,  4, 3, "4+", "5+",  "10+", "S,G", "F",     skills: "Défenseur,Blocage,Crâne Épais",                                                                motsCles: "Trois-quart,Nain"),
                Pos("Lanceur Humain",                 1, 75_000,  6, 3, "3+", "3+",  "9+",  "G,P", "F,A",   skills: "Passe,Prise Sûre",                                                                             motsCles: "Lanceur,Humain"),
                Pos("Coureur Nain",                   1, 80_000,  6, 3, "3+", "4+",  "9+",  "G,P", "F",     skills: "Crâne Épais,Prise Sûre,Sprint",                                                                motsCles: "Coureur,Nain"),
                Pos("Blitzer Humain",                 1, 85_000,  7, 3, "3+", "4+",  "9+",  "G,F", "A",     skills: "Blocage,Tacle",                                                                                motsCles: "Blitzer,Humain"),
                Pos("Blitzer Nain",                   1, 100_000, 5, 3, "4+", "4+",  "10+", "G,F", "P",     skills: "Crâne Épais,Blocage,Tacle,Tacle Plongeant",                                                    motsCles: "Blitzer,Nain"),
                Pos("Tueur de Troll",                 1, 95_000,  5, 3, "4+", "5+",  "9+",  "G,F", "A",     skills: "Crâne Épais,Blocage,Intrépide,Frénésie,Haine (Troll)",                                         motsCles: "Nain,Spécial"),
                Pos("Ogre",                 1, 140_000, 5, 5, "4+", "5+",  "10+", "F",   "G,A",   skills: "Cerveau Lent,Crâne Épais,Solitaire (3+),Châtaigne,Lancer de Coéquipier",      motsCles: "Gros Bras,Ogre"),
                Pos("Homme-arbre",          1, 120_000, 2, 6, "5+", "5+",  "11+", "F",   "A,G,P", skills: "Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier,Timmm-ber", motsCles: "Gros Bras,Homme-arbre"),
            ],
            []
        );

        // === 2. Amazones ===
        // Ligue : Super-ligue de Lustrie — Relances : 60k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Amazones",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "LustrianSuperleague",
                ReglesSpeciales = "Toutes les amazones ont Esquive de base. Équipe agile sans Gros Bras."
            },
            [
                Pos("Guerrière Aigle",    16, 50_000,  6, 3, "3+", "4+", "8+", "G",   "F,A",   skills: "Esquive",                                  motsCles: "Trois-quart,Humaine"),
                Pos("Guerrière Python",    2, 80_000,  6, 3, "3+", "3+", "8+", "G,P", "F,A",   skills: "Esquive,Passe,Sur le Ballon,Passe Assurée", motsCles: "Lanceuse,Humaine"),
                Pos("Guerrière Piranha",   2, 90_000,  7, 3, "3+", "4+", "8+", "G,A", "F",     skills: "Esquive,Rétablissement,Frappe-et-Court",    motsCles: "Blitzer,Humaine"),
                Pos("Guerrière Jaguar",    2, 110_000, 6, 4, "3+", "4+", "9+", "G,F", "A",     skills: "Esquive,Défenseur",                         motsCles: "Bloqueuse,Humaine"),
            ],
            []
        );

        // === 3. Bas-fonds ===
        // Ligue : Défi des Bas-fonds — Relances : 70k — Apothicaire : Oui
        // Règles spéciales : Chantage & Corruption
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Bas-fonds",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "UnderworldChallenge",
                ReglesSpeciales = "Chantage & Corruption. Mélange de races des bas-fonds."
            },
            [
                Pos("Trois-quart Gobelin",            16, 40_000,  6, 2, "3+", "4+", "8+",  "A,S,M",   "G,P,F",   skills: "Esquive,Poids Plume,Minus",                                                                            motsCles: "Trois-quart,Gobelin"),
                Pos("Trois-quart Snotling",             6, 15_000,  5, 1, "3+", "4+", "6+",  "A,S,M",   "G",       skills: "Esquive,Insignifiant,Poids Plume,Minus,Glissade Contrôlée,Microbe",                                     motsCles: "Trois-quart,Snotling"),
                Pos("Rat des Clans",                    3, 50_000,  7, 3, "3+", "4+", "8+",  "S,G,M",   "A,F",     skills: "Animosité (Gobelins)",                                                                                  motsCles: "Trois-quart,Skaven"),
                Pos("Lanceur Skaven",                   1, 80_000,  7, 3, "3+", "2+", "8+",  "G,M,P",   "A,S,F",   skills: "Animosité (Gobelins),Passe,Prise Sûre",                                                                motsCles: "Lanceur,Skaven"),
                Pos("Coureur d'Égouts",                 1, 85_000,  9, 2, "2+", "4+", "8+",  "A,S,G,M", "F",       skills: "Animosité (Gobelins),Esquive,Poignard",                                                                 motsCles: "Coureur,Skaven"),
                Pos("Blitzer Skaven",                   1, 90_000,  8, 3, "3+", "4+", "9+",  "G,M,F",   "A,S",     skills: "Animosité (Gobelins),Blocage,Arracher le Ballon",                                                       motsCles: "Blitzer,Skaven"),
                Pos("Troll",                  1, 115_000, 4, 5, "5+", "5+", "10+", "M,F",     "G,A,P",   skills: "Toujours Affamé,Solitaire (4+),Gerbe de Vomi,Châtaigne,Gros Débile,Régénération,Lancer de Coéquipier", motsCles: "Gros Bras,Troll"),
                Pos("Rat Ogre",               1, 150_000, 6, 5, "4+", "6+", "9+",  "M,F",     "G,A",     skills: "Sauvagerie Animale,Frénésie,Solitaire (4+),Châtaigne,Queue Préhensile",                motsCles: "Gros Bras,Skaven"),
            ],
            []
        );

        // === 4. Bretonniens ===
        // Ligue : Classique du Vieux Monde — Relances : 60k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Bretonniens",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "OldWorldClassic",
                ReglesSpeciales = "Chevaliers bretonniens et écuyers. Équipe équilibrée avec excellente armure."
            },
            [
                Pos("Écuyer Bretonnien",    16, 50_000,  6, 3, "3+", "4+", "8+",  "G",   "A,F",   skills: "Lutte",                              motsCles: "Trois-quart,Humain"),
                Pos("Receveur Chevalier",    2, 85_000,  7, 3, "3+", "4+", "9+",  "A,G", "F",     skills: "Intrépide,Nerfs d'Acier,Réception",  motsCles: "Receveur,Humain"),
                Pos("Lanceur Chevalier",     2, 80_000,  6, 3, "3+", "3+", "9+",  "G,P", "A,F",   skills: "Intrépide,Nerfs d'Acier,Passe",      motsCles: "Lanceur,Humain"),
                Pos("Chevalier du Graal",    2, 95_000,  7, 3, "3+", "4+", "10+", "G,F", "A",     skills: "Blocage,Intrépide,Appuis Sûrs",      motsCles: "Blitzer,Humain"),
            ],
            []
        );

        // === 5. Elfes Noirs ===
        // Ligue : Ligue des Royaumes Elfiques — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Elfes Noirs",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "ElvenKingdoms",
                ReglesSpeciales = "Elfes cruels et agiles. Équipe agile avec compétences scélérates."
            },
            [
                Pos("Trois-quart Elfe",               16, 65_000,  6, 3, "2+", "3+", "9+", "G,A", "S,F",                                                   motsCles: "Trois-quart,Elfe"),
                Pos("Coureur Elfe",                    2, 80_000,  7, 3, "2+", "3+", "8+", "G,A,P", "S,F",   skills: "Délestage,Dégagement",               motsCles: "Coureur,Elfe"),
                Pos("Assassin",                        2, 90_000,  7, 3, "2+", "4+", "8+", "S,A", "F,G",     skills: "Poursuite,Poignard,Frappe-et-Court", motsCles: "Elfe,Spécial"),
                Pos("Blitzer Elfe",                    2, 105_000, 7, 3, "2+", "3+", "9+", "G,A", "S,F,P",   skills: "Blocage",                            motsCles: "Blitzer,Elfe"),
                Pos("Furie",                           2, 110_000, 7, 3, "2+", "4+", "8+", "G,A", "F,S",     skills: "Esquive,Frénésie,Rétablissement",    motsCles: "Elfe,Spécial"),
            ],
            []
        );

        // === 6. Elfes Sylvains ===
        // Ligue : Ligue des Royaumes Elfiques — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Elfes Sylvains",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "ElvenKingdoms",
                ReglesSpeciales = "Elfes rapides et agiles des forêts. Incluent un Homme-arbre."
            },
            [
                Pos("Trois-quart Elfe",               16, 65_000,  7, 3, "2+", "3+", "8+", "G,A", "F",                                                               motsCles: "Trois-quart,Elfe"),
                Pos("Lanceur Elfe",                    2, 85_000,  7, 3, "2+", "2+", "8+", "G,A,P", "F",     skills: "Passe,Libération Contrôlée",                    motsCles: "Lanceur,Elfe"),
                Pos("Receveur Elfe",                   2, 90_000,  8, 2, "2+", "3+", "8+", "G,A", "F,P",     skills: "Réception,Esquive,Sprint",                      motsCles: "Receveur,Elfe"),
                Pos("Danseur de Guerre",               2, 130_000, 8, 3, "2+", "3+", "8+", "G,A", "F,P",     skills: "Blocage,Esquive,Saut",                          motsCles: "Blitzer,Elfe"),
                Pos("Homme-arbre",           1, 120_000, 2, 6, "5+", "5+", "11+", "F",  "A,G,P",   skills: "Solitaire (4+),Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier", motsCles: "Gros Bras,Homme-arbre"),
            ],
            []
        );

        // === 7. Élus du Chaos ===
        // Ligue : Clash du Chaos — Relances : 50k — Apothicaire : Oui
        // Règles spéciales : Favoris de… (au choix)
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Élus du Chaos",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "ChaosClash,FavoredOfChaos",
                ReglesSpeciales = "Favoris de… (au choix). Hommes-bêtes et élus avec mutations."
            },
            [
                Pos("Trois-quart Homme-Bête",         16, 55_000,  6, 3, "3+", "3+", "9+",  "G,M",   "A,S,F,P", skills: "Cornes,Crâne Épais",                                                                              motsCles: "Trois-quart,Homme-Bête"),
                Pos("Élu du Chaos",                   4, 100_000, 5, 4, "3+", "5+", "10+", "G,F,M", "A,S",     skills: "Clé de Bras",                                                                                     motsCles: "Bloqueur,Humain"),
                Pos("Troll",                 1, 115_000, 4, 5, "5+", "5+", "10+", "F,M",   "G,A,P",   skills: "Toujours Affamé,Solitaire (4+),Gerbe de Vomi,Châtaigne,Gros Débile,Régénération,Lancer de Coéquipier", motsCles: "Gros Bras,Troll"),
                Pos("Ogre",                  1, 140_000, 5, 5, "4+", "5+", "10+", "F,M",   "G,A",     skills: "Cerveau Lent,Crâne Épais,Solitaire (4+),Châtaigne,Lancer de Coéquipier",             motsCles: "Gros Bras,Ogre"),
                Pos("Minotaure",             1, 150_000, 5, 5, "4+", "6+", "9+",  "F,M",   "G,A",     skills: "Solitaire (4+),Frénésie,Cornes,Châtaigne,Crâne Épais,Fureur Débridée",              motsCles: "Gros Bras,Minotaure"),
            ],
            []
        );

        // === 8. Gnomes ===
        // Ligue : Coupe du Dé à Coudre Halfling, Ligue Sylvestre — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Gnomes",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "HalflingThimbleCup",
                ReglesSpeciales = "Petite race agile avec animaux renards. Nombreuses compétences défensives."
            },
            [
                Pos("Trois-quart Gnome",              16, 40_000,  5, 2, "3+", "4+", "7+",  "A",   "S,G,F",   skills: "Rétablissement,Poids Plume,Minus,Lutte",                                                           motsCles: "Trois-quart,Gnome"),
                Pos("Renard Sylvestre",                2, 50_000,  7, 2, "2+", "-",  "6+",  "-",   "A",       skills: "Esquive,Mon Ballon,Glissade Contrôlée,Minus",                                                      motsCles: "Coureur,Animal"),
                Pos("Illusionniste Gnome",             2, 50_000,  5, 2, "3+", "3+", "7+",  "A,P", "S,G",     skills: "Rétablissement,Minus,Farceur,Lutte",                                                               motsCles: "Gnome,Spécial"),
                Pos("Belluaire Gnome",                2, 55_000,  5, 2, "3+", "4+", "8+",  "A",   "S,G,F",   skills: "Garde,Rétablissement,Minus,Lutte",                                                                 motsCles: "Bloqueur,Gnome"),
                Pos("Homme-arbre",           2, 120_000, 2, 6, "5+", "5+", "11+", "F",   "A,G,P",   skills: "Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier,Timmm-ber", motsCles: "Gros Bras,Homme-arbre"),
            ],
            []
        );

        // === 9. Gobelins ===
        // Ligue : Bagarre des Terres Arides, Défi des Bas-Fonds — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Chantage & Corruption
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Gobelins",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "BadlandsBrawl,UnderworldChallenge",
                ReglesSpeciales = "Chantage & Corruption. Spécialistes farfelus avec armes secrètes."
            },
            [
                Pos("Trois-quart Gobelin",            16, 40_000,  6, 2, "3+", "4+", "8+",  "A,S", "G,F,P",   skills: "Esquive,Poids Plume,Minus",                                                                        motsCles: "Trois-quart,Gobelin"),
                Pos("Cinglé",                          1, 40_000,  6, 2, "3+", "-",  "8+",  "S",   "A,G,F",   skills: "Tronçonneuse,Arme Secrète,Minus,Sans Ballon",                                                      motsCles: "Gobelin,Spécial"),
                Pos("Bomba",                          1, 45_000,  6, 2, "3+", "4+", "8+",  "S,P", "A,G,F",   skills: "Bombardier,Esquive,Arme Secrète,Minus",                                                            motsCles: "Gobelin,Spécial"),
                Pos("Ouligan'",                       1, 60_000,  6, 2, "3+", "5+", "8+",  "A,S", "G,F",     skills: "Joueur Déloyal,Présence Perturbante,Esquive,Poids Plume,Minus,Provocation",                        motsCles: "Gobelin,Spécial"),
                Pos("Planeur de la Mort",             1, 65_000,  6, 2, "3+", "6+", "8+",  "A",   "S,G,F",   skills: "Esquive,Poids Plume,Minus,Piqué",                                                                  motsCles: "Gobelin,Spécial"),
                Pos("Fanatique",                      1, 70_000,  3, 7, "3+", "-",  "8+",  "S,F", "A,G",     skills: "Chaîne & Boulet,Sans Ballon,Arme Secrète,Minus",                                                   motsCles: "Gobelin,Spécial"),
                Pos("Échassier à Ressort",             1, 75_000,  7, 2, "3+", "4+", "8+",  "A",   "G,F,S",   skills: "Esquive,Monté sur Ressort,Minus",                                                                  motsCles: "Gobelin,Spécial"),
                Pos("Troll Entraîné",                  2, 115_000, 4, 5, "5+", "5+", "10+", "F",   "A,G,P",   skills: "Toujours Affamé,Gros Débile,Châtaigne,Lancer de Coéquipier,Gerbe de Vomi,Régénération", motsCles: "Gros Bras,Troll"),
            ],
            []
        );

        // === 10. Halflings ===
        // Ligue : Coupe du Dé à Coudre Halfling, Ligue Sylvestre — Relances : 60k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Halflings",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "HalflingThimbleCup",
                ReglesSpeciales = "Petite race avec Hommes-arbres. Difficile à gérer, très fun."
            },
            [
                Pos("Aspirant Halfling",               16, 30_000,  5, 2, "3+", "4+", "7+",  "A",   "S,G,F",   skills: "Esquive,Poids Plume,Minus",                                                                       motsCles: "Trois-quart,Halfling"),
                Pos("Balaise Halfling",                 2, 50_000,  5, 2, "3+", "3+", "8+",  "A,P", "S,G,F",   skills: "Esquive,Parade,Minus",                                                                            motsCles: "Bloqueur,Halfling"),
                Pos("Receveur Halfling",                2, 55_000,  5, 2, "3+", "4+", "7+",  "A",   "S,G,F",   skills: "Réception,Esquive,Poids Plume,Minus,Sprint",                                                      motsCles: "Receveur,Halfling"),
                Pos("Homme-arbre",            2, 120_000, 2, 6, "5+", "5+", "11+", "F",   "A,G,P",   skills: "Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier,Timmm-ber", motsCles: "Gros Bras,Homme-arbre"),
            ],
            []
        );

        // === 11. Hauts Elfes ===
        // Ligue : Ligue des Royaumes Elfiques — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Hauts Elfes",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "ElvenKingdoms",
                ReglesSpeciales = "Elfes nobles et équilibrés. Bon mix offensif/défensif."
            },
            [
                Pos("Trois-quart Elfe",               16, 65_000,  6, 3, "2+", "3+", "9+", "G,A", "F",                                                  motsCles: "Trois-quart,Elfe"),
                Pos("Lion Blanc",                       2, 110_000, 7, 3, "2+", "3+", "9+", "G,A", "F,P",   skills: "Griffes,Lutte",                     motsCles: "Blitzer,Elfe"),
                Pos("Guerrier Phénix",                  2, 90_000,  6, 3, "2+", "2+", "9+", "G,A,P", "F",   skills: "Passe,Passe Assurée,Perce-Nuages",  motsCles: "Lanceur,Elfe"),
                Pos("Prince Dragon",                   2, 110_000, 8, 3, "2+", "4+", "9+", "G,A", "F",     skills: "Appuis Sûrs,Blocage,Mon Ballon",     motsCles: "Blitzer,Coureur,Elfe"),
            ],
            []
        );

        // === 12. Hommes-lézards ===
        // Ligue : Super-ligue de Lustrie — Relances : 70k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Hommes-lézards",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "LustrianSuperleague",
                ReglesSpeciales = "Combinaison de Skinks agiles et de Saurus puissants. Kroxigor disponible."
            },
            [
                Pos("Trois-quart Skink",              16, 60_000,  8, 2, "3+", "4+", "8+",  "A",   "G,S,P,F", skills: "Esquive,Minus",                                                                              motsCles: "Trois-quart,Homme-Lézard"),
                Pos("Skink Caméléon",                  2, 70_000,  7, 2, "3+", "3+", "8+",  "A,P", "G,S,F",   skills: "Esquive,Sur le Ballon,Poursuite,Minus",                                                      motsCles: "Lanceur,Homme-Lézard"),
                Pos("Bloqueur Saurus",                 6, 90_000,  6, 4, "5+", "6+", "10+", "G,F", "A",       skills: "Juggernaut,Instable",                                                                        motsCles: "Bloqueur,Homme-Lézard"),
                Pos("Kroxigor",                        1, 140_000, 6, 5, "5+", "6+", "10+", "F",   "A,G",     skills: "Cerveau Lent,Solitaire (4+),Châtaigne,Crâne Épais,Queue Préhensile",        motsCles: "Gros Bras,Homme-Lézard"),
            ],
            []
        );

        // === 13. Horreurs Nécromantiques ===
        // Ligue : Spot de Sylvanie — Relances : 70k — Apothicaire : Non
        // Règles spéciales : Maîtres de la Non-Vie
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Horreurs Nécromantiques",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "MastersOfUndeath",
                ReglesSpeciales = "Maîtres de la Non-Vie. Pas d'apothicaire. Zombies, goules, spectres, golems et loups-garous."
            },
            [
                Pos("Trois-quart Zombie",             16, 40_000,  4, 3, "4+", "6+", "9+",  "S,G", "A,F",     skills: "Fourchette,Instable,Régénération",                                         motsCles: "Trois-quart,Zombie,Humain,Mort-Vivant"),
                Pos("Coureur Goule",                   2, 75_000,  7, 3, "3+", "3+", "8+",  "A,G", "S,P,F",   skills: "Esquive,Régénération",                                                     motsCles: "Coureur,Goule,Mort-Vivant"),
                Pos("Spectre",                          2, 85_000,  6, 3, "3+", "-",  "9+",  "G,F", "A,S",     skills: "Blocage,Répulsion,Sans Ballon,Régénération,Glissade Contrôlée",            motsCles: "Bloqueur,Spectre,Mort-Vivant"),
                Pos("Golem de Chair",                  2, 110_000, 4, 4, "4+", "6+", "10+", "G,F", "A,S",     skills: "Régénération,Stabilité,Crâne Épais,Instable",                              motsCles: "Bloqueur,Artefact,Mort-Vivant"),
                Pos("Loup-garou",                      2, 120_000, 8, 3, "3+", "3+", "9+",  "A,G", "S,P,F",   skills: "Griffes,Frénésie,Régénération",                                           motsCles: "Blitzer,Loup-garou,Mort-Vivant"),
            ],
            []
        );

        // === 14. Humains ===
        // Ligue : Classique du Vieux Monde — Relances : 50k — Apothicaire : Oui
        // Règles spéciales : Capitaine
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Humains",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "OldWorldClassic",
                ReglesSpeciales = "Capitaine. Équipe équilibrée polyvalente avec un Ogre optionnel."
            },
            [
                Pos("Trois-quart Humain",             16, 50_000,  6, 3, "3+", "4+", "9+",  "G",   "A,S,F",                                                                                          motsCles: "Trois-quart,Humain"),
                Pos("Aspirant Halfling",               3, 30_000,  5, 2, "3+", "4+", "7+",  "A",   "S,G,F",   skills: "Esquive,Poids Plume,Minus",                                                   motsCles: "Trois-quart,Halfling"),
                Pos("Receveur Humain",                 2, 75_000,  8, 3, "3+", "4+", "8+",  "G,A", "S,F,P",   skills: "Réception,Esquive",                                                           motsCles: "Receveur,Humain"),
                Pos("Lanceur Humain",                  2, 75_000,  6, 3, "3+", "3+", "9+",  "G,P", "A,S,F",   skills: "Passe,Prise Sûre",                                                            motsCles: "Lanceur,Humain"),
                Pos("Blitzer Humain",                  2, 85_000,  7, 3, "3+", "4+", "9+",  "G,F", "A,S",     skills: "Blocage,Tacle",                                                               motsCles: "Blitzer,Humain"),
                Pos("Ogre",                  1, 140_000, 5, 5, "4+", "5+", "10+", "F",   "A,G",     skills: "Cerveau Lent,Solitaire (3+),Châtaigne,Crâne Épais,Lancer de Coéquipier", motsCles: "Gros Bras,Ogre"),
            ],
            []
        );

        // === 15. Khorne ===
        // Ligue : Clash du Chaos — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Favoris de Khorne, Bagarreurs Brutaux
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Khorne",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "ChaosClash",
                ReglesSpeciales = "Favoris de Khorne, Bagarreurs Brutaux. Équipe agressive dédiée au combat."
            },
            [
                Pos("Maraudeur Sanglant",             16, 50_000,  6, 3, "3+", "4+", "8+",  "G,M",   "A,S,F",   skills: "Frénésie",                                                                    motsCles: "Trois-quart,Humain"),
                Pos("Khorngor",                          2, 70_000,  6, 3, "3+", "4+", "9+",  "G,F,M", "A,S,P",   skills: "Cornes,Juggernaut,Rétablissement,Crâne Épais",                                motsCles: "Coureur,Homme-Bête"),
                Pos("Rabatteur Sanglant",                4, 105_000, 5, 4, "4+", "6+", "10+", "G,F,M", "A,S",     skills: "Frénésie",                                                                    motsCles: "Bloqueur,Humain"),
                Pos("Rejeton Sanglant",                  1, 160_000, 5, 5, "4+", "6+", "9+",  "F,M",   "A,G",     skills: "Griffes,Frénésie,Solitaire (4+),Châtaigne,Fureur Débridée",    motsCles: "Gros Bras,Rejeton"),
            ],
            []
        );

        // === 16. Morts-Ambulants ===
        // Ligue : Spot de Sylvanie — Relances : 70k — Apothicaire : Non
        // Règles spéciales : Maîtres de la Non-Vie
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Morts-Ambulants",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "MastersOfUndeath",
                ReglesSpeciales = "Maîtres de la Non-Vie. Pas d'apothicaire. Squelettes, zombies, goules, blitzers et momies."
            },
            [
                Pos("Trois-quart Squelette",          16, 40_000,  5, 3, "4+", "6+", "8+",  "G",   "A,S,F",   skills: "Régénération,Crâne Épais",                                         motsCles: "Trois-quart,Humain,Squelette,Mort-Vivant"),
                Pos("Trois-quart Zombie",              16, 40_000,  4, 3, "4+", "6+", "9+",  "S,G", "A,F",     skills: "Fourchette,Régénération,Instable",                                  motsCles: "Trois-quart,Humain,Zombie,Mort-Vivant"),
                Pos("Coureur Goule",                    2, 75_000,  7, 3, "3+", "3+", "8+",  "A,G", "S,P,F",   skills: "Esquive,Régénération",                                             motsCles: "Coureur,Goule,Mort-Vivant"),
                Pos("Blitzer Squelette",                2, 95_000,  6, 3, "3+", "5+", "9+",  "G,F", "A,S",     skills: "Blocage,Régénération,Tacle,Crâne Épais",                           motsCles: "Blitzer,Humain,Squelette,Mort-Vivant"),
                Pos("Momie",                            2, 125_000, 3, 5, "5+", "6+", "10+", "F",   "A,G",     skills: "Régénération,Châtaigne",                            motsCles: "Humain,Mort-Vivant,Bloqueur,Gros Bras"),
            ],
            []
        );

        // === 17. Nains ===
        // Ligue : Super-ligue du Bord du Monde — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Chantage & Corruption, Bagarreurs Brutaux
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Nains",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "BadlandsBrawl",
                ReglesSpeciales = "Chantage & Corruption, Bagarreurs Brutaux. Équipe lente mais très résistante avec Roule-Mort."
            },
            [
                Pos("Trois-quart Nain",               16, 70_000,  4, 3, "4+", "5+", "10+", "G,S", "F",       skills: "Blocage,Défenseur,Crâne Épais",                                                                           motsCles: "Trois-quart,Nain"),
                Pos("Coureur Nain",                    2, 80_000,  6, 3, "3+", "4+", "9+",  "G,P", "F",       skills: "Prise Sûre,Crâne Épais,Sprint",                                                                           motsCles: "Coureur,Nain"),
                Pos("Blitzer Nain",                    2, 100_000, 5, 3, "4+", "4+", "10+", "G,F", "P",       skills: "Blocage,Tacle,Tacle Plongeant,Crâne Épais",                                                               motsCles: "Blitzer,Nain"),
                Pos("Tueur de Troll",                  2, 95_000,  5, 3, "4+", "5+", "9+",  "G,F", "S",       skills: "Blocage,Intrépide,Frénésie,Crâne Épais,Haine (Troll)",                                                    motsCles: "Nain,Spécial"),
                Pos("Roule-Mort",                      1, 170_000, 5, 7, "5+", "-",  "11+", "S,F", "G",       skills: "Esquive en Force,Joueur Déloyal,Juggernaut,Solitaire (4+),Châtaigne,Sans Ballon,Arme Secrète,Stabilité", motsCles: "Gros Bras,Nain,Spécial"),
            ],
            []
        );

        // === 18. Nains du Chaos ===
        // Ligue : Clash du Chaos, Bagarre des Terres Arides — Relances : 70k — Apothicaire : Oui
        // Règles spéciales : Favoris de Hashut
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Nains du Chaos",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "ChaosClash,BadlandsBrawl",
                ReglesSpeciales = "Favoris de Hashut. Nains du chaos avec hobgobelins et minotaure."
            },
            [
                Pos("Trois-quart Hobgobelin",         16, 40_000,  6, 3, "3+", "4+", "8+",  "G",   "F,A",                                                                                        motsCles: "Trois-quart,Gobelin"),
                Pos("Surineur Sournois",               2, 60_000,  6, 3, "3+", "5+", "8+",  "S,G", "F,A",     skills: "Poursuite,Poignard",                                                      motsCles: "Gobelin,Spécial"),
                Pos("Bloqueur Nain du Chaos",          4, 70_000,  4, 3, "4+", "6+", "10+", "G,F", "A,S,M",   skills: "Blocage,Peau de Fer,Crâne Épais",                                        motsCles: "Bloqueur,Nain"),
                Pos("Forgeflamme",                    2, 80_000,  5, 3, "4+", "6+", "10+", "G,F", "A,M",     skills: "Bagarreur,Souffle Ardent,Présence Perturbante,Crâne Épais",               motsCles: "Nain,Spécial"),
                Pos("Centaure Taureau",               2, 130_000, 6, 4, "4+", "6+", "10+", "G,F", "A,S,M",   skills: "Sprint,Équilibre,Crâne Épais,Instable",                                   motsCles: "Blitzer,Nain"),
                Pos("Minotaure",                      1, 150_000, 5, 5, "4+", "6+", "9+",  "M,F", "G,A",     skills: "Solitaire (4+),Frénésie,Cornes,Châtaigne,Crâne Épais,Fureur Débridée", motsCles: "Gros Bras,Minotaure"),
            ],
            []
        );

        // === 19. Noblesse Impériale ===
        // Ligue : Classique du Vieux Monde — Relances : 60k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Noblesse Impériale",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "OldWorldClassic",
                ReglesSpeciales = "Équipe noble avec gardes du corps robustes et Ogre optionnel."
            },
            [
                Pos("Valet Impérial",                 16, 45_000,  6, 3, "3+", "4+", "8+",  "G",   "A,F",     skills: "Parade",                                                                          motsCles: "Trois-quart,Humain"),
                Pos("Lanceur Impérial",                2, 75_000,  6, 3, "3+", "2+", "9+",  "G,P", "A,F",     skills: "Passe,Transmission dans la Course,Pro",                                           motsCles: "Lanceur,Humain"),
                Pos("Garde du Corps",                  4, 85_000,  5, 3, "3+", "4+", "9+",  "G,F", "A",       skills: "Stabilité,Lutte",                                                                 motsCles: "Bloqueur,Humain"),
                Pos("Noble Blitzer",                   2, 90_000,  7, 3, "3+", "4+", "9+",  "G,A", "P,F",     skills: "Blocage,Réception,Pro",                                                           motsCles: "Blitzer,Humain"),
                Pos("Ogre",                  1, 140_000, 5, 5, "4+", "5+", "10+", "F",   "A,G",     skills: "Cerveau Lent,Solitaire (3+),Châtaigne,Crâne Épais,Lancer de Coéquipier", motsCles: "Gros Bras,Ogre"),
            ],
            []
        );

        // === 20. Nordiques ===
        // Ligue : Classique du Vieux Monde, Clash du Chaos — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Si Clash du Chaos → Favoris de Khorne
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Nordiques",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "OldWorldClassic,ChaosClash",
                ReglesSpeciales = "Si Clash du Chaos → Favoris de Khorne. Barbares nordiques solides avec Yéti."
            },
            [
                Pos("Pillard Nordique",               16, 50_000,  6, 3, "3+", "4+", "8+",  "G",   "A,P,F",   skills: "Blocage,Crâne Épais,Ivrogne,Instable",                                             motsCles: "Trois-quart,Humain"),
                Pos("Sanglier de Secours",             2, 20_000,  5, 1, "3+", "-",  "6+",  "-",   "A",       skills: "Esquive,Sans Ballon,Minus,Microbe,Petit Remontant",                                 motsCles: "Animal,Spécial"),
                Pos("Berserker Nordique",              2, 90_000,  6, 3, "3+", "5+", "8+",  "G,F", "A,P",     skills: "Blocage,Frénésie,Rétablissement",                                                  motsCles: "Blitzer,Humain"),
                Pos("Valkyrie",                       2, 95_000,  7, 3, "3+", "3+", "8+",  "A,G,P", "F",     skills: "Réception,Intrépide,Passe,Arracher le Ballon",                                     motsCles: "Receveur,Lanceur,Humain"),
                Pos("Ulfwerener",                     2, 105_000, 6, 4, "4+", "6+", "9+",  "G,F", "A",       skills: "Frénésie,Instable",                                                                motsCles: "Bloqueur,Humain"),
                Pos("Yéti",                           1, 140_000, 5, 5, "4+", "6+", "9+",  "F",   "G,A",     skills: "Solitaire (4+),Griffes,Présence Perturbante,Frénésie,Fureur Débridée", motsCles: "Gros Bras,Yéti"),
            ],
            []
        );

        // === 21. Nurgle ===
        // Ligue : Clash du Chaos — Relances : 60k — Apothicaire : Non
        // Règles spéciales : Favoris de Nurgle, Bagarreurs Brutaux
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Nurgle",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "ChaosClash,FavoredOfNurgle",
                ReglesSpeciales = "Favoris de Nurgle, Bagarreurs Brutaux. Pas d'apothicaire. Équipe pestilentielle et robuste."
            },
            [
                Pos("Trois-quart Putrescent",         16, 40_000,  5, 3, "4+", "6+", "9+",  "S,G,M", "A,F",     skills: "Décomposition,Contagieux",                                                                                     motsCles: "Trois-quart,Humain"),
                Pos("Pestigor",                       2, 70_000,  6, 3, "3+", "4+", "9+",  "G,M,F", "A,P,S",   skills: "Cornes,Contagieux,Régénération,Crâne Épais,Appuis Sûrs",                                                       motsCles: "Coureur,Homme-Bête"),
                Pos("Boursouflé",                     4, 110_000, 4, 4, "4+", "6+", "10+", "G,M,F", "A,S",     skills: "Présence Perturbante,Répulsion,Contagieux,Régénération,Instable,Stabilité",                                    motsCles: "Bloqueur,Humain"),
                Pos("Rejeton Putride",                1, 140_000, 4, 5, "5+", "6+", "10+", "F",     "G,S,M",   skills: "Solitaire (4+),Répulsion,Présence Perturbante,Châtaigne,Contagieux,Gros Débile,Régénération,Tentacules,Petit Remontant", motsCles: "Gros Bras,Rejeton"),
            ],
            []
        );

        // === 22. Ogres ===
        // Ligue : Bagarre des Terres Arides, Classique du Vieux Monde — Relances : 70k — Apothicaire : Oui
        // Règles spéciales : Bagarreurs Brutaux, Trois-quarts à Vil Prix
        // ⚠️ Le markdown liste aussi "Favoris de Nurgle" pour les Ogres — probablement une erreur dans la source.
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Ogres",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "BadlandsBrawl,OldWorldClassic",
                ReglesSpeciales = "Bagarreurs Brutaux, Trois-quarts à Vil Prix. Ogres avec Gnoblars minuscules."
            },
            [
                Pos("Trois-quart Gnoblar",            16, 15_000,  5, 1, "3+", "4+", "6+",  "A,S", "G",       skills: "Esquive,Poids Plume,Minus,Glissade Contrôlée,Microbe",                       motsCles: "Trois-quart,Gnoblar"),
                Pos("Bloqueur Ogre",                   5, 140_000, 5, 5, "4+", "5+", "10+", "F",   "A,S,G,P", skills: "Cerveau Lent,Châtaigne,Crâne Épais,Lancer de Coéquipier",    motsCles: "Bloqueur,Gros Bras,Ogre"),
                Pos("Botteur Ogre",                    1, 145_000, 5, 5, "4+", "4+", "10+", "P,F", "A,S,G",  skills: "Cerveau Lent,Châtaigne,Crâne Épais,Botter de Coéquipier",   motsCles: "Lanceur,Gros Bras,Ogre"),
            ],
            []
        );

        // === 23. Orques ===
        // Ligue : Bagarre des Terres Arides — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Bagarreurs Brutaux, Capitaine
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Orques",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "BadlandsBrawl",
                ReglesSpeciales = "Bagarreurs Brutaux, Capitaine. Équipe équilibrée avec gobelins et Troll."
            },
            [
                Pos("Trois-quart Orque",              16, 50_000,  5, 3, "3+", "4+", "10+", "G,F", "A,S",                                                                                                     motsCles: "Trois-quart,Orque"),
                Pos("Trois-quart Gobelin",             4, 40_000,  6, 2, "3+", "4+", "8+",  "A,S", "G,F,P",   skills: "Esquive,Poids Plume,Minus",                                                           motsCles: "Trois-quart,Gobelin"),
                Pos("Lanceur Orque",                   2, 75_000,  6, 3, "3+", "3+", "9+",  "G,P", "A,S,F",   skills: "Passe,Prise Sûre",                                                                    motsCles: "Lanceur,Orque"),
                Pos("Blitzer Orque",                   2, 85_000,  6, 3, "3+", "4+", "10+", "G,F", "A,S",     skills: "Blocage,Esquive en Force",                                                             motsCles: "Blitzer,Orque"),
                Pos("Bloqueur Kosto",                  2, 95_000,  5, 4, "4+", "6+", "10+", "G,F", "A,S",     skills: "Châtaigne,Provocation,Crâne Épais,Instable",                                          motsCles: "Bloqueur,Orque"),
                Pos("Troll",                 1, 115_000, 4, 5, "5+", "5+", "10+", "F",   "A,G,P",   skills: "Toujours Affamé,Solitaire (4+),Gerbe de Vomi,Châtaigne,Gros Débile,Régénération,Lancer de Coéquipier", motsCles: "Gros Bras,Troll"),
            ],
            []
        );

        // === 24. Orques Noirs ===
        // Ligue : Bagarre des Terres Arides — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Chantage & Corruption, Bagarreurs Brutaux
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Orques Noirs",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "BadlandsBrawl",
                ReglesSpeciales = "Chantage & Corruption, Bagarreurs Brutaux. Orques noirs très physiques."
            },
            [
                Pos("Malabar Gobelin",                16, 45_000,  6, 2, "3+", "4+", "8+",  "A,S", "G,P,F",   skills: "Esquive,Poids Plume,Minus,Crâne Épais",                                              motsCles: "Trois-quart,Gobelin"),
                Pos("Orque Noir",                      6, 90_000,  4, 4, "4+", "5+", "10+", "G,F", "A,S",     skills: "Bagarreur,Projection",                                                               motsCles: "Bloqueur,Orque Noir"),
                Pos("Troll Entraîné",                  1, 115_000, 4, 5, "5+", "5+", "10+", "F",   "A,G,P",   skills: "Toujours Affamé,Gros Débile,Châtaigne,Lancer de Coéquipier,Gerbe de Vomi,Régénération", motsCles: "Gros Bras,Troll"),
            ],
            []
        );

        // === 25. Renégats du Chaos ===
        // Ligue : Clash du Chaos — Relances : 70k — Apothicaire : Oui
        // Règles spéciales : Favoris de… (au choix)
        // Note : max 3 Gros Bras dans l'équipe.
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Renégats du Chaos",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "ChaosClash,FavoredOfChaos",
                ReglesSpeciales = "Favoris de… (au choix). Mélange de races renégates. Maximum 3 Gros Bras."
            },
            [
                Pos("Humain Renégat",                  16, 50_000,  6, 3, "3+", "4+", "9+",  "S,G,M",   "A,F",     skills: "Animosité (tous)",                                                                                  motsCles: "Trois-quart,Humain"),
                Pos("Gobelin Renégat",                  1, 40_000,  6, 2, "3+", "4+", "8+",  "A,S,M",   "G,P",     skills: "Animosité (tous),Esquive,Minus,Poids Plume",                                                        motsCles: "Trois-quart,Gobelin"),
                Pos("Orque Renégat",                    1, 50_000,  5, 3, "3+", "4+", "10+", "S,G,M",   "A,F",     skills: "Animosité (tous)",                                                                                  motsCles: "Trois-quart,Orque"),
                Pos("Skaven Renégat",                   1, 50_000,  7, 3, "3+", "4+", "8+",  "G,S,M",   "A,F",     skills: "Animosité (tous)",                                                                                  motsCles: "Trois-quart,Skaven"),
                Pos("Elfe Noir Renégat",               1, 65_000,  6, 3, "2+", "3+", "9+",  "S,G,A,M", "F",       skills: "Animosité (tous)",                                                                                  motsCles: "Trois-quart,Elfe Noir"),
                Pos("Lanceur Humain Renégat",           1, 75_000,  6, 3, "3+", "3+", "9+",  "S,G,M,P", "A,F",     skills: "Animosité (tous),Passe,Prise Sûre",                                                                 motsCles: "Lanceur,Humain"),
                Pos("Troll Renégat",                   1, 115_000, 4, 5, "5+", "5+", "10+", "F",       "G,A,M,P", skills: "Toujours Affamé,Solitaire (4+),Gerbe de Vomi,Châtaigne,Gros Débile,Régénération,Lancer de Coéquipier", motsCles: "Gros Bras,Troll"),
                Pos("Ogre Renégat",                    1, 140_000, 5, 5, "4+", "5+", "10+", "F",       "G,A,M",   skills: "Cerveau Lent,Crâne Épais,Solitaire (4+),Châtaigne,Lancer de Coéquipier",             motsCles: "Gros Bras,Ogre"),
                Pos("Minotaure Renégat",               1, 150_000, 5, 5, "4+", "6+", "9+",  "F",       "G,A,M",   skills: "Solitaire (4+),Frénésie,Cornes,Châtaigne,Crâne Épais,Fureur Débridée",              motsCles: "Gros Bras,Minotaure"),
                Pos("Rat Ogre Renégat",                1, 150_000, 6, 5, "4+", "6+", "9+",  "F",       "G,A,M",   skills: "Sauvagerie Animale,Frénésie,Solitaire (4+),Châtaigne,Queue Préhensile",              motsCles: "Gros Bras,Skaven"),
            ],
            [new TeamTypeKeywordLimit { MotCle = "Gros Bras", Max = 3 }]
        );

        // === 26. Rois des Tombes ===
        // Ligue : Spot de Sylvanie — Relances : 60k — Apothicaire : Non
        // Règles spéciales : Maîtres de la Non-Vie
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Rois des Tombes",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "MastersOfUndeath",
                ReglesSpeciales = "Maîtres de la Non-Vie. Pas d'apothicaire. Morts-vivants khémriens avec Gardiens des Tombes."
            },
            [
                Pos("Trois-quart Squelette",          16, 40_000,  5, 3, "4+", "6+", "8+",  "G",   "A,S,F",   skills: "Régénération,Crâne Épais",                                              motsCles: "Trois-quart,Humain,Squelette,Mort-Vivant"),
                Pos("Lanceur Squelette",                2, 65_000,  6, 3, "4+", "3+", "9+",  "G,P", "A,S,F",   skills: "Passe,Régénération,Prise Sûre,Crâne Épais",                             motsCles: "Lanceur,Humain,Squelette,Mort-Vivant"),
                Pos("Blitzer Squelette",                2, 85_000,  6, 3, "4+", "5+", "9+",  "G,F", "A,S",     skills: "Blocage,Régénération,Crâne Épais",                                      motsCles: "Blitzer,Humain,Squelette,Mort-Vivant"),
                Pos("Gardien des Tombes",              4, 115_000, 4, 5, "5+", "6+", "10+", "F",   "G,A",     skills: "Bagarreur,Décomposition,Régénération",                   motsCles: "Bloqueur,Humain,Gros Bras,Mort-Vivant"),
            ],
            []
        );

        // === 27. Skavens ===
        // Ligue : Défi des Bas-Fonds — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Skavens",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "UnderworldChallenge",
                ReglesSpeciales = "Rats rapides et agiles. Coureurs d'Égouts très mobiles et Rat Ogre disponible."
            },
            [
                Pos("Rat des Clans",                  16, 50_000,  7, 3, "3+", "4+", "8+",  "G",   "S,A,M,F",                                                                                        motsCles: "Trois-quart,Skaven"),
                Pos("Lanceur Skaven",                   2, 80_000,  7, 3, "3+", "2+", "8+",  "G,P", "A,S,M,F", skills: "Passe,Prise Sûre",                                                            motsCles: "Lanceur,Skaven"),
                Pos("Coureur d'Égouts",                 2, 85_000,  9, 2, "2+", "4+", "8+",  "A,S,G", "F,M",   skills: "Esquive,Poignard",                                                            motsCles: "Coureur,Skaven"),
                Pos("Blitzer Skaven",                   2, 90_000,  8, 3, "3+", "4+", "9+",  "G,F", "A,M,S",   skills: "Blocage,Arracher le Ballon",                                                  motsCles: "Blitzer,Skaven"),
                Pos("Rat Ogre",               1, 150_000, 6, 5, "4+", "6+", "9+",  "F",   "A,G,M",   skills: "Sauvagerie Animale,Frénésie,Solitaire (4+),Châtaigne,Queue Préhensile", motsCles: "Gros Bras,Skaven"),
            ],
            []
        );

        // === 28. Snotlings ===
        // Ligue : Défi des Bas-Fonds — Relances : 70k — Apothicaire : Oui
        // Règles spéciales : Chantage & Corruption, Trois-quarts à Vil Prix, Déferlement
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Snotlings",
                CoutRelance = 70_000,
                ReglesSpecialesLigue = "UnderworldChallenge",
                ReglesSpeciales = "Chantage & Corruption, Trois-quarts à Vil Prix, Déferlement. Hordes de snotlings avec chariots et trolls."
            },
            [
                Pos("Trois-quart Snotling",           16, 15_000,  5, 1, "3+", "4+", "6+",  "A,S", "G",       skills: "Esquive,Poids Plume,Minus,Glissade Contrôlée,Microbe,Insignifiant",                         motsCles: "Trois-quart,Snotling"),
                Pos("R'bondisseur",                   2, 20_000,  6, 1, "3+", "4+", "6+",  "A,S", "G",       skills: "Esquive,Monté sur Ressort,Minus,Poids Plume,Glissade Contrôlée",                            motsCles: "Snotling,Spécial"),
                Pos("Échassier",                      2, 20_000,  6, 1, "3+", "4+", "6+",  "A,S", "G",       skills: "Esquive,Poids Plume,Minus,Glissade Contrôlée,Sprint",                                      motsCles: "Coureur,Snotling"),
                Pos("Lance-Champi",                   2, 30_000,  5, 1, "3+", "4+", "6+",  "A,P,S", "G",     skills: "Bombardier,Esquive,Arme Secrète,Minus,Glissade Contrôlée,Poids Plume,Microbe",             motsCles: "Snotling,Spécial"),
                Pos("Chariot à Pompe",                2, 100_000, 5, 5, "5+", "6+", "9+",  "S,F", "A,G",     skills: "Joueur Déloyal,Gros Débile,Juggernaut,Châtaigne,Stabilité",               motsCles: "Gros Bras,Spécial"),
                Pos("Troll Entraîné",                  2, 115_000, 4, 5, "5+", "5+", "10+", "F",   "A,G,P",   skills: "Toujours Affamé,Gros Débile,Châtaigne,Lancer de Coéquipier,Gerbe de Vomi,Régénération", motsCles: "Gros Bras,Troll"),
            ],
            []
        );

        // === 29. Union Elfique ===
        // Ligue : Ligue des Royaumes Elfiques — Relances : 50k — Apothicaire : Oui
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Union Elfique",
                CoutRelance = 50_000,
                ReglesSpecialesLigue = "ElvenKingdoms",
                ReglesSpeciales = "Union d'elfes polyvalents. Receveurs coûteux mais excellents."
            },
            [
                Pos("Trois-quart Elfe",               16, 65_000,  6, 3, "2+", "3+", "8+",  "G,A", "F",       skills: "Fumblerooski",                          motsCles: "Trois-quart,Elfe"),
                Pos("Lanceur Elfe",                    2, 75_000,  6, 3, "2+", "2+", "8+",  "G,A,P", "F",     skills: "Passe,Passe Désespérée",                motsCles: "Lanceur,Elfe"),
                Pos("Receveur Elfe",                   2, 100_000, 8, 3, "2+", "4+", "8+",  "G,A", "F",       skills: "Réception,Nerfs d'Acier,Réception Plongeante", motsCles: "Receveur,Elfe"),
                Pos("Blitzer Elfe",                    2, 115_000, 7, 3, "2+", "3+", "9+",  "G,A", "F,P",     skills: "Blocage,Glissade Contrôlée",            motsCles: "Blitzer,Elfe"),
            ],
            []
        );

        // === 30. Vampires ===
        // Ligue : Spot de Sylvanie — Relances : 60k — Apothicaire : Oui
        // Règles spéciales : Maîtres de la Non-Vie
        yield return new TeamSeed(
            new TeamType
            {
                GameId = bbGameId,
                RulesVersionId = bbVersionId,
                Nom = "Vampires",
                CoutRelance = 60_000,
                ReglesSpecialesLigue = "MastersOfUndeath",
                ReglesSpeciales = "Maîtres de la Non-Vie. Vampires puissants avec Soif de Sang sur les sbires."
            },
            [
                Pos("Trois-quart Sbire",              16, 40_000,  6, 3, "3+", "4+", "8+",  "G",   "A,F",                                                                                motsCles: "Trois-quart,Humain,Sbire"),
                Pos("Coureur Vampire",                  2, 100_000, 8, 3, "2+", "3+", "8+",  "A,G", "F,P",     skills: "Regard Hypnotique,Régénération,Soif de Sang (2+)",               motsCles: "Coureur,Mort-Vivant,Vampire"),
                Pos("Lanceur Vampire",                  2, 110_000, 6, 4, "2+", "2+", "9+",  "A,G,P", "F",     skills: "Passe,Regard Hypnotique,Régénération,Soif de Sang (2+)",         motsCles: "Lanceur,Mort-Vivant,Vampire"),
                Pos("Blitzer Vampire",                  2, 110_000, 6, 4, "2+", "4+", "9+",  "A,G,F", "-",     skills: "Juggernaut,Regard Hypnotique,Régénération,Soif de Sang (3+)",   motsCles: "Blitzer,Mort-Vivant,Vampire"),
                Pos("Vargheist",              1, 150_000, 5, 5, "4+", "6+", "10+", "F",   "A,G",     skills: "Frénésie,Griffes,Régénération,Soif de Sang (3+),Solitaire (4+)", motsCles: "Gros Bras,Mort-Vivant,Vampire"),
            ],
            []
        );
    }

    // ──────────────────────────────────────────────────────────────
    // Helper : construit un PlayerPosition depuis les paramètres
    // ──────────────────────────────────────────────────────────────
    private static PlayerPosition Pos(
        string nom, int qteMax, int cout,
        int mv, int force, string ag, string cp, string ar,
        string principal, string secondaire,
        string skills = "",
        string motsCles = "")
    {
        var p = new PlayerPosition
        {
            Nom = nom,
            QuantiteMax = qteMax,
            Cout = cout,
            Mouvement = mv,
            Force = force,
            Agilite = ag,
            CapacitePasse = cp,
            Armure = ar,
            CompetencesPrincipales = principal,
            CompetencesSecondaires = secondaire,
            MotsCles = motsCles,
        };
        p._StartingSkillsTemp = skills;
        return p;
    }
}
