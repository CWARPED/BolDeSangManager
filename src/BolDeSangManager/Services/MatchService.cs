using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class MatchService(
    ApplicationDbContext db,
    ILogger<MatchService> logger,
    GmailEmailSender emailSender,
    SettingsService settings)
{
    public async Task<Match?> GetMatchAsync(int matchId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.Coach)
            .Include(m => m.EquipeDomicile).ThenInclude(e => e.TeamType)
            .Include(m => m.EquipeExterieur).ThenInclude(e => e.TeamType)
            .Include(m => m.Feuille).ThenInclude(f => f!.RecordsJoueurs).ThenInclude(r => r.TeamPlayer).ThenInclude(p => p!.PlayerPosition)
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
        var bareme = XpBareme.ParDefaut(match.Division?.League?.Game?.Type ?? GameType.BloodBowl);
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

        // Variation des fans dévoués
        domicile.FansDevoues = Math.Max(1, domicile.FansDevoues + feuille.VariationFansDomicile);
        exterieur.FansDevoues = Math.Max(1, exterieur.FansDevoues + feuille.VariationFansExterieur);

        await db.SaveChangesAsync();
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

    public async Task ValiderFeuilleAsync(int matchId, string notesCommissaire)
    {
        var feuille = await db.MatchSheets.FirstOrDefaultAsync(f => f.MatchId == matchId)
            ?? throw new InvalidOperationException("Feuille introuvable");

        var match = await db.Matches.FindAsync(matchId)
            ?? throw new InvalidOperationException("Match introuvable");

        feuille.ValideParCommissaire = true;
        feuille.NotesCommissaire = notesCommissaire;
        match.Statut = MatchStatus.Termine;

        await db.SaveChangesAsync();
        logger.LogInformation("Match id={MatchId} validé par le commissaire", matchId);
    }

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
        dom.FansDevoues = Math.Max(1, dom.FansDevoues - feuille.VariationFansDomicile);
        ext.FansDevoues = Math.Max(1, ext.FansDevoues - feuille.VariationFansExterieur);

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
        var baremeModif = XpBareme.ParDefaut(gameType);
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
    }

    public async Task<List<Match>> GetMatchsEnAttenteValidationAsync(int ligueId) =>
        await db.Matches
            .Include(m => m.EquipeDomicile)
            .Include(m => m.EquipeExterieur)
            .Include(m => m.Division)
            .Include(m => m.Feuille)
            .Where(m => m.Division!.LeagueId == ligueId
                     && (m.Statut == MatchStatus.FeuilleEnSaisie
                      || m.Statut == MatchStatus.ValidationCompetences))
            .ToListAsync();

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
