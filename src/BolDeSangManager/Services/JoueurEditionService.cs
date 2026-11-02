using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// « Modifier le joueur » : la fenêtre ouverte par le crayon en bout de ligne
/// sur la page d'une équipe. Enregistre en UNE fois le nom (#16), les Haines
/// (#28) et le titre de capitaine — ce qui se modifie d'un joueur hors feuille
/// de match.
///
/// ⚠️ Ne JAMAIS passer par <c>TeamService.ModifierEquipeAsync</c> : il supprime
/// puis recrée tout le roster (d'où son verrou en phase Inscription) et
/// effacerait PSP, compétences, blessures et améliorations. Ici, on ne touche
/// qu'aux champs reçus, sans regarder le statut de la ligue : ni le nom ni une
/// Haine n'entrent dans un calcul (VEA, budget, classement).
///
/// Droits : le coach de l'équipe, et le commissaire de la ligue (admins et
/// grands commissaires compris). L'écran propose, le service fait autorité.
/// </summary>
public class JoueurEditionService(
    ApplicationDbContext db,
    IAuthorizationService auth,
    ILogger<JoueurEditionService> logger)
{
    public const int NomLongueurMax = 100;

    /// <param name="nom">Nouveau nom ; vide = repli sur le numéro de maillot.</param>
    /// <param name="haines">Liste COMPLÈTE des cibles voulues (ex. ["Orque"]) ;
    /// une Haine absente de la liste est retirée.</param>
    /// <param name="capitaine">true = désigner (l'ancien capitaine perd le
    /// titre), false = retirer le titre, null = ne pas y toucher.</param>
    public async Task ModifierJoueurAsync(int joueurId, string userId, string? nom, IEnumerable<string?> haines,
        bool? capitaine = null)
    {
        var joueur = await db.TeamPlayers
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == joueurId)
            ?? throw new InvalidOperationException("Joueur introuvable");

        if (joueur.Team.CoachId != userId && !await auth.PeutGererLigueAsync(userId, joueur.Team.LeagueId))
            throw new InvalidOperationException(
                "Seuls le coach de l'équipe et le commissaire de la ligue peuvent modifier ce joueur.");

        // ── Nom : mêmes règles qu'à la création. Un joueur mort ou retraité
        // reste renommable — c'est de l'historique, pas du jeu.
        var propre = (nom ?? string.Empty).Trim();
        if (propre.Length > NomLongueurMax)
            throw new InvalidOperationException(
                $"Le nom d'un joueur ne peut pas dépasser {NomLongueurMax} caractères.");
        var nouveauNom = string.IsNullOrWhiteSpace(propre) ? $"#{joueur.Numero}" : propre;

        // ── Haines : tout est validé AVANT d'écrire quoi que ce soit.
        var voulues = new List<string>();
        foreach (var saisie in haines)
        {
            var cible = HaineHelpers.Normaliser(saisie);
            if (cible is not null && !voulues.Contains(cible, StringComparer.OrdinalIgnoreCase))
                voulues.Add(cible);
        }
        var nouvelles = voulues.Where(h => !HaineHelpers.Contient(joueur.Haines, h)).ToList();
        if (nouvelles.Count > 0 && joueur.EstMort)
            throw new InvalidOperationException("Un joueur mort ne gagne plus de Haine.");
        foreach (var h in nouvelles)
            if (await HaineService.ALaHaineDansSonProfilAsync(db, joueur, h))
                throw new InvalidOperationException(
                    $"{HaineHelpers.Libelle(h)} fait déjà partie du profil de son poste.");

        // ── Capitaine : un seul par équipe, seulement si la race a la règle,
        // jamais un absent (mort, retraité). Vérifié avant toute écriture.
        List<TeamPlayer> anciensCapitaines = [];
        if (capitaine == true && !joueur.EstCapitaine)
        {
            var race = await db.TeamTypes
                .Include(t => t.ReglesSpecialesListe).ThenInclude(l => l.SpecialRule)
                .FirstAsync(t => t.Id == joueur.Team.TeamTypeId);
            if (!CapitaineHelper.RaceAUnCapitaine(race))
                throw new InvalidOperationException("La race de cette équipe n'a pas de capitaine.");
            if (joueur.EstMort || joueur.EstRetraite)
                throw new InvalidOperationException("Un joueur mort ou retraité ne peut pas être capitaine.");
            anciensCapitaines = await db.TeamPlayers
                .Where(p => p.TeamId == joueur.TeamId && p.EstCapitaine && p.Id != joueur.Id)
                .ToListAsync();
        }

        var anciennes = joueur.Haines;
        joueur.Nom = nouveauNom;
        joueur.Haines = HaineHelpers.Joindre(voulues);
        if (capitaine is bool cap)
        {
            foreach (var p in anciensCapitaines) p.EstCapitaine = false;
            joueur.EstCapitaine = cap;
        }
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Joueur id={JoueurId} modifié par {UserId} : nom « {Nom} », Haines « {Avant} » → « {Apres} », capitaine {Capitaine}",
            joueur.Id, userId, joueur.Nom, anciennes, joueur.Haines, joueur.EstCapitaine);
    }
}
