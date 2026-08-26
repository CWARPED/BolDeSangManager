using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class LeagueService(
    ApplicationDbContext db,
    ILogger<LeagueService> logger,
    IAuthorizationService authService)
{
    public async Task<List<League>> GetAllLiguesAsync() =>
        await db.Leagues
            .Include(l => l.Game)
            .Include(l => l.RulesVersion)
            .Include(l => l.Commissaire)
            .Include(l => l.Equipes)
            .OrderByDescending(l => l.CreeLe)
            .ToListAsync();

    public async Task<League?> GetLigueAsync(int id) =>
        await db.Leagues
            .Include(l => l.Game)
            .Include(l => l.RulesVersion)
            .Include(l => l.Commissaire)
            // Équipes de la ligue chargées directement : en format Libre elles
            // n'ont pas encore de division au moment de composer le calendrier,
            // et passer uniquement par Divisions les rendrait invisibles.
            .Include(l => l.Equipes).ThenInclude(e => e.Coach)
            .Include(l => l.Equipes).ThenInclude(e => e.TeamType)
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes).ThenInclude(e => e.Coach)
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes).ThenInclude(e => e.TeamType)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs)
                .ThenInclude(m => m.EquipeDomicile)
            .Include(l => l.Divisions).ThenInclude(d => d.Matchs)
                .ThenInclude(m => m.EquipeExterieur)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<League> CreerLigueAsync(League ligue, string commissaireId)
    {
        ligue.CommissaireId = commissaireId;
        ligue.Statut = LeagueStatus.Creation;
        ligue.CreeLe = DateTime.UtcNow;

        db.Leagues.Add(ligue);
        await db.SaveChangesAsync();
        logger.LogInformation("Ligue créée : {NomLigue} (id={Id}) par commissaire {CommissaireId}", ligue.Nom, ligue.Id, commissaireId);
        return ligue;
    }

    public async Task DemarrerInscriptionsAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        ligue.Statut = LeagueStatus.Inscription;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Lance la saison : crée la division par défaut puis génère le pool de
    /// matchs (sauf en format Libre, où le commissaire compose lui-même).
    /// </summary>
    /// <returns>
    /// Les numéros de ronde dont l'échéance a été retirée parce qu'ils
    /// dépassent le calendrier réellement généré. Le commissaire peut dater ses
    /// rondes avant le lancement, alors que le nombre de rondes n'est pas encore
    /// connu (il dépend du nombre d'équipes inscrites) : les dates en trop sont
    /// nettoyées ici pour ne pas afficher d'échéances fantômes.
    /// Liste vide en format Libre — aucune ronde n'existe encore au lancement,
    /// et tout nettoyer effacerait le planning préparé en amont.
    /// </returns>
    public async Task<IReadOnlyList<int>> LancerSaisonAsync(int ligueId)
    {
        var ligue = await db.Leagues
            .Include(l => l.Equipes)
            .Include(l => l.Divisions)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Equipes.Count < 2)
            throw new InvalidOperationException("Il faut au moins 2 équipes pour lancer la saison.");

        // Créer une division par défaut si aucune
        if (!ligue.Divisions.Any())
        {
            var div = new Division { LeagueId = ligueId, Nom = "Division Unique", Ordre = 1 };
            db.Divisions.Add(div);
            await db.SaveChangesAsync();

            foreach (var equipe in ligue.Equipes)
                equipe.DivisionId = div.Id;

            await db.SaveChangesAsync();
        }

        // Format Libre : le commissaire compose lui-même les rondes après le
        // lancement. On crée quand même la division ci-dessus, il en a besoin
        // pour y rattacher ses rencontres.
        var calendrierGenere = !DisplayHelpers.EstFormatLibre(ligue.Format);
        if (calendrierGenere)
            await GenererPoolMatchsAsync(ligue);

        ligue.Statut = LeagueStatus.EnCours;
        await db.SaveChangesAsync();

        var orphelines = calendrierGenere
            ? await NettoyerEcheancesOrphelinesAsync(ligueId)
            : [];

        logger.LogInformation("Saison lancée pour la ligue {NomLigue} (id={Id}) avec {NbEquipes} équipes (format={Format})", ligue.Nom, ligue.Id, ligue.Equipes.Count, ligue.Format);
        return orphelines;
    }

    /// <summary>
    /// Retire les échéances dont la ronde n'existe pas dans le calendrier généré.
    /// Appelé uniquement quand le calendrier est produit automatiquement : en
    /// format Libre les rondes sont créées après coup, il n'y a rien à comparer.
    /// </summary>
    private async Task<IReadOnlyList<int>> NettoyerEcheancesOrphelinesAsync(int ligueId)
    {
        var rondesReelles = await db.Matches
            .Where(m => m.Division!.LeagueId == ligueId)
            .Select(m => m.Ronde)
            .Distinct()
            .ToListAsync();

        var orphelines = await db.EcheancesRondes
            .Where(e => e.LeagueId == ligueId && !rondesReelles.Contains(e.Ronde))
            .ToListAsync();

        if (orphelines.Count == 0) return [];

        db.EcheancesRondes.RemoveRange(orphelines);
        await db.SaveChangesAsync();

        var numeros = orphelines.Select(e => e.Ronde).OrderBy(r => r).ToList();
        logger.LogInformation(
            "Ligue {Id} : {Nb} échéance(s) retirée(s), rondes {Rondes} absentes du calendrier généré",
            ligueId, numeros.Count, string.Join(", ", numeros));
        return numeros;
    }

    /// <summary>
    /// Format Libre : définit (ou remplace) les rencontres d'une ronde.
    /// Une équipe non citée est simplement au repos ce tour-ci.
    /// Une ronde dont un match est déjà joué ne peut plus être modifiée.
    /// </summary>
    public async Task DefinirRondeAsync(int ligueId, int ronde, IReadOnlyList<(int domicileId, int exterieurId)> paires)
    {
        var ligue = await db.Leagues
            .Include(l => l.Equipes)
            .Include(l => l.Divisions)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (!DisplayHelpers.EstFormatLibre(ligue.Format))
            throw new InvalidOperationException(
                "Seules les ligues au format Libre permettent de composer les rondes à la main.");

        if (ronde < 1)
            throw new InvalidOperationException("Le numéro de ronde doit être supérieur ou égal à 1.");

        var equipesDeLaLigue = ligue.Equipes.ToDictionary(e => e.Id, e => e.Nom);

        // Validation des paires avant toute écriture.
        var vues = new Dictionary<int, string>();
        foreach (var (domicileId, exterieurId) in paires)
        {
            if (domicileId == exterieurId)
                throw new InvalidOperationException("Une équipe ne peut pas se rencontrer elle-même.");

            foreach (var id in new[] { domicileId, exterieurId })
            {
                if (!equipesDeLaLigue.TryGetValue(id, out var nom))
                    throw new InvalidOperationException("Une des équipes sélectionnées n'appartient pas à cette ligue.");

                if (!vues.TryAdd(id, nom))
                    throw new InvalidOperationException(
                        $"« {nom} » apparaît deux fois dans la ronde {ronde} : une équipe ne peut jouer qu'un match par ronde.");
            }
        }

        var divisionId = ligue.Divisions.OrderBy(d => d.Ordre).FirstOrDefault()?.Id
            ?? throw new InvalidOperationException("La ligue n'a pas encore de division. Lancez la saison d'abord.");

        var existants = await db.Matches
            .Where(m => m.Division!.LeagueId == ligueId && m.Ronde == ronde && !m.EstPlayoff)
            .ToListAsync();

        if (existants.Any(BrouillardHelpers.EstJoue))
            throw new InvalidOperationException(
                $"La ronde {ronde} a déjà commencé : elle ne peut plus être modifiée.");

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            db.Matches.RemoveRange(existants);
            await db.SaveChangesAsync();

            db.Matches.AddRange(paires.Select(p => new Match
            {
                DivisionId        = divisionId,
                Ronde             = ronde,
                EquipeDomicileId  = p.domicileId,
                EquipeExterieurId = p.exterieurId,
                Statut            = MatchStatus.Programme,
                EstPlayoff        = false
            }));
            await db.SaveChangesAsync();

            await tx.CommitAsync();
            logger.LogInformation("Ronde {Ronde} définie pour la ligue {Id} : {NbMatchs} rencontre(s)", ronde, ligueId, paires.Count);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Date indicative de fin de ronde : celle à laquelle les matchs devraient
    /// être joués. Purement informative — passer `null` retire l'échéance.
    /// </summary>
    public async Task DefinirEcheanceRondeAsync(int ligueId, int ronde, DateTime? dateLimite)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ronde < 1)
            throw new InvalidOperationException("Le numéro de ronde doit être supérieur ou égal à 1.");

        var existante = await db.EcheancesRondes
            .FirstOrDefaultAsync(e => e.LeagueId == ligueId && e.Ronde == ronde);

        // Une échéance est une DATE, pas un instant. On la stocke à midi UTC :
        // minuit basculerait d'un jour à l'autre selon le fuseau et l'heure
        // d'été, et la date affichée ne serait plus celle qui a été saisie.
        DateTime? valeur = dateLimite is null
            ? null
            : new DateTime(dateLimite.Value.Year, dateLimite.Value.Month, dateLimite.Value.Day,
                           12, 0, 0, DateTimeKind.Utc);

        if (valeur is null)
        {
            if (existante is not null) db.EcheancesRondes.Remove(existante);
        }
        else if (existante is null)
        {
            db.EcheancesRondes.Add(new EcheanceRonde
            {
                LeagueId   = ligueId,
                Ronde      = ronde,
                DateLimite = valeur.Value
            });
        }
        else
        {
            existante.DateLimite = valeur.Value;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Échéance de la ronde {Ronde} (ligue {Id}) : {Date}", ronde, ligueId, dateLimite?.ToString("yyyy-MM-dd") ?? "retirée");
    }

    /// <summary>Échéances indicatives d'une ligue, indexées par numéro de ronde.</summary>
    public async Task<Dictionary<int, DateTime>> GetEcheancesRondesAsync(int ligueId) =>
        await db.EcheancesRondes
            .Where(e => e.LeagueId == ligueId)
            .ToDictionaryAsync(e => e.Ronde, e => e.DateLimite);

    /// <summary>
    /// Format Libre : propose un appariement des équipes encore libres d'une
    /// ronde, en évitant autant que possible les affrontements déjà programmés
    /// dans les autres rondes de la ligue.
    ///
    /// Un simple appariement dans l'ordre rejouerait sans cesse les mêmes
    /// rencontres. On retient donc, parmi les paires possibles, celle dont les
    /// deux équipes se sont le moins souvent affrontées (puis, à égalité, celle
    /// qui a le moins joué), en inversant domicile/extérieur par rapport à la
    /// dernière confrontation.
    /// </summary>
    public async Task<List<(int domicileId, int exterieurId)>> ProposerAppariementsAsync(
        int ligueId, int ronde, IReadOnlyList<int> equipesLibres,
        IReadOnlyList<(int domicileId, int exterieurId)>? dejaComposees = null)
    {
        var libres = equipesLibres.ToList();
        var propositions = new List<(int, int)>();
        if (libres.Count < 2) return propositions;

        // Historique des confrontations, toutes rondes confondues sauf celle-ci.
        var historique = (await db.Matches
            .Where(m => m.Division!.LeagueId == ligueId && m.Ronde != ronde)
            .Select(m => new { m.EquipeDomicileId, m.EquipeExterieurId })
            .ToListAsync())
            .Select(m => (m.EquipeDomicileId, m.EquipeExterieurId))
            .ToList();

        // Rondes composées à l'écran mais pas encore enregistrées : sans elles,
        // deux rondes créées d'affilée proposeraient les mêmes rencontres.
        if (dejaComposees is not null)
            historique.AddRange(dejaComposees);

        static string Cle(int a, int b) => a < b ? $"{a}-{b}" : $"{b}-{a}";

        var nbRencontres = new Dictionary<string, int>();
        var nbMatchs     = new Dictionary<int, int>();
        var dernierDomicile = new Dictionary<string, int>();

        foreach (var (dom, ext) in historique)
        {
            var cle = Cle(dom, ext);
            nbRencontres[cle] = nbRencontres.GetValueOrDefault(cle) + 1;
            dernierDomicile[cle] = dom;
            nbMatchs[dom] = nbMatchs.GetValueOrDefault(dom) + 1;
            nbMatchs[ext] = nbMatchs.GetValueOrDefault(ext) + 1;
        }

        while (libres.Count >= 2)
        {
            // L'équipe la moins servie ouvre l'appariement.
            var a = libres.OrderBy(id => nbMatchs.GetValueOrDefault(id)).ThenBy(id => id).First();
            libres.Remove(a);

            // Son adversaire : celui qu'elle a le moins rencontré.
            var b = libres
                .OrderBy(id => nbRencontres.GetValueOrDefault(Cle(a, id)))
                .ThenBy(id => nbMatchs.GetValueOrDefault(id))
                .ThenBy(id => id)
                .First();
            libres.Remove(b);

            // Alternance : si a recevait la dernière fois, il se déplace.
            var cle = Cle(a, b);
            var aRecuDernierement = dernierDomicile.TryGetValue(cle, out var d) && d == a;
            propositions.Add(aRecuDernierement ? (b, a) : (a, b));

            nbRencontres[cle] = nbRencontres.GetValueOrDefault(cle) + 1;
            dernierDomicile[cle] = aRecuDernierement ? b : a;
            nbMatchs[a] = nbMatchs.GetValueOrDefault(a) + 1;
            nbMatchs[b] = nbMatchs.GetValueOrDefault(b) + 1;
        }

        return propositions;
    }

    /// <summary>
    /// Renumérote les rondes d'une ligue en 1, 2, 3… sans trou, après une
    /// suppression. Une ronde déjà commencée ne peut pas changer de numéro
    /// (des matchs joués y font référence) : dans ce cas on ne touche à rien
    /// et on renvoie false, à charge de l'appelant de laisser la numérotation.
    /// </summary>
    public async Task<bool> RenumeroterRondesAsync(int ligueId)
    {
        var matchs = await db.Matches
            .Where(m => m.Division!.LeagueId == ligueId && !m.EstPlayoff)
            .ToListAsync();

        if (matchs.Count == 0) return true;

        var ordre = matchs.Select(m => m.Ronde).Distinct().OrderBy(r => r).ToList();
        var cible = ordre.Select((ancien, i) => (ancien, nouveau: i + 1))
                         .Where(x => x.ancien != x.nouveau)
                         .ToList();

        if (cible.Count == 0) return true;   // déjà compact

        // Une ronde commencée ne doit pas être renumérotée.
        var rondesCommencees = matchs.Where(BrouillardHelpers.EstJoue)
                                     .Select(m => m.Ronde).ToHashSet();
        if (cible.Any(x => rondesCommencees.Contains(x.ancien)))
        {
            logger.LogInformation("Renumérotation ignorée pour la ligue {Id} : une ronde a déjà commencé", ligueId);
            return false;
        }

        var map = cible.ToDictionary(x => x.ancien, x => x.nouveau);
        foreach (var m in matchs.Where(m => map.ContainsKey(m.Ronde)))
            m.Ronde = map[m.Ronde];

        var echeances = await db.EcheancesRondes.Where(e => e.LeagueId == ligueId).ToListAsync();
        foreach (var e in echeances.Where(e => map.ContainsKey(e.Ronde)))
            e.Ronde = map[e.Ronde];

        await db.SaveChangesAsync();
        logger.LogInformation("Rondes renumérotées pour la ligue {Id} : {Nb} décalage(s)", ligueId, map.Count);
        return true;
    }

    /// <summary>
    /// Format Libre : supprime une ronde entière. Les rondes suivantes ne sont
    /// pas renumérotées — le commissaire recompose s'il le souhaite.
    /// </summary>
    public async Task SupprimerRondeAsync(int ligueId, int ronde)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (!DisplayHelpers.EstFormatLibre(ligue.Format))
            throw new InvalidOperationException(
                "Seules les ligues au format Libre permettent de supprimer une ronde.");

        var matchs = await db.Matches
            .Where(m => m.Division!.LeagueId == ligueId && m.Ronde == ronde && !m.EstPlayoff)
            .ToListAsync();

        if (matchs.Any(BrouillardHelpers.EstJoue))
            throw new InvalidOperationException(
                $"La ronde {ronde} a déjà commencé : elle ne peut plus être supprimée.");

        db.Matches.RemoveRange(matchs);

        // L'échéance de la ronde n'a plus d'objet.
        var echeance = await db.EcheancesRondes
            .FirstOrDefaultAsync(e => e.LeagueId == ligueId && e.Ronde == ronde);
        if (echeance is not null) db.EcheancesRondes.Remove(echeance);

        await db.SaveChangesAsync();
        logger.LogInformation("Ronde {Ronde} supprimée pour la ligue {Id} ({NbMatchs} rencontre(s))", ronde, ligueId, matchs.Count);
    }

    private async Task GenererPoolMatchsAsync(League ligue)
    {
        var divisions = await db.Divisions
            .Include(d => d.Equipes)
            .Where(d => d.LeagueId == ligue.Id)
            .ToListAsync();

        foreach (var division in divisions)
        {
            var equipes = division.Equipes.ToList();
            var matchs = GenererRoundRobin(equipes, division.Id);
            db.Matches.AddRange(matchs);
        }
        await db.SaveChangesAsync();
    }

    private static List<Match> GenererRoundRobin(List<Team> equipes, int divisionId)
    {
        // Copie locale ; si nombre impair, ajouter null comme équipe fantôme (bye)
        // Cela rend n pair et garantit que chaque équipe rencontre toutes les autres.
        var liste = new List<Team?>(equipes.Cast<Team?>());
        if (liste.Count % 2 != 0) liste.Add(null);
        int n = liste.Count;

        var matchs = new List<Match>();

        for (int ronde = 1; ronde < n; ronde++)
        {
            for (int i = 0; i < n / 2; i++)
            {
                var domicile = liste[i];
                var exterieur = liste[n - 1 - i];

                // Ignorer les matches impliquant l'équipe fantôme (= bye)
                if (domicile is null || exterieur is null) continue;

                matchs.Add(new Match
                {
                    DivisionId = divisionId,
                    Ronde = ronde,
                    EquipeDomicileId = domicile.Id,
                    EquipeExterieurId = exterieur.Id,
                    Statut = MatchStatus.Programme,
                    EstPlayoff = false
                });
            }

            // Rotation : fixer la première équipe, faire tourner les autres
            var derniere = liste[n - 1];
            liste.RemoveAt(n - 1);
            liste.Insert(1, derniere);
        }

        return matchs;
    }

    public async Task LancerPhaseDeReposAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Statut != LeagueStatus.EnCours)
            throw new InvalidOperationException("La phase de repos ne peut être lancée que depuis l'état EnCours.");

        var teamIds = await db.Teams.Where(t => t.LeagueId == ligueId).Select(t => t.Id).ToListAsync();
        var joueurs = await db.TeamPlayers
            .Where(j => teamIds.Contains(j.TeamId) && j.ManqueSuivantMatch)
            .ToListAsync();

        foreach (var j in joueurs)
            j.ManqueSuivantMatch = false;

        ligue.Statut = LeagueStatus.PhaseDeRepos;
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Phase de repos lancée pour la ligue {NomLigue} (id={Id}) : {NbResetRPM} RPM reset sur {NbEquipes} équipes",
            ligue.Nom, ligue.Id, joueurs.Count, teamIds.Count);
    }

    public async Task GenererPlayoffsAsync(int ligueId)
    {
        var ligue = await db.Leagues
            .Include(l => l.Divisions).ThenInclude(d => d.Equipes)
            .FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        if (ligue.Statut != LeagueStatus.PhaseDeRepos && ligue.Statut != LeagueStatus.EnCours)
            throw new InvalidOperationException("Les playoffs ne peuvent être générés qu'après la saison régulière ou la phase de repos.");

        // Trier les équipes par points de ligue
        var equipesQualifiees = ligue.Divisions
            .SelectMany(d => d.Equipes)
            .OrderByDescending(e => e.PointsLigue)
            .ThenByDescending(e => e.TouchdownsMarques - e.TouchdownsConcedes)
            .ThenByDescending(e => e.EliminationsInfligees)
            .Take(ligue.NombreEquipesPlayoff)
            .ToList();

        if (equipesQualifiees.Count < 2)
            throw new InvalidOperationException("Pas assez d'équipes qualifiées pour les playoffs.");

        // Générer les quarts (ou demi si 4 équipes)
        var matchsPlayoff = new List<Match>();
        int ronde = 100; // Ronde 100+ = playoffs
        for (int i = 0; i < equipesQualifiees.Count / 2; i++)
        {
            matchsPlayoff.Add(new Match
            {
                DivisionId = ligue.Divisions.First().Id,
                Ronde = ronde,
                EquipeDomicileId = equipesQualifiees[i].Id,
                EquipeExterieurId = equipesQualifiees[equipesQualifiees.Count - 1 - i].Id,
                Statut = MatchStatus.Programme,
                EstPlayoff = true
            });
        }

        db.Matches.AddRange(matchsPlayoff);
        ligue.Statut = LeagueStatus.PlayOffs;
        await db.SaveChangesAsync();
        logger.LogInformation("Playoffs générés pour la ligue {NomLigue} (id={Id}) : {NbMatchs} matchs, {NbEquipes} équipes qualifiées", ligue.Nom, ligue.Id, matchsPlayoff.Count, equipesQualifiees.Count);
    }

    /// <summary>
    /// Validation post-match de repos par un coach : applique améliorations, recrutements et achat de relances
    /// sans qu'un Match ne soit nécessaire. Trace via PhaseDeReposValidation.
    /// </summary>
    public async Task ValiderApresMatchReposAsync(
        int ligueId,
        int teamId,
        List<(int joueurId, int skillId, bool estPrincipale, int xpDepensee)> competences,
        List<(int positionId, string nom, int numero)> nouveauxJoueurs,
        int nouvellesRelances,
        TeamService teamService)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        if (ligue.Statut != LeagueStatus.PhaseDeRepos)
            throw new InvalidOperationException("La validation de repos n'est possible que pendant la phase de repos.");

        var dejaValide = await db.PhaseDeReposValidations
            .AnyAsync(v => v.LeagueId == ligueId && v.TeamId == teamId);
        if (dejaValide)
            throw new InvalidOperationException("Cette équipe a déjà validé sa phase de repos.");

        foreach (var (joueurId, skillId, estPrincipale, xpDepensee) in competences)
        {
            var type = estPrincipale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire;
            await teamService.AppliquerAmeliorationAsync(joueurId, type, skillId: skillId,
                matchSheetId: null, xpDepensee: xpDepensee);
        }

        foreach (var (positionId, nom, numero) in nouveauxJoueurs)
            await teamService.RecruterJoueurAsync(teamId, positionId, nom, numero);

        if (nouvellesRelances > 0)
        {
            var equipe = await db.Teams.Include(t => t.TeamType).FirstAsync(t => t.Id == teamId);
            var coutRelance = (equipe.TeamType?.CoutRelance ?? 50_000) * 2;
            var total = nouvellesRelances * coutRelance;
            if (equipe.Tresorerie < total)
                throw new InvalidOperationException("Fonds insuffisants pour acheter les relances.");
            const int maxRelances = 8;
            if (equipe.NombreRelances + nouvellesRelances > maxRelances)
                throw new InvalidOperationException($"Maximum {maxRelances} relances par équipe.");
            equipe.Tresorerie -= total;
            equipe.NombreRelances += nouvellesRelances;
        }

        db.PhaseDeReposValidations.Add(new PhaseDeReposValidation
        {
            LeagueId = ligueId,
            TeamId = teamId
        });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Phase de repos validée pour équipe id={TeamId} dans ligue id={LigueId} : {NbComp} comp., {NbNouv} recrues, {NbRel} relances",
            teamId, ligueId, competences.Count, nouveauxJoueurs.Count, nouvellesRelances);
    }

    public async Task TerminerLigueAsync(int ligueId)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");
        ligue.Statut = LeagueStatus.Termine;
        await db.SaveChangesAsync();
    }

    public async Task<List<Match>> GetMatchsDivisionAsync(int divisionId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.Feuille)
            .Where(m => m.DivisionId == divisionId)
            .OrderBy(m => m.Ronde)
            .ToListAsync();

    public async Task<List<Game>> GetGamesAsync() =>
        await db.Games.Include(g => g.Versions).ToListAsync();

    public async Task<List<RulesVersion>> GetVersionsAsync(int gameId) =>
        await db.RulesVersions
            .Where(v => v.GameId == gameId)
            .OrderBy(v => v.Ordre)
            .ToListAsync();

    public async Task<bool> EstCommissaireAsync(int ligueId, string userId)
        => await authService.PeutGererLigueAsync(userId, ligueId);

    /// <summary>
    /// Enregistre le règlement (markdown) d'une ligue (R5). Réservé aux commissaires :
    /// l'habilitation est revérifiée ici, pas seulement masquée dans l'UI.
    /// </summary>
    public async Task DefinirReglementAsync(int ligueId, string markdown, string userId)
    {
        if (!await authService.PeutGererLigueAsync(userId, ligueId))
            throw new UnauthorizedAccessException("Seul un commissaire peut modifier le règlement.");

        var ligue = await db.Leagues.FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        ligue.Reglement = markdown ?? string.Empty;
        await db.SaveChangesAsync();

        logger.LogInformation("Ligue id={LigueId} : règlement mis à jour par {UserId} ({Taille} caractères)",
            ligueId, userId, ligue.Reglement.Length);
    }

    /// <summary>
    /// Active ou désactive le mode brouillard d'une ligue (#2). Réservé aux
    /// commissaires : l'habilitation est revérifiée ici, pas seulement dans l'UI.
    /// </summary>
    public async Task DefinirModeBrouillardAsync(int ligueId, bool actif, string userId)
    {
        if (!await authService.PeutGererLigueAsync(userId, ligueId))
            throw new UnauthorizedAccessException("Seul un commissaire peut modifier ce réglage.");

        var ligue = await db.Leagues.FirstOrDefaultAsync(l => l.Id == ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        ligue.ModeBrouillard = actif;
        await db.SaveChangesAsync();

        logger.LogInformation("Ligue id={LigueId} : mode brouillard {Etat} par {UserId}",
            ligueId, actif ? "activé" : "désactivé", userId);
    }

    public async Task<List<TeamPlayer>> GetTopJoueursParPspAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Where(j => j.Team.LeagueId == ligueId && !j.EstMort && !j.EstRetraite)
            .OrderByDescending(j => j.PointsStarPlayer)
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopMarqueursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.Touchdowns))
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopElimineursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.EliminationsInfligees))
            .Take(limit)
            .ToListAsync();

    public async Task<List<TeamPlayer>> GetTopPasseursAsync(int ligueId, int limit = 10) =>
        await db.TeamPlayers
            .Include(j => j.Team)
            .Include(j => j.PlayerPosition)
            .Include(j => j.RecordsMatchs)
            .Where(j => j.Team.LeagueId == ligueId)
            .OrderByDescending(j => j.RecordsMatchs.Sum(r => r.Passes + r.Interceptions))
            .Take(limit)
            .ToListAsync();

    public record CoachClassement(ApplicationUser Coach, int PointsLigue, int Victoires, int Nuls, int Defaites);

    public async Task<List<CoachClassement>> GetTopCoachsAsync(int ligueId)
    {
        var equipes = await db.Teams
            .Include(t => t.Coach)
            .Where(t => t.LeagueId == ligueId)
            .ToListAsync();

        return [.. equipes
            .GroupBy(t => t.Coach)
            .Select(g => new CoachClassement(
                g.Key,
                g.Sum(t => t.PointsLigue),
                g.Sum(t => t.NombreVictoires),
                g.Sum(t => t.NombreNuls),
                g.Sum(t => t.NombreDefaites)))
            .OrderByDescending(c => c.PointsLigue)
            .ThenByDescending(c => c.Victoires)];
    }

    public async Task AttribuerAwardAsync(
        int ligueId, AwardType type,
        int? teamPlayerId = null, int? teamId = null, string? coachId = null)
    {
        var ligue = await db.Leagues.FindAsync(ligueId)
            ?? throw new InvalidOperationException("Ligue introuvable");

        var award = new LeagueAward
        {
            LeagueId = ligueId,
            Type = type,
            TeamPlayerId = teamPlayerId,
            TeamId = teamId,
            CoachId = coachId
        };
        db.LeagueAwards.Add(award);
        await db.SaveChangesAsync();
        logger.LogInformation("Award {AwardType} attribué dans la ligue id={LigueId}", type, ligueId);
    }

    public async Task<List<LeagueAward>> GetAwardsAsync(int ligueId) =>
        await db.LeagueAwards
            .Include(a => a.TeamPlayer).ThenInclude(j => j!.Team)
            .Include(a => a.Team)
            .Include(a => a.Coach)
            .Where(a => a.LeagueId == ligueId)
            .OrderBy(a => a.Type)
            .ToListAsync();

    public async Task SupprimerLigueAsync(int ligueId)
    {
        var divIds    = await db.Divisions.Where(d => d.LeagueId == ligueId).Select(d => d.Id).ToListAsync();
        var matchIds  = await db.Matches.Where(m => m.DivisionId.HasValue && divIds.Contains(m.DivisionId.Value)).Select(m => m.Id).ToListAsync();
        var sheetIds  = await db.MatchSheets.Where(s => matchIds.Contains(s.MatchId)).Select(s => s.Id).ToListAsync();
        var teamIds   = await db.Teams.Where(t => t.LeagueId == ligueId).Select(t => t.Id).ToListAsync();
        var playerIds = await db.TeamPlayers.Where(j => teamIds.Contains(j.TeamId)).Select(j => j.Id).ToListAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.MatchPlayerRecords.Where(r => sheetIds.Contains(r.MatchSheetId)).ExecuteDeleteAsync();
        await db.MatchSheets.Where(s => matchIds.Contains(s.MatchId)).ExecuteDeleteAsync();
        await db.PlayerInjuries.Where(b => playerIds.Contains(b.TeamPlayerId)).ExecuteDeleteAsync();
        await db.TeamPlayerSkills.Where(s => playerIds.Contains(s.TeamPlayerId)).ExecuteDeleteAsync();
        await db.Matches.Where(m => matchIds.Contains(m.Id)).ExecuteDeleteAsync();
        await db.TeamPlayers.Where(j => playerIds.Contains(j.Id)).ExecuteDeleteAsync();
        await db.Teams.Where(t => teamIds.Contains(t.Id)).ExecuteDeleteAsync();
        await db.Divisions.Where(d => divIds.Contains(d.Id)).ExecuteDeleteAsync();
        await db.Leagues.Where(l => l.Id == ligueId).ExecuteDeleteAsync();
        await tx.CommitAsync();

        logger.LogInformation("Ligue id={Id} supprimée avec toutes ses données", ligueId);
    }

    public async Task<List<LeagueCommissioner>> GetCommissairesDeLigueAsync(int ligueId)
        => await db.LeagueCommissioners
            .Include(lc => lc.User)
            .Where(lc => lc.LeagueId == ligueId)
            .OrderBy(lc => lc.AssigneLe)
            .ToListAsync();

    public async Task PromouvoirCommissaireDeLigueAsync(int ligueId, string userId, string assignePar)
    {
        var existe = await db.LeagueCommissioners.AnyAsync(lc => lc.LeagueId == ligueId && lc.UserId == userId);
        if (existe) return;
        db.LeagueCommissioners.Add(new LeagueCommissioner
        {
            LeagueId = ligueId,
            UserId = userId,
            AssignePar = assignePar
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Coach id={UserId} promu commissaire de la ligue {LigueId} par {AssignePar}", userId, ligueId, assignePar);
    }

    public async Task RetirerCommissaireDeLigueAsync(int ligueId, string userId)
    {
        var entry = await db.LeagueCommissioners
            .FirstOrDefaultAsync(lc => lc.LeagueId == ligueId && lc.UserId == userId);
        if (entry is null) return;
        db.LeagueCommissioners.Remove(entry);
        await db.SaveChangesAsync();
        logger.LogInformation("Coach id={UserId} retiré comme commissaire de la ligue {LigueId}", userId, ligueId);
    }
}
