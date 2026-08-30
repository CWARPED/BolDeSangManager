using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <param name="notifications">
/// Diffusion temps réel aux écrans de match ouverts. Optionnel : les tests
/// unitaires instancient le service sans lui, et une notification manquante ne
/// doit jamais faire échouer une opération métier.
/// </param>
public class MatchService(
    ApplicationDbContext db,
    ILogger<MatchService> logger,
    GmailEmailSender emailSender,
    SettingsService settings,
    LeagueNotificationService? notifications = null)
{
    /// <summary>
    /// Prévient les écrans ouverts qu'un match vient de changer. Appelé APRÈS
    /// le SaveChanges : on ne diffuse que des faits déjà en base.
    /// </summary>
    private Task NotifierMatchAsync(int matchId) =>
        notifications?.NotifierMatchAsync(matchId) ?? Task.CompletedTask;

    /// <summary>
    /// Lit un match en ignorant le cache de suivi EF.
    ///
    /// ⚠️ Indispensable au rafraîchissement temps réel : le DbContext est scopé
    /// au circuit Blazor, donc à l'onglet, et il vit aussi longtemps que la
    /// page. Sans ce vidage EF rend l'instance chargée à l'ouverture, et
    /// l'écran se « rafraîchit » sur des données périmées.
    /// </summary>
    public async Task<Match?> GetMatchFraisAsync(int matchId)
    {
        db.ChangeTracker.Clear();
        return await GetMatchAsync(matchId);
    }

    public async Task<Match?> GetMatchAsync(int matchId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.TeamType)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.TeamType)
            .Include(m => m.Feuille).ThenInclude(f => f!.RecordsJoueurs).ThenInclude(r => r.TeamPlayer).ThenInclude(p => p!.PlayerPosition)
            // R7 : accès de catégorie du poste, pour filtrer les compétences à l'après-match
            .Include(m => m.Feuille).ThenInclude(f => f!.RecordsJoueurs).ThenInclude(r => r.TeamPlayer)
                .ThenInclude(p => p!.PlayerPosition).ThenInclude(pp => pp!.AccesCategories)
            .Include(m => m.Division).ThenInclude(d => d!.League)
            .FirstOrDefaultAsync(m => m.Id == matchId);

    public async Task<List<Match>> GetMatchsEquipeAsync(int teamId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.Division).ThenInclude(d => d!.League)
            .Include(m => m.Feuille)
            .Where(m => m.EquipeDomicileId == teamId || m.EquipeExterieurId == teamId)
            .OrderBy(m => m.Ronde)
            .ToListAsync();

    /// <summary>
    /// Matchs de PLUSIEURS équipes en une seule requête.
    ///
    /// Les écrans transverses (accueil, « Mes matchs ») bouclaient sur les équipes
    /// du coach en appelant <see cref="GetMatchsEquipeAsync"/> à chaque tour :
    /// 15 équipes = 15 allers-retours SQL pour un seul affichage (motif N+1).
    /// Ici tout est ramené d'un coup, quel que soit le nombre d'équipes.
    /// </summary>
    public async Task<List<Match>> GetMatchsEquipesAsync(IReadOnlyCollection<int> teamIds)
    {
        if (teamIds.Count == 0) return [];

        return await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.Division).ThenInclude(d => d!.League)
            .Include(m => m.Feuille)
            .Where(m => teamIds.Contains(m.EquipeDomicileId)
                     || teamIds.Contains(m.EquipeExterieurId))
            .OrderBy(m => m.Ronde)
            .ToListAsync();
    }

    /// <summary>
    /// Tous les matchs d'une ligue (#2) — sert à évaluer la règle du mode brouillard
    /// côté serveur, y compris sur accès direct à une page de match.
    /// </summary>
    public async Task<List<Match>> GetMatchsLigueAsync(int ligueId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Division)
            .Where(m => m.Division!.LeagueId == ligueId)
            .OrderBy(m => m.Ronde)
            .ToListAsync();

    /// <summary>
    /// Date plancher pour programmer un match : la date du dernier match déjà
    /// programmé dans une ronde ANTÉRIEURE de la même ligue.
    ///
    /// Sert à éviter le cas signalé — poser la ronde 5 avant la ronde 4 — en
    /// faisant démarrer le calendrier au bon endroit plutôt qu'au mois courant.
    /// Renvoie null quand aucune ronde précédente n'est datée (rien à borner),
    /// et ignore les playoffs, qui suivent leur propre logique.
    /// </summary>
    public async Task<DateTime?> GetDatePlancherAsync(int matchId)
    {
        var match = await db.Matches
            .Include(m => m.Division)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match?.Division is null || match.EstPlayoff) return null;

        return await db.Matches
            .Where(m => m.Division!.LeagueId == match.Division.LeagueId
                        && !m.EstPlayoff
                        && m.Ronde < match.Ronde
                        && m.DateProgrammee != null)
            .MaxAsync(m => (DateTime?)m.DateProgrammee);
    }

    /// <summary>
    /// Fixe (ou efface) la date et le lieu d'un match (#1).
    ///
    /// Saisie libre : les deux coaches concernés et les commissaires peuvent la
    /// modifier — on fait confiance à l'entente entre joueurs. L'habilitation est
    /// vérifiée ici et pas seulement masquée dans l'UI.
    /// </summary>
    /// <param name="date">Date/heure en UTC, ou null pour effacer.</param>
    public async Task ProgrammerMatchAsync(int matchId, DateTime? date, string lieu, string userId,
        bool estCommissaire = false)
    {
        var match = await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        var estCoach = match.EquipeDomicile?.CoachId == userId
                    || match.EquipeExterieur?.CoachId == userId;

        if (!estCoach && !estCommissaire)
            throw new UnauthorizedAccessException(
                "Seuls les deux coaches du match et les commissaires peuvent fixer la date.");

        if (match.Statut is MatchStatus.Termine or MatchStatus.Concede)
            throw new InvalidOperationException("Ce match est déjà joué : sa date ne peut plus être modifiée.");

        // Ordre des rondes : une ronde ne peut pas être posée avant une ronde
        // antérieure déjà datée. Vérifié ICI et pas seulement grisé dans le
        // calendrier, la saisie de date étant éditable au clavier.
        if (date is DateTime voulue)
        {
            var plancher = await GetDatePlancherAsync(matchId);
            if (plancher is DateTime p && voulue.Date < p.Date)
                throw new InvalidOperationException(
                    $"La ronde précédente se joue le {p.ToLocalTime():dd/MM/yyyy} : "
                    + "ce match ne peut pas être programmé avant.");
        }

        match.DateProgrammee = date;
        match.Lieu = (lieu ?? string.Empty).Trim();
        await db.SaveChangesAsync();

        logger.LogInformation("Match id={MatchId} programmé au {Date} à '{Lieu}' par {UserId}",
            matchId, date?.ToString("u") ?? "(non fixée)", match.Lieu, userId);

        await NotifierMatchAsync(matchId);
    }

    public async Task<MatchSheet> SaisirFeuilleMatchAsync(int matchId, MatchSheet feuille,
        List<MatchPlayerRecord> records, string saisiParId)
    {
        var match = await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Division).ThenInclude(d => d!.League)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        feuille.MatchId = matchId;
        feuille.SaisiParId = saisiParId;
        feuille.SaisiLe = DateTime.UtcNow;

        db.MatchSheets.Add(feuille);
        await db.SaveChangesAsync();

        // XP : depuis R4 la valeur saisie par le coach fait foi. Le barème ne sert
        // que de valeur par défaut (pré-remplie côté UI) ; on ne recalcule ici que
        // si l'appelant n'a rien fourni, pour rester compatible avec d'anciens appels.
        var bareme = XpBareme.DeLigue(match.Division?.League,
            match.Division?.League?.Game?.Type ?? GameType.BloodBowl);
        foreach (var record in records)
        {
            record.MatchSheetId = feuille.Id;
            if (record.PspGagnes <= 0)
                record.PspGagnes = bareme.Calculer(record);
            db.MatchPlayerRecords.Add(record);
        }
        await db.SaveChangesAsync();

        // Mettre à jour les stats des équipes
        await MettreAJourStatsEquipesAsync(match, feuille);

        // Purger les « rate le prochain match » : ce match EST le prochain match
        // des deux équipes. À faire impérativement AVANT TraiterBlessuresAsync,
        // qui pose les sanctions issues de cette rencontre-ci.
        await PurgerManqueSuivantMatchAsync(match);

        // Traiter les blessures
        await TraiterBlessuresAsync(records, matchId);

        // Ajouter les PSP aux joueurs
        await MettreAJourPSPJoueursAsync(records);

        // Mettre à jour le statut du match
        match.ScoreDomicile = feuille.TouchdownsDomicile;
        match.ScoreExterieur = feuille.TouchdownsExterieur;
        match.DateJouee = DateTime.UtcNow;
        match.Statut = MatchStatus.FeuilleEnSaisie; // en attente de confirmation adverse

        await db.SaveChangesAsync();
        logger.LogInformation("Feuille saisie pour match id={MatchId} : {Dom} {TdDom}-{TdExt} {Ext}, {NbRecords} joueurs",
            matchId, match.EquipeDomicile?.Nom, feuille.TouchdownsDomicile, feuille.TouchdownsExterieur, match.EquipeExterieur?.Nom, records.Count);

        await EnvoyerEmailConfirmationFeuilleAsync(match, matchId, saisiParId);
        await NotifierMatchAsync(matchId);
        return feuille;
    }

    public async Task ConfirmerFeuilleCoachAsync(int matchId, string coachId)
    {
        var match = await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Feuille)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        if (match.Statut != MatchStatus.FeuilleEnSaisie)
            throw new InvalidOperationException("Ce match n'est pas en attente de confirmation.");

        var feuille = match.Feuille
            ?? throw new InvalidOperationException("Feuille introuvable");

        bool estDomicile = match.EquipeDomicile?.CoachId == coachId;
        bool estExterieur = match.EquipeExterieur?.CoachId == coachId;
        if (!estDomicile && !estExterieur)
            throw new InvalidOperationException("Vous n'êtes pas coach de ce match.");

        if (feuille.SaisiParId == coachId)
            throw new InvalidOperationException("Vous ne pouvez pas confirmer votre propre saisie — attendez que l'adversaire confirme.");

        match.Statut = MatchStatus.ValidationCompetences;
        await db.SaveChangesAsync();
        logger.LogInformation("Feuille du match id={MatchId} confirmée par coach id={CoachId}", matchId, coachId);

        await EnvoyerEmailApresMatchAsync(match, matchId, excludeCoachId: coachId);
        await NotifierMatchAsync(matchId);
    }

    private async Task EnvoyerEmailConfirmationFeuilleAsync(Match match, int matchId, string saisiParId)
    {
        try
        {
            var urlBase = (await settings.GetAsync(SettingsService.CleUrlExterne) ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(urlBase)) return;

            var autreCoachId = match.EquipeDomicile?.CoachId == saisiParId
                ? match.EquipeExterieur?.CoachId
                : match.EquipeDomicile?.CoachId;
            if (autreCoachId is null) return;

            var autreCoach = await db.Users.FirstOrDefaultAsync(u => u.Id == autreCoachId);
            if (autreCoach?.Email is null) return;

            var lien = $"{urlBase}/matchs/{matchId}/feuille";
            var dom = match.EquipeDomicile?.Nom ?? "";
            var ext = match.EquipeExterieur?.Nom ?? "";
            var score = $"{match.ScoreDomicile} – {match.ScoreExterieur}";

            await emailSender.EnvoyerNotificationMatchAsync(
                autreCoach.Email,
                $"Feuille à confirmer — {dom} vs {ext}",
                "Feuille de match à confirmer",
                $"La feuille du match <b>{dom} {score} {ext}</b> a été saisie. Veuillez consulter les statistiques et confirmer le résultat.",
                lien, "Voir et confirmer le match",
                footer: "Si le résultat vous semble incorrect, contactez le commissaire de la ligue.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Échec de l'envoi d'email de notification feuille pour match id={MatchId}", matchId);
        }
    }

    private async Task EnvoyerEmailApresMatchAsync(Match match, int matchId, string? excludeCoachId = null)
    {
        try
        {
            var urlBase = (await settings.GetAsync(SettingsService.CleUrlExterne) ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(urlBase)) return;

            var dom = match.EquipeDomicile?.Nom ?? "";
            var ext = match.EquipeExterieur?.Nom ?? "";
            var score = $"{match.ScoreDomicile} – {match.ScoreExterieur}";
            var lien = $"{urlBase}/matchs/{matchId}/apres-match";

            var coachIds = new[] { match.EquipeDomicile?.CoachId, match.EquipeExterieur?.CoachId }
                .Where(id => id is not null && id != excludeCoachId).ToList();
            var coaches = await db.Users.Where(u => coachIds.Contains(u.Id)).ToListAsync();

            foreach (var coach in coaches)
            {
                if (coach.Email is null) continue;
                await emailSender.EnvoyerNotificationMatchAsync(
                    coach.Email,
                    $"Match confirmé — passez à l'après-match ({dom} vs {ext})",
                    "Match confirmé — après-match disponible",
                    $"Le match <b>{dom} {score} {ext}</b> a été confirmé par les deux coaches. Effectuez votre phase d'après-match : gains de compétences, recrutements, relances.",
                    lien, "Faire mon après-match");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Échec de l'envoi d'emails après-match pour match id={MatchId}", matchId);
        }
    }

    /// <summary>
    /// Calcule les Points Star Player gagnés selon le LRB Saison 3 / Dungeon Bowl Edition 2022.
    /// </summary>
    /// <param name="record">Statistiques du joueur sur le match</param>
    /// <param name="gameType">Type de jeu (TD = 5 en DungeonBowl, 3 sinon)</param>
    public static int CalculerPSPPublic(MatchPlayerRecord record, GameType gameType)
    {
        int pspParTd = gameType == GameType.DungeonBowl ? 5 : 3;
        int psp = 0;
        psp += record.Touchdowns * pspParTd;
        psp += record.Passes * 1;
        psp += record.Interceptions * 2;
        psp += record.EliminationsInfligees * 2;
        if (record.EstMVP) psp += 4;
        return psp;
    }

    private static int CalculerPSP(MatchPlayerRecord record, GameType gameType)
        => CalculerPSPPublic(record, gameType);

    /// <summary>
    /// Lève l'indisponibilité « rate le prochain match » des joueurs des deux
    /// équipes : la sanction est purgée par le match qu'on est en train de saisir.
    ///
    /// Auparavant seule la phase de repos (entre saison régulière et play-offs)
    /// remettait ce drapeau à zéro, ce qui masquait le problème dans les formats
    /// classiques. En Open il n'y a jamais de phase de repos : sans cette purge,
    /// un joueur blessé resterait indisponible pour toujours.
    ///
    /// À appeler AVANT TraiterBlessuresAsync, sinon on effacerait aussitôt les
    /// sanctions issues du match courant.
    /// </summary>
    private async Task PurgerManqueSuivantMatchAsync(Match match)
    {
        var equipeIds = new[] { match.EquipeDomicileId, match.EquipeExterieurId };

        var joueurs = await db.TeamPlayers
            .Where(j => equipeIds.Contains(j.TeamId) && j.ManqueSuivantMatch)
            .ToListAsync();

        if (joueurs.Count == 0) return;

        foreach (var j in joueurs)
            j.ManqueSuivantMatch = false;

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Match id={MatchId} : {Nb} joueur(s) ont purgé leur « rate le prochain match »",
            match.Id, joueurs.Count);
    }

    private async Task MettreAJourStatsEquipesAsync(Match match, MatchSheet feuille)
    {
        var domicile = match.EquipeDomicile;
        var exterieur = match.EquipeExterieur;

        domicile.NombreMatchsJoues++;
        exterieur.NombreMatchsJoues++;
        domicile.TouchdownsMarques += feuille.TouchdownsDomicile;
        domicile.TouchdownsConcedes += feuille.TouchdownsExterieur;
        exterieur.TouchdownsMarques += feuille.TouchdownsExterieur;
        exterieur.TouchdownsConcedes += feuille.TouchdownsDomicile;
        domicile.EliminationsInfligees += feuille.EliminationsDomicile;
        exterieur.EliminationsInfligees += feuille.EliminationsExterieur;

        // Points de ligue (victoire = 3, nul = 1, défaite = 0)
        if (feuille.TouchdownsDomicile > feuille.TouchdownsExterieur)
        {
            domicile.PointsLigue += 3;
            domicile.NombreVictoires++;
            exterieur.NombreDefaites++;
        }
        else if (feuille.TouchdownsDomicile < feuille.TouchdownsExterieur)
        {
            exterieur.PointsLigue += 3;
            exterieur.NombreVictoires++;
            domicile.NombreDefaites++;
        }
        else
        {
            domicile.PointsLigue += 1;
            exterieur.PointsLigue += 1;
            domicile.NombreNuls++;
            exterieur.NombreNuls++;
        }

        // Calcul des gains (règle LRB : 2D6 * 10000 + TDs * 10000, simplifié ici)
        domicile.Tresorerie += feuille.GainsDomicile;
        exterieur.Tresorerie += feuille.GainsExterieur;

        // Variation des fans dévoués.
        // Le plafond de ligue est DUR : il s'applique aussi aux fans gagnés par
        // les résultats, pas seulement aux achats. On mémorise la variation
        // RÉELLEMENT appliquée, car l'annulation doit soustraire celle-ci et non
        // la variation théorique — sinon l'écrêtage fait disparaître des fans.
        var plafondFans = await db.LeagueStaffTypes
            .Where(l => l.LeagueId == domicile.LeagueId && l.Nom == StaffService.NomFans)
            .Select(l => l.MaxLigue)
            .FirstOrDefaultAsync();

        feuille.VariationFansDomicileAppliquee =
            await AppliquerVariationFansAsync(domicile, feuille.VariationFansDomicile, plafondFans);
        feuille.VariationFansExterieurAppliquee =
            await AppliquerVariationFansAsync(exterieur, feuille.VariationFansExterieur, plafondFans);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Applique une variation de fans à une équipe en respectant le plancher (1)
    /// et le plafond de ligue, et retourne la variation réellement appliquée.
    /// </summary>
    private async Task<int> AppliquerVariationFansAsync(Team equipe, int variation, int? plafond)
    {
        var ligne = await db.TeamStaffs
            .Include(t => t.LeagueStaffType)
            .FirstOrDefaultAsync(t => t.TeamId == equipe.Id
                                   && t.LeagueStaffType.Nom == StaffService.NomFans);

        var avant = ligne?.Quantite ?? equipe.FansDevoues;
        var apres = StaffService.Ecreter(avant + variation, minimum: 1, maxLigue: plafond);

        if (ligne is not null) ligne.Quantite = apres;
        else if (apres > 0)
        {
            var type = await db.LeagueStaffTypes.FirstOrDefaultAsync(
                l => l.LeagueId == equipe.LeagueId && l.Nom == StaffService.NomFans);
            if (type is not null)
                db.TeamStaffs.Add(new TeamStaff
                {
                    TeamId = equipe.Id, LeagueStaffTypeId = type.Id, Quantite = apres
                });
        }

        // Colonne historique tenue à jour en miroir : les anciens écrans et les
        // exports qui la lisent encore restent cohérents.
        equipe.FansDevoues = apres;

        return apres - avant;
    }

    /// <summary>
    /// Retire d'une équipe une variation de fans précédemment appliquée.
    /// </summary>
    private async Task AnnulerVariationFansAsync(Team equipe, int variationAppliquee)
    {
        var ligne = await db.TeamStaffs
            .Include(t => t.LeagueStaffType)
            .FirstOrDefaultAsync(t => t.TeamId == equipe.Id
                                   && t.LeagueStaffType.Nom == StaffService.NomFans);

        var avant = ligne?.Quantite ?? equipe.FansDevoues;
        var apres = Math.Max(1, avant - variationAppliquee);

        if (ligne is not null) ligne.Quantite = apres;
        equipe.FansDevoues = apres;
    }

    private async Task TraiterBlessuresAsync(List<MatchPlayerRecord> records, int matchId)
    {
        var avecBlessure = records.Where(r => r.Blessure.HasValue).ToList();
        if (avecBlessure.Count == 0) return;

        var ids = avecBlessure.Select(r => r.TeamPlayerId).Distinct().ToList();
        var joueurs = await db.TeamPlayers.Where(j => ids.Contains(j.Id)).ToDictionaryAsync(j => j.Id);

        foreach (var record in avecBlessure)
        {
            if (!joueurs.TryGetValue(record.TeamPlayerId, out var joueur)) continue;

            db.PlayerInjuries.Add(new PlayerInjury
            {
                TeamPlayerId = record.TeamPlayerId,
                MatchId = matchId,
                Type = record.Blessure!.Value,
                StatAffectee = record.StatAffectee,
                Date = DateTime.UtcNow,
                Description = record.Blessure switch
                {
                    InjuryType.ManqueSuivant       => "Rate le prochain match",
                    InjuryType.BlessurePersistante => $"Blessure persistante : {record.StatAffectee}",
                    InjuryType.RetraiteTemporaire  => "Retraite temporaire",
                    InjuryType.Mort                => "Mort au combat",
                    _                              => ""
                }
            });

            switch (record.Blessure)
            {
                case InjuryType.ManqueSuivant:
                    joueur.ManqueSuivantMatch = true;
                    logger.LogInformation("Joueur {NomJoueur} (id={Id}) rate le prochain match", joueur.Nom, joueur.Id);
                    break;
                case InjuryType.BlessurePersistante when record.StatAffectee.HasValue:
                    AppliquerReductionCaracteristique(joueur, record.StatAffectee.Value);
                    logger.LogWarning("Joueur {NomJoueur} (id={Id}) : séquelle sur {Stat}", joueur.Nom, joueur.Id, record.StatAffectee.Value);
                    break;
                case InjuryType.RetraiteTemporaire:
                    joueur.EstRetraite = true;
                    logger.LogWarning("Joueur {NomJoueur} (id={Id}) mis à la retraite temporaire", joueur.Nom, joueur.Id);
                    break;
                case InjuryType.Mort:
                    joueur.EstMort = true;
                    logger.LogWarning("Joueur {NomJoueur} (id={Id}) est mort au combat !", joueur.Nom, joueur.Id);
                    break;
            }

            // Un capitaine mort ou parti perd son titre : sinon la compétence
            // qu'il accorde resterait affichée sur un joueur absent de
            // l'effectif. Le coach en redésigne un librement.
            if (joueur.EstCapitaine && (joueur.EstMort || joueur.EstRetraite))
            {
                joueur.EstCapitaine = false;
                logger.LogInformation(
                    "Titre de capitaine retiré au joueur {NomJoueur} (id={Id})", joueur.Nom, joueur.Id);
            }
        }
        await db.SaveChangesAsync();
    }

    private static void AppliquerReductionCaracteristique(TeamPlayer joueur, AffectedStat stat)
    {
        switch (stat)
        {
            case AffectedStat.Mouvement: joueur.ModMouvement--; break;
            case AffectedStat.Force: joueur.ModForce--; break;
            case AffectedStat.Agilite: joueur.ModAgilite--; break;
            case AffectedStat.CapacitePasse: joueur.ModCapacitePasse--; break;
            case AffectedStat.Armure: joueur.ModArmure--; break;
        }
    }

    private async Task MettreAJourPSPJoueursAsync(List<MatchPlayerRecord> records)
    {
        var ids = records.Select(r => r.TeamPlayerId).Distinct().ToList();
        var joueurs = await db.TeamPlayers.Where(j => ids.Contains(j.Id)).ToDictionaryAsync(j => j.Id);

        foreach (var record in records)
        {
            if (joueurs.TryGetValue(record.TeamPlayerId, out var joueur))
                joueur.PointsStarPlayer += record.PspGagnes;
        }
        await db.SaveChangesAsync();
    }

    // ⚠️ ValiderFeuilleAsync (« forcer la clôture par le commissaire ») a été
    // retirée : la validation d'un match par un commissaire n'est plus voulue.
    // Ne pas confondre avec ConfirmerFeuilleCoachAsync, qui est la confirmation
    // de la saisie par le coach ADVERSE — celle-là reste, c'est le circuit du
    // mail « Feuille à confirmer » (voir EnvoyerEmailConfirmationFeuilleAsync).
    // Un match se clôture désormais tout seul quand les deux coaches ont validé
    // leur après-match (voir ValiderApresMatchCoachAsync).

    public async Task ValiderApresMatchCoachAsync(
        int matchId, int teamId,
        List<(int joueurId, int skillId, bool estPrincipale, int xpDepensee)> competences,
        List<(int positionId, string nom, int numero)> nouveauxJoueurs,
        int nouvellesRelances,
        TeamService teamService)
    {
        var match = await db.Matches
            .Include(m => m.Feuille)
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.TeamType)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.TeamType)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        if (match.Statut != MatchStatus.ValidationCompetences)
            throw new InvalidOperationException("Ce match n'est pas en phase d'après-match.");

        var feuille = match.Feuille
            ?? throw new InvalidOperationException("Feuille introuvable");

        bool estDomicile = match.EquipeDomicileId == teamId;
        bool estExterieur = match.EquipeExterieurId == teamId;
        if (!estDomicile && !estExterieur)
            throw new InvalidOperationException("Cette équipe ne participe pas à ce match.");

        if (estDomicile && feuille.ApresMatchDomicileValide)
            throw new InvalidOperationException("L'après-match a déjà été validé pour cette équipe.");
        if (estExterieur && feuille.ApresMatchExterieurValide)
            throw new InvalidOperationException("L'après-match a déjà été validé pour cette équipe.");

        // Améliorations (Sélection Primaire si principale, Secondaire sinon)
        foreach (var (joueurId, skillId, estPrincipale, xpDepensee) in competences)
        {
            var type = estPrincipale ? ImprovementType.SelectionPrimaire : ImprovementType.SelectionSecondaire;
            await teamService.AppliquerAmeliorationAsync(joueurId, type, skillId: skillId,
                matchSheetId: feuille.Id, xpDepensee: xpDepensee);
        }

        // Recruter nouveaux joueurs
        foreach (var (positionId, nom, numero) in nouveauxJoueurs)
            await teamService.RecruterJoueurAsync(teamId, positionId, nom, numero);

        // Acheter relances
        if (nouvellesRelances > 0)
        {
            var equipe = await db.Teams
                .Include(t => t.TeamType)
                .FirstAsync(t => t.Id == teamId);
            var coutRelance = (equipe.TeamType?.CoutRelance ?? 50_000) * 2; // règle ligue : double du prix normal
            var total = nouvellesRelances * coutRelance;
            if (equipe.Tresorerie < total)
                throw new InvalidOperationException("Fonds insuffisants pour acheter les relances.");
            var maxRelances = 8;
            if (equipe.NombreRelances + nouvellesRelances > maxRelances)
                throw new InvalidOperationException($"Maximum {maxRelances} relances par équipe.");
            equipe.Tresorerie -= total;
            equipe.NombreRelances += nouvellesRelances;
            await db.SaveChangesAsync();
        }

        // Marquer la validation
        if (estDomicile) feuille.ApresMatchDomicileValide = true;
        else feuille.ApresMatchExterieurValide = true;

        // Auto-clôture quand les deux coaches ont validé
        if (feuille.ApresMatchDomicileValide && feuille.ApresMatchExterieurValide)
        {
            match.Statut = MatchStatus.Termine;
            logger.LogInformation("Match id={MatchId} terminé — après-match validé par les deux coaches", matchId);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Après-match validé par coach de l'équipe id={TeamId} (match id={MatchId})", teamId, matchId);

        // L'autre coach doit voir arriver cette validation : c'est elle qui
        // declenche l'auto-cloture quand les deux ont valide.
        await NotifierMatchAsync(matchId);
    }

    public async Task ModifierFeuilleAsync(int matchId, MatchSheet feuilleModifiee, List<MatchPlayerRecord> nouveauxRecords)
    {
        var match = await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Division).ThenInclude(d => d!.League).ThenInclude(l => l!.Game)
            .Include(m => m.Feuille).ThenInclude(f => f!.RecordsJoueurs)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        var feuille = match.Feuille
            ?? throw new InvalidOperationException("Feuille introuvable");

        var dom = match.EquipeDomicile!;
        var ext = match.EquipeExterieur!;

        // Inverser stats d'équipes
        dom.NombreMatchsJoues--;
        ext.NombreMatchsJoues--;
        dom.TouchdownsMarques    -= feuille.TouchdownsDomicile;
        dom.TouchdownsConcedes   -= feuille.TouchdownsExterieur;
        ext.TouchdownsMarques    -= feuille.TouchdownsExterieur;
        ext.TouchdownsConcedes   -= feuille.TouchdownsDomicile;
        dom.EliminationsInfligees -= feuille.EliminationsDomicile;
        ext.EliminationsInfligees -= feuille.EliminationsExterieur;

        if (feuille.TouchdownsDomicile > feuille.TouchdownsExterieur)
        { dom.PointsLigue -= 3; dom.NombreVictoires--; ext.NombreDefaites--; }
        else if (feuille.TouchdownsDomicile < feuille.TouchdownsExterieur)
        { ext.PointsLigue -= 3; ext.NombreVictoires--; dom.NombreDefaites--; }
        else
        { dom.PointsLigue--; ext.PointsLigue--; dom.NombreNuls--; ext.NombreNuls--; }

        dom.Tresorerie -= feuille.GainsDomicile;
        ext.Tresorerie -= feuille.GainsExterieur;
        // Annulation des fans : on soustrait la variation RÉELLEMENT appliquée,
        // pas la variation théorique. Avec un plafond (ou le plancher à 1), les
        // deux diffèrent — soustraire la théorique ferait disparaître des fans à
        // chaque annulation. Repli sur la théorique pour les feuilles saisies
        // avant l'ajout de la colonne.
        await AnnulerVariationFansAsync(
            dom, feuille.VariationFansDomicileAppliquee ?? feuille.VariationFansDomicile);
        await AnnulerVariationFansAsync(
            ext, feuille.VariationFansExterieurAppliquee ?? feuille.VariationFansExterieur);

        // Inverser PSP des joueurs (chargement groupé)
        var pspIds = feuille.RecordsJoueurs.Select(r => r.TeamPlayerId).Distinct().ToList();
        var joueursPsp = await db.TeamPlayers.Where(j => pspIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id);
        foreach (var old in feuille.RecordsJoueurs)
        {
            if (joueursPsp.TryGetValue(old.TeamPlayerId, out var j))
                j.PointsStarPlayer = Math.Max(0, j.PointsStarPlayer - old.PspGagnes);
        }

        // Supprimer les Improvements liés à cette feuille et inverser la hausse de valeur
        var oldImprovements = await db.PlayerImprovements
            .Where(pi => pi.MatchSheetId == feuille.Id)
            .ToListAsync();

        foreach (var imp in oldImprovements)
        {
            var j = await db.TeamPlayers.FindAsync(imp.TeamPlayerId);
            if (j is null) continue;

            // Inverser la hausse de valeur
            j.ValeurActuelle = Math.Max(0, j.ValeurActuelle - imp.ValeurHausse);

            // R4 : restituer l'XP dépensée pour cette amélioration, sinon elle
            // serait perdue (l'XP gagnée du match est retirée par ailleurs).
            j.PointsStarPlayer += imp.XpDepensee;

            // Inverser le mod de stat éventuel
            if (imp.StatAmelioree.HasValue)
            {
                switch (imp.StatAmelioree.Value)
                {
                    case AffectedStat.Mouvement: j.ModMouvement--; break;
                    case AffectedStat.Force: j.ModForce--; break;
                    case AffectedStat.Agilite: j.ModAgilite--; break;
                    case AffectedStat.CapacitePasse: j.ModCapacitePasse--; break;
                    case AffectedStat.Armure: j.ModArmure--; break;
                }
            }

            // Supprimer la compétence acquise associée (si applicable)
            if (imp.SkillId.HasValue)
            {
                var skillAcquise = await db.TeamPlayerSkills.FirstOrDefaultAsync(
                    tps => tps.TeamPlayerId == imp.TeamPlayerId
                        && tps.SkillId == imp.SkillId.Value
                        && !tps.EstCompetenceDepart);
                if (skillAcquise is not null)
                    db.TeamPlayerSkills.Remove(skillAcquise);
            }
        }
        db.PlayerImprovements.RemoveRange(oldImprovements);
        await db.SaveChangesAsync();

        // Inverser blessures (chargement groupé)
        var anciennesBlessures = await db.PlayerInjuries.Where(b => b.MatchId == matchId).ToListAsync();
        var blessureIds = anciennesBlessures.Select(b => b.TeamPlayerId).Distinct().ToList();
        var joueursBlessures = await db.TeamPlayers.Where(j => blessureIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id);
        foreach (var b in anciennesBlessures)
        {
            if (!joueursBlessures.TryGetValue(b.TeamPlayerId, out var j)) continue;
            switch (b.Type)
            {
                case InjuryType.ManqueSuivant:      j.ManqueSuivantMatch = false; break;
                case InjuryType.RetraiteTemporaire: j.EstRetraite = false; break;
                case InjuryType.Mort:               j.EstMort = false; break;
                case InjuryType.BlessurePersistante when b.StatAffectee.HasValue:
                    switch (b.StatAffectee.Value)
                    {
                        case AffectedStat.Mouvement:     j.ModMouvement++; break;
                        case AffectedStat.Force:         j.ModForce++; break;
                        case AffectedStat.Agilite:       j.ModAgilite++; break;
                        case AffectedStat.CapacitePasse: j.ModCapacitePasse++; break;
                        case AffectedStat.Armure:        j.ModArmure++; break;
                    }
                    break;
            }
        }
        db.PlayerInjuries.RemoveRange(anciennesBlessures);
        db.MatchPlayerRecords.RemoveRange(feuille.RecordsJoueurs);
        await db.SaveChangesAsync();

        // Appliquer la nouvelle feuille
        feuille.TouchdownsDomicile    = feuilleModifiee.TouchdownsDomicile;
        feuille.TouchdownsExterieur   = feuilleModifiee.TouchdownsExterieur;
        feuille.EliminationsDomicile  = feuilleModifiee.EliminationsDomicile;
        feuille.EliminationsExterieur = feuilleModifiee.EliminationsExterieur;
        feuille.GainsDomicile         = feuilleModifiee.GainsDomicile;
        feuille.GainsExterieur        = feuilleModifiee.GainsExterieur;
        feuille.VariationFansDomicile  = feuilleModifiee.VariationFansDomicile;
        feuille.VariationFansExterieur = feuilleModifiee.VariationFansExterieur;
        feuille.NotesCommissaire       = feuilleModifiee.NotesCommissaire;

        var gameType = match.Division?.League?.Game?.Type ?? GameType.BloodBowl;
        var baremeModif = XpBareme.DeLigue(match.Division?.League, gameType);
        foreach (var r in nouveauxRecords)
        {
            r.MatchSheetId = feuille.Id;
            if (r.PspGagnes <= 0)
                r.PspGagnes = baremeModif.Calculer(r);
            db.MatchPlayerRecords.Add(r);
        }
        await db.SaveChangesAsync();

        await MettreAJourStatsEquipesAsync(match, feuille);
        await TraiterBlessuresAsync(nouveauxRecords, matchId);
        await MettreAJourPSPJoueursAsync(nouveauxRecords);

        match.ScoreDomicile  = feuille.TouchdownsDomicile;
        match.ScoreExterieur = feuille.TouchdownsExterieur;
        match.Statut = MatchStatus.ValidationCompetences; // le commissaire qui modifie = confirmation directe
        feuille.ValideParCommissaire = false;
        feuille.ApresMatchDomicileValide = false;
        feuille.ApresMatchExterieurValide = false;
        await db.SaveChangesAsync();

        await EnvoyerEmailApresMatchAsync(match, matchId);
        logger.LogInformation("Feuille match id={MatchId} modifiée par commissaire", matchId);
        await NotifierMatchAsync(matchId);
    }

    // ⚠️ GetMatchsEnAttenteValidationAsync retirée avec la validation commissaire :
    // elle listait les matchs « en attente du commissaire », notion abandonnée.

    // Calcul simplifié des gains d'après-match selon les règles LRB
    public static (int gainsDomicile, int gainsExterieur) CalculerGains(
        int tdDomicile, int tdExterieur, int fansDomicile, int fansExterieur)
    {
        // Revenus de base : affluence * 10000 (simulé avec fans)
        var affluence = (fansDomicile + fansExterieur) * 5;
        var revenusBase = (int)(affluence * 10_000 * 0.5); // moitié pour chaque équipe simplifiée

        var gainsDomicile = revenusBase + tdDomicile * 10_000;
        var gainsExterieur = revenusBase + tdExterieur * 10_000;

        return (gainsDomicile, gainsExterieur);
    }
}
