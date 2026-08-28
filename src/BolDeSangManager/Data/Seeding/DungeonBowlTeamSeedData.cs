using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/dungeonbowl.md §2 (Edition 2022, 8 collèges de magie).
/// Toutes les équipes payent 50 000 po pour les Relances (règle uniforme DB).
/// Rosters marqués ⚠️ dans le markdown ont été recoupés avec l'ancien DbSeeder.cs (lignes 409-567).
/// </summary>
public static class DungeonBowlTeamSeedData
{
    public record TeamSeed(TeamType Type, List<PlayerPosition> Positions);

    public static IEnumerable<TeamSeed> GetColleges(int dbGameId, int dbVersionId)
    {
        // === 1. Collège des Cieux ===
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège des Cieux",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien Céleste disponible. Vitesse et équilibre."
            },
            [
                Pos("Skink Runner",            16, 60_000,  8, 2, "3+", "4+", "8+",  "AG",   "PF",   skills: "Esquive,Minus", motsCles: "Trois-quart,Homme-Lézard"),
                Pos("Norse Lineman",            16, 60_000,  6, 3, "3+", "4+", "8+",  "GA",   "F",    skills: "Blocage,Poivrot,Crâne Épais", motsCles: "Trois-quart,Humain"),
                Pos("Eagle Warrior",            16, 50_000,  6, 3, "3+", "4+", "8+",  "GA",   "F",    skills: "Esquive", motsCles: "Trois-quart,Humaine"),
                Pos("Noble Blitzer",             4, 105_000, 7, 3, "3+", "4+", "9+",  "AG",   "PF",   skills: "Blocage,Réception",  motsCles: "Blitzer,Humain"),
                Pos("Norse Berzerker",           4, 90_000,  6, 3, "3+", "5+", "8+",  "GFA",  "P",    skills: "Blocage,Frénésie,Rétablissement",  motsCles: "Blitzer,Humain"),
                Pos("Piranaha Warrior",          4, 90_000,  7, 3, "3+", "5+", "8+",  "GAF",  "P",    skills: "Esquive,Frappe et Cours,Rétablissement",  motsCles: "Blitzer,Humaine"),
                Pos("Lanceur Humain",            2, 80_000,  6, 3, "3+", "2+", "9+",  "GPAF", "",     skills: "Passe,Prise Sûre",  motsCles: "Lanceur,Humain"),
                Pos("Python Warrior",            2, 75_000,  6, 3, "3+", "3+", "8+",  "GA",   "F",    skills: "Esquive,Sur le Ballon,Passe,Passe Assurée",  motsCles: "Lanceur,Humaine"),
                Pos("Saurus",                    6, 85_000,  6, 4, "5+", "6+", "10+", "GF",   "A",  motsCles: "Bloqueur,Homme-Lézard"),
                Pos("Jaguar Warrior",            6, 110_000, 6, 4, "3+", "5+", "9+",  "GFA",  "",     skills: "Défenseur,Esquive",  motsCles: "Bloqueur,Humaine"),
                Pos("Chameleon Skink",           2, 70_000,  7, 2, "3+", "3+", "8+",  "A",    "GPF",  skills: "Esquive,Sur le Ballon,Poursuite,Minus",  motsCles: "Lanceur,Homme-Lézard"),
                Pos("Valkyrie",                  2, 95_000,  7, 3, "3+", "3+", "8+",  "AGP",  "F",    skills: "Réception,Intrépide,Passe,Arracher le Ballon",  motsCles: "Receveur,Lanceur,Humain"),
            ]
        );

        // === 2. Collège du Feu ===
        // ⚠️ Roster mal aligné dans le brut — recoupé avec DbSeeder.cs.
        // Bloodborn Marauder : compétence Frénésie retenue (DbSeeder). Ogre Runt Punter coût : 145 000 (estimation DbSeeder).
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège du Feu",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien Brillant (boule de feu). Résistance et blocage."
            },
            [
                Pos("Dwarf Blocker",                 16, 70_000,  4, 3, "4+", "5+", "10+", "GF",   "A",    skills: "Blocage,Tacle,Crâne Épais", motsCles: "Trois-quart,Nain"),
                Pos("Gnoblar",                       16, 15_000,  5, 1, "3+", "5+", "6+",  "A",    "G",    skills: "Esquive,Poids Plume,Glissade Contrôlée,Minus,Microbe", motsCles: "Trois-quart,Gnoblar"),
                Pos("Bloodborn Marauder",            16, 50_000,  6, 3, "4+", "5+", "8+",  "GM",   "AF",   skills: "Frénésie", motsCles: "Trois-quart,Humain"),
                Pos("Chaos Dwarf Blocker",            4, 80_000,  5, 3, "4+", "6+", "10+", "GF",   "AP",   skills: "Blocage,Crâne Épais,Peau de Fer",  motsCles: "Bloqueur,Nain"),
                Pos("Dwarf Runner",                   2, 80_000,  5, 3, "3+", "4+", "9+",  "PF",   "AG",   skills: "Prise Sûre,Crâne Épais",  motsCles: "Coureur,Nain"),
                Pos("Dwarf Blitzer",                  2, 95_000,  5, 3, "3+", "4+", "10+", "GF",   "AP",   skills: "Blocage,Crâne Épais",  motsCles: "Blitzer,Nain"),
                Pos("Troll Slayer",                   2, 85_000,  5, 3, "3+", "5+", "9+",  "GF",   "A",    skills: "Blocage,Intrépide,Frénésie,Crâne Épais",  motsCles: "Nain,Spécial"),
                Pos("Breath Fire",                    3, 140_000, 5, 5, "4+", "5+", "10+", "F",    "AGP",  skills: "Bagarreur,Crachat de Feu,Crâne Épais,Présence Perturbante", motsCles: "Gros Bras,Nain,Spécial"),
                Pos("Ogre Blocker",                   3, 145_000, 5, 5, "4+", "4+", "10+", "F",    "AGP",  skills: "Cerveau Lent,Châtaigne,Crâne Épais,Lancer de Coéquipier", motsCles: "Gros Bras,Ogre"),
                Pos("Ogre Runt Punter",               3, 145_000, 5, 5, "4+", "6+", "10+", "F",    "AGP",  skills: "Cerveau Lent,Châtaigne,Crâne Épais,Botter de Coéquipier", motsCles: "Lanceur,Gros Bras,Ogre"),
            ]
        );

        // === 3. Collège de l'Ombre ===
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège de l'Ombre",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien Gris. Assassins et vitesse dans les ombres."
            },
            [
                Pos("Dark Elf Lineman",               16, 70_000,  6, 3, "2+", "4+", "9+",  "AG",   "F", motsCles: "Trois-quart,Elfe"),
                Pos("Skaven Lineman",                16, 50_000,  7, 3, "3+", "4+", "8+",  "G",    "AMF", motsCles: "Trois-quart,Skaven"),
                Pos("Goblin Lineman",                16, 40_000,  6, 2, "3+", "4+", "8+",  "A",    "GPF",  skills: "Esquive,Poids Plume,Minus", motsCles: "Trois-quart,Gobelin"),
                Pos("Gnome Lineman",                 16, 40_000,  5, 2, "3+", "4+", "7+",  "A",    "FG",   skills: "Lutte,Minus,Poids Plume,Rétablissement", motsCles: "Trois-quart,Gnome"),
                Pos("Hobgoblin Lineman",             16, 40_000,  6, 3, "3+", "4+", "8+",  "G",    "AF", motsCles: "Trois-quart,Gobelin"),
                Pos("Dark Elf Runner",                4, 80_000,  7, 3, "2+", "3+", "8+",  "AGP",  "F",    skills: "Délestage",  motsCles: "Coureur,Elfe"),
                Pos("Gutter Runner",                  4, 85_000,  9, 2, "2+", "4+", "8+",  "AG",   "",     skills: "Esquive",  motsCles: "Coureur,Skaven"),
                Pos("Woodland Fox Runner",            4, 50_000,  7, 2, "2+", "-",  "6+",  "AG",   "MPF",  skills: "Mon Ballon,Esquive,Glissade Contrôlée,Minus",  motsCles: "Coureur,Animal"),
                Pos("Dark Elf Blitzer",               2, 100_000, 7, 3, "2+", "4+", "9+",  "AG",   "PF",   skills: "Blocage",  motsCles: "Blitzer,Elfe"),
                Pos("Skaven Blitzer",                 2, 90_000,  7, 3, "3+", "5+", "9+",  "GF",   "AMP",  skills: "Blocage",  motsCles: "Blitzer,Skaven"),
                Pos("Skaven Thrower",                 2, 85_000,  7, 3, "3+", "2+", "8+",  "GP",   "AMF",  skills: "Passe,Prise Sûre",  motsCles: "Lanceur,Skaven"),
                Pos("Gnome Illusionist Thrower",      2, 50_000,  5, 2, "3+", "3+", "7+",  "AP",   "G",    skills: "Lutte,Minus,Rétablissement,Farceur",  motsCles: "Gnome,Spécial"),
                Pos("Witch Elf",                      2, 110_000, 7, 3, "2+", "5+", "8+",  "AG",   "PF",   skills: "Esquive,Frénésie,Rétablissement",  motsCles: "Elfe,Spécial"),
                Pos("Assassin",                       2, 85_000,  7, 3, "2+", "5+", "8+",  "AG",   "PF",   skills: "Poursuite,Poignard",  motsCles: "Elfe,Spécial"),
                Pos("Gnome Beastmaster",              2, 55_000,  5, 2, "3+", "4+", "8+",  "A",    "FG",   skills: "Garde,Lutte,Minus,Rétablissement",  motsCles: "Bloqueur,Gnome"),
                Pos("Hobgoblin Sneaky Stabba",        2, 70_000,  6, 3, "3+", "5+", "8+",  "G",    "AF",   skills: "Poignard,Poursuite",  motsCles: "Gobelin,Spécial"),
            ]
        );

        // === 4. Collège de la Lumière ===
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège de la Lumière",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien de Lumière. Elfes rapides et excellents lanceurs."
            },
            [
                Pos("Elven Union Lineman",             16, 60_000,  6, 3, "2+", "4+", "8+",  "AG",   "F", motsCles: "Trois-quart,Elfe"),
                Pos("Imperial Retainer",               16, 45_000,  6, 3, "4+", "4+", "8+",  "G",    "AF",   skills: "Parade", motsCles: "Trois-quart,Humain"),
                Pos("Elven Union Catcher",              4, 100_000, 8, 3, "2+", "4+", "8+",  "AG",   "F",    skills: "Réception,Nerfs d'Acier",  motsCles: "Receveur,Elfe"),
                Pos("Human Catcher",                    4, 65_000,  8, 2, "3+", "5+", "8+",  "AG",   "FP",   skills: "Réception,Esquive",  motsCles: "Receveur,Humain"),
                Pos("Elven Union Blitzer",              2, 115_000, 7, 3, "2+", "3+", "9+",  "AG",   "PF",   skills: "Blocage,Glissade Contrôlée",  motsCles: "Blitzer,Elfe"),
                Pos("Elven Union Thrower",              4, 75_000,  6, 3, "2+", "2+", "8+",  "AGP",  "F",    skills: "Passe",  motsCles: "Lanceur,Elfe"),
                Pos("Imperial Thrower",                 4, 75_000,  6, 3, "3+", "3+", "9+",  "GP",   "AF",   skills: "Passe,Passe dans la Course",  motsCles: "Lanceur,Humain"),
            ]
        );

        // === 5. Collège de la Vie ===
        // ⚠️ Tableau brut très mal aligné — recoupé avec DbSeeder.cs.
        // Halfling Hefty Bloater est listé comme Blitzer dans le markdown mais comme Défenseur dans DbSeeder — retenu : Blitzeur (markdown).
        // Stilty Runna présent dans markdown (20k, 0-4) non présent dans DbSeeder — inclus.
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège de la Vie",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien de Jade. Mélange nature, halflings et mort-vivants pestilents."
            },
            [
                Pos("Halfling Hopeful",             16, 30_000,  5, 2, "3+", "4+", "7+",  "A",    "GF",   skills: "Esquive,Poids Plume,Minus", motsCles: "Trois-quart,Halfling"),
                Pos("Rotter",                       16, 35_000,  5, 3, "4+", "6+", "9+",  "GM",   "AF",   skills: "Décomposition,Contagieux", motsCles: "Trois-quart,Humain"),
                Pos("Snotling",                     16, 15_000,  5, 1, "3+", "5+", "6+",  "A",    "G",    skills: "Esquive,Poids Plume,Minus,Glissade Contrôlée,Microbe", motsCles: "Trois-quart,Snotling"),
                Pos("Wood Elf Lineman",             16, 70_000,  7, 3, "2+", "4+", "8+",  "AG",   "F", motsCles: "Trois-quart,Elfe"),
                Pos("Stilty Runna",                  4, 20_000,  6, 1, "3+", "5+", "6+",  "A",    "G",    skills: "Esquive,Poids Plume,Minus,Glissade Contrôlée,Sprint",  motsCles: "Coureur,Snotling"),
                Pos("Halfling Catcher",              4, 55_000,  5, 2, "3+", "5+", "7+",  "A",    "GF",   skills: "Réception,Esquive,Poids Plume,Minus,Sprint",  motsCles: "Receveur,Halfling"),
                Pos("Wood Elf Catcher",              4, 90_000,  8, 2, "2+", "4+", "8+",  "AG",   "F",    skills: "Réception,Esquive",  motsCles: "Receveur,Elfe"),
                Pos("Halfling Hefty Bloater",        2, 125_000, 8, 3, "2+", "4+", "8+",  "AG",   "PF",   skills: "Blocage,Esquive,Saut",  motsCles: "Blitzer,Halfling"),
                Pos("Fungus Flinga",                 2, 95_000,  7, 3, "2+", "2+", "8+",  "AGP",  "F",    skills: "Passe",  motsCles: "Lanceur,Snotling"),
                Pos("Fun-Hoppa",                     4, 50_000,  5, 2, "3+", "3+", "8+",  "AP",   "GF",   skills: "Esquive,Parade,Minus",  motsCles: "Bloqueur,Halfling"),
                Pos("Treeman",                       2, 120_000, 2, 6, "5+", "5+", "11+", "F",    "AGP",  skills: "Châtaigne,Stabilité,Bras Musclé,Prendre Racine,Crâne Épais,Lancer de Coéquipier,Timmm-ber", motsCles: "Gros Bras,Homme-arbre"),
                Pos("Trained Troll",                 2, 115_000, 4, 5, "5+", "5+", "10+", "F",    "AGP",  skills: "Toujours Affamé,Solitaire (3+),Châtaigne,Gerbe de Vomi,Gros Débile,Régénération,Lancer de Coéquipier", motsCles: "Gros Bras,Troll"),
                Pos("Rotspawn",                      2, 140_000, 4, 5, "5+", "-",  "10+", "F",    "AGP",  skills: "Présence Perturbante,Répulsion,Solitaire (4+),Châtaigne,Contagieux,Gros Débile,Régénération,Tentacules", motsCles: "Gros Bras,Rejeton"),
            ]
        );

        // === 6. Collège du Métal ===
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège du Métal",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien d'Or. Orques et Humains robustes."
            },
            [
                Pos("Goblin Bruiser",               16, 45_000,  6, 2, "3+", "4+", "8+",  "A",    "GPF",  skills: "Esquive,Poids Plume,Minus,Crâne Épais", motsCles: "Trois-quart,Gobelin"),
                Pos("Orc Lineman",                  16, 50_000,  5, 3, "3+", "4+", "10+", "G",    "AF", motsCles: "Trois-quart,Orque"),
                Pos("Human Lineman",                16, 50_000,  6, 3, "3+", "4+", "9+",  "G",    "AF", motsCles: "Trois-quart,Humain"),
                Pos("Orc Blitzer",                   4, 80_000,  6, 3, "3+", "4+", "10+", "GF",   "AP",   skills: "Blocage",  motsCles: "Blitzer,Orque"),
                Pos("Human Blitzer",                 4, 85_000,  7, 3, "3+", "4+", "9+",  "GF",   "AP",   skills: "Blocage",  motsCles: "Blitzer,Humain"),
                Pos("Orc Thrower",                   2, 65_000,  5, 3, "3+", "3+", "9+",  "GP",   "AF",   skills: "Passe,Prise Sûre",  motsCles: "Lanceur,Orque"),
                Pos("Black Orc Blocker",             6, 90_000,  4, 4, "4+", "5+", "10+", "GF",   "AP",   skills: "Bagarreur,Projection",  motsCles: "Bloqueur,Orque Noir"),
                Pos("Big Un Blocker",                6, 90_000,  5, 4, "4+", "-",  "10+", "GF",   "A", motsCles: "Bloqueur,Orque"),
                Pos("Bodyguard Blocker",             6, 90_000,  6, 3, "3+", "5+", "9+",  "GF",   "A",    skills: "Stabilité,Lutte",  motsCles: "Bloqueur,Humain"),
                Pos("Bloodseeker Blocker",           6, 110_000, 5, 4, "4+", "6+", "10+", "GMF",  "A",    skills: "Frénésie",  motsCles: "Bloqueur,Humain"),
            ]
        );

        // === 7. Collège de la Mort ===
        // ⚠️ Tableau mal aligné — recoupé avec DbSeeder.cs.
        // Vampire Blitzer : pas de compétence secondaire (note compilateur + DbSeeder) → secondaire = "".
        // Wraith Blitzer : markdown a Blocage,Répulsion,Sans les Mains,Régénération,Glissade Contrôlée.
        //   DbSeeder a Blocage,Stabilité,Glissade Contrôlée,Régénération → markdown retenu (plus complet).
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège de la Mort",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien d'Améthyste. Vampires et morts-vivants. Régénération."
            },
            [
                Pos("Skeleton",                16, 40_000,  5, 3, "4+", "6+", "8+",  "G",    "AF",   skills: "Régénération,Crâne Épais", motsCles: "Trois-quart,Humain,Squelette,Mort-Vivant"),
                Pos("Zombie",                  16, 40_000,  4, 3, "4+", "-",  "9+",  "G",    "AF",   skills: "Régénération", motsCles: "Trois-quart,Humain,Zombie,Mort-Vivant"),
                Pos("Thrall",                  16, 40_000,  6, 3, "3+", "4+", "8+",  "G",    "AF", motsCles: "Trois-quart,Humain,Sbire"),
                Pos("Ghoul Runner",             4, 75_000,  7, 3, "3+", "4+", "8+",  "AG",   "PF",   skills: "Esquive",  motsCles: "Coureur,Goule,Mort-Vivant"),
                Pos("Vampire Runner",           4, 100_000, 8, 3, "2+", "4+", "8+",  "AG",   "FP",   skills: "Regard Hypnotique,Régénération,Soif de Sang (2+)",  motsCles: "Coureur,Mort-Vivant,Vampire"),
                Pos("Vampire Thrower",          2, 110_000, 6, 4, "2+", "2+", "9+",  "AGP",  "F",    skills: "Passe,Regard Hypnotique,Régénération,Soif de Sang (2+)",  motsCles: "Lanceur,Mort-Vivant,Vampire"),
                Pos("Wraith Blitzer",           4, 95_000,  6, 3, "3+", "-",  "9+",  "GF",   "A",    skills: "Blocage,Répulsion,Sans les Mains,Régénération,Glissade Contrôlée",  motsCles: "Bloqueur,Spectre,Mort-Vivant"),
                Pos("Wight Blitzer",            4, 90_000,  6, 3, "3+", "5+", "9+",  "GF",   "AP",   skills: "Blocage,Régénération",  motsCles: "Blitzer,Humain,Squelette,Mort-Vivant"),
                Pos("Vampire Blitzer",          4, 110_000, 6, 4, "2+", "5+", "9+",  "AGS",  "",     skills: "Juggernaut,Regard Hypnotique,Régénération,Soif de Sang (3+)",  motsCles: "Blitzer,Mort-Vivant,Vampire"),
                Pos("Flesh Golem",              4, 115_000, 4, 4, "4+", "-",  "10+", "GF",   "A",    skills: "Régénération,Stabilité,Crâne Épais",  motsCles: "Bloqueur,Artefact,Mort-Vivant"),
                Pos("Mummy",                    2, 125_000, 3, 5, "5+", "-",  "10+", "F",    "AG",   skills: "Châtaigne,Régénération",  motsCles: "Humain,Mort-Vivant,Bloqueur,Gros Bras"),
                Pos("Vargheist",                2, 150_000, 5, 5, "4+", "-",  "10+", "F",    "AG",   skills: "Frénésie,Griffes,Régénération,Soif de Sang (3+),Solitaire (4+)", motsCles: "Gros Bras,Mort-Vivant,Vampire"),
            ]
        );

        // === 8. Collège des Bêtes ===
        // ⚠️ Tableau mal aligné — recoupé avec DbSeeder.cs.
        // Beer Boar : pas de catégorie principal/secondaire (–/–) confirmé dans les deux sources.
        // Bloodspawn présent dans markdown mais pas DbSeeder → inclus (markdown fait autorité).
        // Yethee présent dans markdown mais pas DbSeeder → inclus.
        yield return new TeamSeed(
            new TeamType
            {
                GameId = dbGameId,
                RulesVersionId = dbVersionId,
                Nom = "Collège des Bêtes",
                CoutRelance = 50_000,
                ReglesSpeciales = "Magicien d'Ambre. Créatures du Chaos et mutants."
            },
            [
                Pos("Beastman Runner",          16, 60_000,  6, 3, "3+", "4+", "9+",  "GMF",  "AP",   skills: "Cornes", motsCles: "Trois-quart,Homme-Bête"),
                Pos("Werewolf Runner",           2, 125_000, 8, 3, "3+", "4+", "9+",  "AG",   "PF",   skills: "Griffes,Frénésie,Régénération",  motsCles: "Coureur,Loup-garou,Mort-Vivant"),
                Pos("Pestigor Blitzer",          4, 75_000,  6, 3, "3+", "4+", "9+",  "GMF",  "AP",   skills: "Cornes,Contagieux,Régénération",  motsCles: "Blitzer,Homme-Bête"),
                Pos("Khorngor Blitzer",          4, 70_000,  6, 3, "3+", "4+", "9+",  "GMF",  "AP",   skills: "Cornes,Juggernaut",  motsCles: "Blitzer,Homme-Bête"),
                Pos("Bull Centaur",              4, 130_000, 6, 4, "4+", "6+", "10+", "FG",   "AM",   skills: "Crâne Épais,Équilibre,Sprint",  motsCles: "Blitzer,Nain"),
                Pos("Chaos Chosen",              4, 100_000, 5, 4, "3+", "5+", "10+", "GMF",  "A",  motsCles: "Bloqueur,Humain"),
                Pos("Ulfwerener",                4, 105_000, 6, 4, "4+", "-",  "9+",  "GF",   "A",    skills: "Frénésie",  motsCles: "Bloqueur,Humain"),
                Pos("Beer Boar",                 2, 20_000,  5, 1, "3+", "-",  "6+",  "",     "",     skills: "Esquive,Sans les Mains,Minus,Microbe,Choppe-moi",  motsCles: "Animal,Spécial"),
                Pos("Minotaur",                  3, 150_000, 5, 5, "4+", "-",  "9+",  "MF",   "AG",   skills: "Solitaire (4+),Frénésie,Cornes,Châtaigne,Crâne Épais,Furie Débridée", motsCles: "Gros Bras,Minotaure"),
                Pos("Kroxigor",                  3, 140_000, 6, 5, "5+", "-",  "9+",  "F",    "AGM",  skills: "Cerveau Lent,Solitaire (4+),Châtaigne,Crâne Épais,Queue Préhensile", motsCles: "Gros Bras,Homme-Lézard"),
                Pos("Rat Ogre",                  3, 150_000, 6, 5, "4+", "-",  "9+",  "MF",   "AG",   skills: "Sauvagerie Animale,Frénésie,Solitaire (4+),Châtaigne,Queue Préhensile", motsCles: "Gros Bras,Skaven"),
                Pos("Bloodspawn",                3, 160_000, 5, 5, "4+", "-",  "10+", "F",    "AG",   skills: "Griffes,Solitaire (4+),Frénésie,Châtaigne,Furie Débridée", motsCles: "Gros Bras,Rejeton"),
                Pos("Yethee",                    3, 140_000, 5, 5, "4+", "-",  "9+",  "F",    "AG",   skills: "Griffes,Présence Perturbante,Solitaire (4+),Frénésie,Furie Débridée", motsCles: "Gros Bras,Yéti"),
            ]
        );
    }

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
