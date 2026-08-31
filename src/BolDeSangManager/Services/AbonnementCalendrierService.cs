using System.Security.Cryptography;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Abonnement iCalendar par URL secrète (option A du plan
/// <c>.hermes/plans/2026-08-31-export-agenda-abonnement-ics.md</c>).
///
/// Le coach colle UNE fois une adresse dans son agenda ; Google, Apple ou
/// Outlook la réinterrogent ensuite tout seuls. Contrairement au téléchargement
/// .ics (qui reste en place pour l'import manuel), une nouvelle date ou un
/// nouveau match arrivent sans action de sa part.
///
/// ⚠️ Le flux est servi <b>sans authentification</b> : un agenda tiers interroge
/// l'URL sans cookie. Le jeton tient donc lieu de mot de passe — d'où
/// <see cref="RandomNumberGenerator"/> (jamais <c>Guid</c> ni <c>Random</c>, tous
/// deux prévisibles), 32 octets, encodés en Base64 URL-safe.
///
/// ⚠️ Le flux public applique le <b>mode brouillard</b> exactement comme les
/// écrans : un export qui le contournerait le viderait de son sens (le coach
/// n'aurait qu'à s'abonner pour voir tout le calendrier masqué).
/// </summary>
public class AbonnementCalendrierService(
    ApplicationDbContext db,
    CalendrierService calendrier)
{
    /// <summary>Taille du secret. 32 octets = 256 bits, non énumérable.</summary>
    private const int OctetsJeton = 32;

    /// <summary>
    /// Jeton du coach, créé au premier appel puis réutilisé. L'URL doit rester
    /// stable : la régénérer à chaque affichage casserait tous les abonnements
    /// déjà collés dans les agendas.
    /// </summary>
    public async Task<string?> ObtenirOuCreerJetonAsync(string userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        if (!string.IsNullOrEmpty(user.JetonCalendrier)) return user.JetonCalendrier;

        user.JetonCalendrier = NouveauJeton();
        await db.SaveChangesAsync();
        return user.JetonCalendrier;
    }

    /// <summary>
    /// Remplace le jeton : l'ancienne URL cesse immédiatement de répondre.
    /// Seule protection en cas de fuite du lien, donc obligatoire dans l'UI.
    /// </summary>
    public async Task<string?> RegenererJetonAsync(string userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        user.JetonCalendrier = NouveauJeton();
        await db.SaveChangesAsync();
        return user.JetonCalendrier;
    }

    /// <summary>
    /// Base64 URL-safe : ni <c>+</c>, ni <c>/</c>, ni <c>=</c>, donc utilisable
    /// tel quel dans un chemin d'URL sans encodage supplémentaire.
    /// </summary>
    public static string NouveauJeton() =>
        Base64Url(RandomNumberGenerator.GetBytes(OctetsJeton));

    private static string Base64Url(byte[] octets) =>
        Convert.ToBase64String(octets)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    /// <summary>
    /// Coach propriétaire de ce jeton, ou null s'il est inconnu.
    /// Un jeton vide ne doit jamais correspondre : sinon tout compte n'ayant
    /// jamais demandé d'abonnement serait exposé par l'URL « /calendrier/.ics ».
    /// </summary>
    public async Task<ApplicationUser?> TrouverParJetonAsync(string? jeton)
    {
        if (string.IsNullOrWhiteSpace(jeton)) return null;

        return await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.JetonCalendrier == jeton && !u.EstSupprime);
    }

    /// <summary>
    /// Matchs que ce coach a le droit de voir, mode brouillard appliqué ligue
    /// par ligue — la même règle que « Mes matchs ».
    /// </summary>
    /// <param name="ligueId">Restreint le flux à une ligue ; null = toutes.</param>
    /// <param name="ligueComplete">
    /// true = TOUS les matchs de la ligue, pas seulement ceux du coach (flux
    /// « calendrier de la ligue »). Exige <paramref name="ligueId"/>.
    ///
    /// ⚠️ Décision produit assumée : ce flux N'APPLIQUE PAS le mode brouillard.
    /// Voir les matchs datés des autres équipes est justement ce qu'on veut
    /// pour s'organiser, et le brouillard vise à empêcher de préparer SES
    /// prochaines rencontres, pas à cacher un calendrier commun. Conséquence à
    /// connaître : sur une ligue en brouillard, ce flux montre PLUS que
    /// l'onglet Calendrier de l'application, qui filtre, lui. L'interface le
    /// signale au commissaire au moment de publier le lien.
    /// </param>
    public async Task<List<Match>> MatchsVisiblesAsync(
        string userId, int? ligueId = null, bool ligueComplete = false)
    {
        if (ligueComplete)
        {
            if (ligueId is not int id) return [];

            return await db.Matches.AsNoTracking().AsSplitQuery()
                .Include(m => m.EquipeDomicile)
                .Include(m => m.EquipeExterieur)
                .Include(m => m.Division).ThenInclude(d => d!.League)
                .Where(m => m.Division!.LeagueId == id)
                .OrderBy(m => m.Ronde)
                .ToListAsync();
        }

        var mesEquipes = await db.Teams.AsNoTracking()
            .Where(t => t.CoachId == userId)
            .Select(t => t.Id)
            .ToHashSetAsync();

        if (mesEquipes.Count == 0) return [];

        var matchs = await db.Matches.AsNoTracking().AsSplitQuery()
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Division).ThenInclude(d => d!.League)
            .Where(m => mesEquipes.Contains(m.EquipeDomicileId)
                     || mesEquipes.Contains(m.EquipeExterieurId))
            .Where(m => ligueId == null || m.Division!.LeagueId == ligueId)
            .OrderBy(m => m.Ronde)
            .ToListAsync();

        var ligues = await db.Leagues.AsNoTracking()
            .Include(l => l.CommissairesDeLigue)
            .ToListAsync();

        var brouillardParLigue = ligues.ToDictionary(l => l.Id, l => l.ModeBrouillard);

        // Commissaire au sens NOMMÉ sur la ligue. Un rôle global (Admin) n'élève
        // volontairement pas ce flux : moins de données dans un lien non
        // authentifié est toujours le choix le plus prudent.
        var commissaireDe = ligues
            .Where(l => l.CommissaireId == userId
                     || l.CommissairesDeLigue.Any(c => c.UserId == userId))
            .Select(l => l.Id)
            .ToHashSet();

        return BrouillardHelpers.FiltrerVisiblesMultiLigues(
                matchs.DistinctBy(m => m.Id), mesEquipes, brouillardParLigue, commissaireDe)
            .OrderBy(m => m.Ronde)
            .ToList();
    }

    /// <summary>
    /// Contenu .ics du flux d'un jeton. Retourne null si le jeton est inconnu
    /// (l'endpoint répond alors 404) ou si la ligue demandée n'existe pas.
    /// Réutilise <see cref="CalendrierService.GenererIcs(IEnumerable{Match}, string)"/>
    /// tel quel : la génération RFC 5545 est déjà correcte et testée.
    /// </summary>
    public async Task<byte[]?> GenererFluxAsync(
        string? jeton, int? ligueId = null, bool ligueComplete = false)
    {
        var user = await TrouverParJetonAsync(jeton);
        if (user is null) return null;

        var nom = "Mes matchs — BolDeSang";

        if (ligueId is int id)
        {
            var ligue = await db.Leagues.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (ligue is null) return null;
            nom = ligueComplete
                ? $"{ligue.Nom} — calendrier complet"
                : $"{ligue.Nom} — BolDeSang";
        }
        else if (ligueComplete)
        {
            // « Ligue complète » sans ligue n'a pas de sens : refuser plutôt que
            // de retomber silencieusement sur le flux personnel.
            return null;
        }

        var matchs = await MatchsVisiblesAsync(user.Id, ligueId, ligueComplete);
        return calendrier.GenererIcs(matchs, nom);
    }
}
