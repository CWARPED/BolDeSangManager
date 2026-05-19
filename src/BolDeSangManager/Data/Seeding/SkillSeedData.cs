using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;

namespace BolDeSangManager.Data.Seeding;

/// <summary>
/// Source : docs/regles/bloodbowl.md §9 (LRB S3) et docs/regles/dungeonbowl.md §5.4 (DB-only).
/// Catégories alignées sur les données du DbSeeder existant pour garantir la compatibilité
/// avec SeedPositionSkillsAsync (recherche par Nom, insensible à la casse).
/// </summary>
public static class SkillSeedData
{
    public static IEnumerable<Skill> GetSkills(int versionId, GameType game)
    {
        // ═══════════════════ AGILITÉ (A) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Esquive",               Categorie = SkillCategory.Agilite, Description = "Ce joueur peut relancer un jet d'Esquive raté une fois par activation.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Balle Collante",        Categorie = SkillCategory.Agilite, Description = "Si ce joueur tombe et porte le ballon, testez son Agilité — s'il réussit, il ne lâche pas le ballon." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Bondissant",            Categorie = SkillCategory.Agilite, Description = "Ce joueur peut Bondir par-dessus les joueurs Debout adverses.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Crampon",               Categorie = SkillCategory.Agilite, Description = "Ce joueur ne peut pas Foncer sur un résultat de 1 ou 2 (au lieu de seulement 1)." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Défieur",               Categorie = SkillCategory.Agilite, Description = "Quand ce joueur est la cible d'un Blocage, il peut choisir d'annuler le résultat et forcer un nouveau jet." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Délestage",             Categorie = SkillCategory.Agilite, Description = "Ce joueur peut passer le ballon à un coéquipier adjacent comme action gratuite quand il tombe." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Filou",                 Categorie = SkillCategory.Agilite, Description = "Ce joueur peut tenter de ramasser le ballon même dans la Zone de Tacle d'un adversaire sans modificateur." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Jongleur",              Categorie = SkillCategory.Agilite, Description = "Ce joueur peut tenter de Réceptionner plusieurs passes dans le même tour." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Pas Chassé",            Categorie = SkillCategory.Agilite, Description = "Ce joueur peut se déplacer dans la case qu'un adversaire vient de quitter." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Prise Sûre",            Categorie = SkillCategory.Agilite, Description = "Ce joueur peut toujours relancer un jet de Ramassage ou de Réception raté." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Protection du Ballon",  Categorie = SkillCategory.Agilite, Description = "Quand ce joueur porte le ballon et est Poussé, il ne lâche pas le ballon." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Réflexes",              Categorie = SkillCategory.Agilite, Description = "Ce joueur peut tenter d'annuler un résultat de dé de Blocage en effectuant un jet d'Agilité." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Saut",                  Categorie = SkillCategory.Agilite, Description = "Ce joueur peut Bondir par-dessus les joueurs À Terre ou Sonnés au lieu de les contourner." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Tacle Plongeant",       Categorie = SkillCategory.Agilite, Description = "Ce joueur peut effectuer un Tacle en se plaçant À Terre." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Réception Plongeante",  Categorie = SkillCategory.Agilite, Description = "Ce joueur peut tenter de Réceptionner une passe à 1 case en se plaçant À Terre." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Équilibre",             Categorie = SkillCategory.Agilite, Description = "Ce joueur peut tenter un jet d'Agilité pour rester debout après un résultat Repousse contre soi." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Glissade Contrôlée",   Categorie = SkillCategory.Agilite, Description = "Quand ce joueur tombe, il n'est pas Sonné mais reste À Terre." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Sprint",                Categorie = SkillCategory.Agilite, Description = "Ce joueur peut Foncer 3 fois par activation (au lieu de 2)." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Balle-Bombe",           Categorie = SkillCategory.Agilite, Description = "Ce joueur peut lancer une balle-bombe au lieu du ballon.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Poids Plume",           Categorie = SkillCategory.Agilite, Description = "Ce joueur peut être Lancé par un coéquipier avec Lancer de Coéquipier.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Minus",                 Categorie = SkillCategory.Agilite, Description = "Ce joueur souffre de -1 sur ses jets d'AR.", EstTrait = true };

        // ═══════════════════ FORCE (F) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Bagarreur",             Categorie = SkillCategory.Force, Description = "Ce joueur peut relancer un dé de Blocage qui ne lui convient pas lors d'un Blocage." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Bras Musclé",           Categorie = SkillCategory.Force, Description = "Augmente la portée du Lancer de Coéquipier." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Clé de Bras",           Categorie = SkillCategory.Force, Description = "Sur résultat Repousse subi, ce joueur peut forcer l'adversaire à rester en place." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Crâne Épais",           Categorie = SkillCategory.Force, Description = "Ce joueur peut relancer son premier jet d'AR par match." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Esquive en Force",      Categorie = SkillCategory.Force, Description = "Quand cible d'un Blocage, ce joueur peut tenter de Repousser l'attaquant." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Garde",                 Categorie = SkillCategory.Force, Description = "Ce joueur fournit du soutien offensif lors d'un Blocage même s'il est lui-même Marqué.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Gros Bras",             Categorie = SkillCategory.Force, Description = "Ce joueur peut Lancer un coéquipier avec Poids Plume.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Intrépide",             Categorie = SkillCategory.Force, Description = "Ce joueur ne souffre pas des malus de Force contre des adversaires plus forts." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Lancer de Coéquipier",  Categorie = SkillCategory.Force, Description = "Ce joueur peut Lancer un coéquipier avec Poids Plume comme Action de Passe." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Marteau-pilon",         Categorie = SkillCategory.Force, Description = "Ce joueur inflige +1 sur le jet d'AR quand il Plaque un adversaire." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Blocage Multiple",      Categorie = SkillCategory.Force, Description = "Ce joueur peut Bloquer deux adversaires adjacents simultanément." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Peau de Fer",           Categorie = SkillCategory.Force, Description = "Ce joueur bénéficie de +1 sur ses jets d'AR." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Stabilité",             Categorie = SkillCategory.Force, Description = "Ce joueur ne peut pas être Repoussé par un Blocage." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Châtaigne",             Categorie = SkillCategory.Force, Description = "Quand ce joueur frappe un adversaire, celui-ci souffre d'un modificateur supplémentaire sur son jet d'AR.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Cerveau Lent",          Categorie = SkillCategory.Force, Description = "Ce joueur doit réussir un jet pour Foncer. Sur un échec, il chute.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Gros Débile",           Categorie = SkillCategory.Force, Description = "Ce joueur doit réussir un jet pour activer son Trait à chaque tour.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Prendre Racine",        Categorie = SkillCategory.Force, Description = "Ce joueur peut décider de rester sur place : il ne peut pas se déplacer mais il est plus difficile à pousser.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Accablant",             Categorie = SkillCategory.Force, Description = "Ce joueur frappe un adversaire avec +1 sur le jet de Blessure.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Massacre",              Categorie = SkillCategory.Force, Description = "Si ce joueur inflige une Blessure, il gagne +1 PSP supplémentaire." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Projection",            Categorie = SkillCategory.Force, Description = "Ce joueur peut projeter un joueur adverse dans un espace adjacent lors d'un Blocage." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Juggernaut",            Categorie = SkillCategory.Force, Description = "Lors d'un Blitz, ce joueur peut traiter un résultat Plaqu' adverse comme Repousse." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Frénésie",              Categorie = SkillCategory.Force, Description = "Ce joueur doit continuer à Bloquer/Blitzer le même adversaire après l'avoir Repoussé." };

        // ═══════════════════ GÉNÉRALE (G) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Arracher le Ballon",    Categorie = SkillCategory.Generale, Description = "Ce joueur peut tenter d'arracher le ballon à un joueur adverse qui le porte." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Blocage",               Categorie = SkillCategory.Generale, Description = "Quand ce joueur effectue un Blocage ou est la cible d'un Blocage, les dés de Blocage 'Plaqu'' comptent comme 'Repousse'.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Chef",                  Categorie = SkillCategory.Generale, Description = "Une fois par tour d'équipe, ce joueur peut utiliser une Relance d'équipe sans que l'équipe en dépense une." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Défenseur",             Categorie = SkillCategory.Generale, Description = "Ce joueur peut fournir du soutien défensif même s'il est Marqué." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Frappe et Cours",       Categorie = SkillCategory.Generale, Description = "Ce joueur peut Bloquer puis se déplacer dans la même activation." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Lutte",                 Categorie = SkillCategory.Generale, Description = "Quand ce joueur effectue une Action de Blocage, il peut forcer les deux joueurs à tomber quel que soit le résultat." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Nerfs d'Acier",         Categorie = SkillCategory.Generale, Description = "Ce joueur peut tenter de Réceptionner même dans 2 cases d'un joueur adverse Debout." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Parade",                Categorie = SkillCategory.Generale, Description = "Ce joueur bénéficie d'un bonus défensif contre les Blocages adverses." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Pro",                   Categorie = SkillCategory.Generale, Description = "Une fois par tour, ce joueur peut relancer un dé sur 3+ sans utiliser de Relance d'équipe." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Réception",             Categorie = SkillCategory.Generale, Description = "Ce joueur peut ignorer tous les modificateurs négatifs sur les jets de Réception." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Rétablissement",        Categorie = SkillCategory.Generale, Description = "Ce joueur peut se relever sans utiliser 3 cases de Mouvement." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Solidarité",            Categorie = SkillCategory.Generale, Description = "Ce joueur peut partager une Relance avec ses coéquipiers dans les 3 cases." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Sur le Ballon",         Categorie = SkillCategory.Generale, Description = "Ce joueur peut tenter un Sécurisation du Ballon dans la Zone de Tacle d'un adversaire." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Tacle",                 Categorie = SkillCategory.Generale, Description = "Les joueurs adverses dans la Zone de Tacle de ce joueur ne peuvent pas utiliser Esquive." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Choc",                  Categorie = SkillCategory.Generale, Description = "Ce joueur peut se déplacer en dehors de sa Zone de Tacle sans jet d'Esquive si l'adversaire est ensuite repoussé d'au moins une case." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Courageux",             Categorie = SkillCategory.Generale, Description = "Ce joueur n'est pas affecté par les Zones de Tacle des adversaires plus forts que lui." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Désengagement",         Categorie = SkillCategory.Generale, Description = "Ce joueur peut se déplacer d'une case sans effectuer de jet d'Esquive, même s'il quitte une Zone de Tacle." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Filer",                 Categorie = SkillCategory.Generale, Description = "Ce joueur ne génère pas de Zone de Tacle." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Interception",          Categorie = SkillCategory.Generale, Description = "Ce joueur peut tenter d'intercepter une passe en se déplaçant d'une case avant le jet." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Plaquage",              Categorie = SkillCategory.Generale, Description = "Les joueurs adverses dans la Zone de Tacle de ce joueur ne peuvent pas utiliser Esquive ou Glissade Contrôlée." };

        // ═══════════════════ PASSE (P) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Passe",                 Categorie = SkillCategory.Passe, Description = "Ce joueur peut relancer son premier jet de Passe raté par match." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Passe Assurée",         Categorie = SkillCategory.Passe, Description = "Ce joueur peut toujours relancer un jet de Passe Courte ou Rapide raté." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Passe dans la Course",  Categorie = SkillCategory.Passe, Description = "Ce joueur peut effectuer une Action de Passe pendant une Action de Blitz." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Passe Longue Portée",   Categorie = SkillCategory.Passe, Description = "Ce joueur augmente la portée de ses passes de 2 cases." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Portée",                Categorie = SkillCategory.Passe, Description = "Ce joueur bénéficie de +1 case de portée sur toutes ses passes." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Précision",             Categorie = SkillCategory.Passe, Description = "Ce joueur bénéficie d'un bonus sur les jets de précision de passe." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Fumblerooskie",         Categorie = SkillCategory.Passe, Description = "Ce joueur peut volontairement lâcher le ballon dans une case adjacente." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Botter de Coéquipier",  Categorie = SkillCategory.Passe, Description = "Ce joueur peut botter un coéquipier avec Poids Plume comme Action de Passe.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="En Avant",              Categorie = SkillCategory.Passe, Description = "Ce joueur peut passer en arrière sans malus." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Flèche",                Categorie = SkillCategory.Passe, Description = "Les passes de ce joueur ont leur distance réduite de 2 cases vers la Longue Portée." };
        // Spécifiques DungeonBowl
        if (game == GameType.DungeonBowl)
            yield return new Skill { RulesVersionId = versionId, Nom = "Transmission dans la Course", Categorie = SkillCategory.Passe, Description = "Ce joueur peut continuer son mouvement après une Transmission." };
        if (game == GameType.DungeonBowl)
            yield return new Skill { RulesVersionId = versionId, Nom = "Passe par un Portail", Categorie = SkillCategory.Passe, Description = "Ce joueur peut déclarer une Passe après avoir utilisé un portail." };
        if (game == GameType.DungeonBowl)
            yield return new Skill { RulesVersionId = versionId, Nom = "Lancer contre un Mur", Categorie = SkillCategory.Passe, Description = "+1 quand ce joueur vise un mur lors d'une Passe." };

        // ═══════════════════ MUTATION (M) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Bec Acéré",             Categorie = SkillCategory.Mutation, Description = "Ce joueur peut relancer un jet d'AR quand il inflige une Blessure.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Bras Supplémentaire",   Categorie = SkillCategory.Mutation, Description = "+1 sur jets de Ramassage, Réception et Interception." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Bras Tentaculaire",     Categorie = SkillCategory.Mutation, Description = "Ce joueur peut attraper n'importe quel objet en vol passant à 1 case." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Deux Têtes",            Categorie = SkillCategory.Mutation, Description = "Ce joueur bénéficie de +1 sur ses jets d'Esquive." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Doigts de Poulpe",      Categorie = SkillCategory.Mutation, Description = "Ce joueur bénéficie de +1 sur ses jets de Ramassage et de Réception." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Grande Gueule",         Categorie = SkillCategory.Mutation, Description = "Ce joueur peut Croquer un adversaire adjacent (l'immobilise).", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Griffes",               Categorie = SkillCategory.Mutation, Description = "Ce joueur ignore l'Armure des joueurs adverses sur un résultat de 8+ sur 2D6." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Cornes",                Categorie = SkillCategory.Mutation, Description = "Ce joueur bénéficie de +1 Force lors d'une Action de Blitz." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Jambes Fléchies",       Categorie = SkillCategory.Mutation, Description = "Ce joueur peut Bondir d'une case supplémentaire lors de chaque Bond." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Langue Visqueuse",      Categorie = SkillCategory.Mutation, Description = "Ce joueur peut utiliser sa langue pour attraper le ballon à 2 cases." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Main Démesurée",        Categorie = SkillCategory.Mutation, Description = "+1 sur jets de Ramassage et d'Interception." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Membres Supplémentaires", Categorie = SkillCategory.Mutation, Description = "Ce joueur bénéficie de +1 sur ses jets de Blocage (comme avoir la compétence Blocage)." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Présence Perturbante",  Categorie = SkillCategory.Mutation, Description = "Les jets de Passe dans les 3 cases de ce joueur souffrent d'un malus.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Queue Préhensile",      Categorie = SkillCategory.Mutation, Description = "Les adversaires dans la Zone de Tacle de ce joueur subissent -1 sur leurs jets d'Esquive." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Répulsion",             Categorie = SkillCategory.Mutation, Description = "Lors d'un Blocage, ce joueur peut Repousser sans contact." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Tentacules",            Categorie = SkillCategory.Mutation, Description = "Un adversaire tentant de quitter la Zone de Tacle de ce joueur doit réussir un jet d'Agilité ou reste Marqué." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Très Longues Jambes",   Categorie = SkillCategory.Mutation, Description = "+1 sur Bond et ce joueur peut Bondir par-dessus joueurs Debout." };
        // Traits mutation
        yield return new Skill { RulesVersionId = versionId, Nom ="Animosité",             Categorie = SkillCategory.Mutation, Description = "Ce joueur peut ne pas passer le ballon à certains coéquipiers.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Sauvagerie Animale",    Categorie = SkillCategory.Mutation, Description = "Ce joueur peut attaquer ses propres coéquipiers s'il n'est pas activé correctement.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Solitaire",             Categorie = SkillCategory.Mutation, Description = "Ce joueur doit réussir un jet pour utiliser une Relance d'équipe.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Soif de Sang",          Categorie = SkillCategory.Mutation, Description = "Ce joueur doit boire le sang d'un coéquipier adjacent en début de tour.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Regard Hypnotique",     Categorie = SkillCategory.Mutation, Description = "Ce joueur peut hypnotiser un adversaire adjacent, l'empêchant d'utiliser Esquive.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Toujours Affamé",       Categorie = SkillCategory.Mutation, Description = "Quand ce joueur Bloque, il peut accidentellement manger son coéquipier.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Régénération",          Categorie = SkillCategory.Mutation, Description = "Quand ce joueur est Éliminé, jetez un D6 : sur 4+, le joueur ne rate que le reste du match.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Décomposition",         Categorie = SkillCategory.Mutation, Description = "Ce joueur perd 1 en Armure à chaque match.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Contagieux",            Categorie = SkillCategory.Mutation, Description = "Les joueurs Éliminés par ce joueur peuvent contracter une maladie.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Microbe",               Categorie = SkillCategory.Mutation, Description = "Ce joueur est si petit qu'il fournit du soutien offensif sans être Marqué.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Poivrot",               Categorie = SkillCategory.Mutation, Description = "Ce joueur commence aléatoirement chaque match ivre ou sobre.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Furie Débridée",        Categorie = SkillCategory.Mutation, Description = "Ce joueur bénéficie de +1 dé lors d'un Blitz.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Timmm-ber",             Categorie = SkillCategory.Mutation, Description = "Ce joueur peut tomber intentionnellement sur un adversaire.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Gerbe de Vomi",         Categorie = SkillCategory.Mutation, Description = "Ce joueur peut régurgiter de la bile sur les adversaires adjacents.", EstTrait = true };

        // ═══════════════════ SCÉLÉRATE (S) ═══════════════════
        yield return new Skill { RulesVersionId = versionId, Nom ="Blocage Sournois",      Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut faire un Blocage supplémentaire lors d'une Action de Mouvement." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Coup de Poing Vicieux", Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut frapper un joueur À Terre adjacent sans effectuer d'Action de Blocage." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Poignard",              Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut poignarder un adversaire lors d'une Action de Blocage, ajoutant +1 sur le jet d'AR.", EstTrait = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Meurtre Prémédité",     Categorie = SkillCategory.Scelerate, Description = "Si ce joueur inflige une Élimination, il peut tenter d'aggraver la blessure.", EstElite = true };
        yield return new Skill { RulesVersionId = versionId, Nom ="Croc-en-jambe",         Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut forcer un adversaire dans sa Zone de Tacle à relancer un jet d'Esquive réussi." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Coup de Pied Sournois", Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut frapper un joueur au sol même pendant une Action de Mouvement." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Poursuite",             Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut se déplacer d'une case vers un adversaire qui esquive sa Zone de Tacle." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Ralentissement",        Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut retarder l'activation d'un adversaire adjacent." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Sournois",              Categorie = SkillCategory.Scelerate, Description = "Si ce joueur utilise une compétence Scélérate, il n'est expulsé que sur un 1-2 (au lieu de 1)." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Poignard Sournois",     Categorie = SkillCategory.Scelerate, Description = "Ce joueur peut utiliser son poignard sans risquer l'expulsion." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Vraiment Sournois",     Categorie = SkillCategory.Scelerate, Description = "Sur un Foul, ce joueur peut être moins facilement repéré par les arbitres." };
        yield return new Skill { RulesVersionId = versionId, Nom ="Joueur Déloyal",        Categorie = SkillCategory.Scelerate, Description = "Sur Foul, +1 à un jet d'AR ou de Blessure (au choix après le dé)." };

        // ═══════════════════ SPÉCIFIQUES DUNGEON BOWL ═══════════════════
        if (game == GameType.DungeonBowl)
            yield return new Skill { RulesVersionId = versionId, Nom = "Navigateur de Portail", Categorie = SkillCategory.Generale, Description = "Ce joueur peut relancer le D6 lorsqu'il détermine le portail d'arrivée." };
    }
}
