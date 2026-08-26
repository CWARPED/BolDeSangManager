using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

/// <summary>
/// Staff d'équipe : fans dévoués, relances, coachs assistants, cheerleaders,
/// apothicaire — et tout ce que l'association ajoute elle-même dans les règles.
///
/// Trajet des valeurs, calqué sur le barème d'XP :
/// <c>StaffType</c> (règles) → <c>LeagueStaffType</c> (copie prise à la création
/// de la ligue, ajustable par le commissaire) → <c>TeamStaff</c> (quantité
/// détenue). Design COPIE : baisser un prix dans les règles ne rétro-modifie
/// jamais la VEA d'une ligue en cours.
/// </summary>
public class StaffService(ApplicationDbContext db, ILogger<StaffService> logger)
{
    /// <summary>Noms des cinq staff standard, matérialisés par la migration.</summary>
    public const string NomFans          = "Fans dévoués";
    public const string NomRelances      = "Relances";
    public const string NomCoachs        = "Coachs assistants";
    public const string NomCheerleaders  = "Cheerleaders";
    public const string NomApothicaire   = "Apothicaire";

    // ── Définitions côté RÈGLES ───────────────────────────────────────────────

    public async Task<List<StaffDefinition>> GetStaffTypesAsync(int rulesVersionId) =>
        await db.StaffTypes
            .Where(s => s.RulesVersionId == rulesVersionId)
            .OrderBy(s => s.Ordre).ThenBy(s => s.Nom)
            .ToListAsync();

    public async Task<StaffDefinition> AjouterStaffTypeAsync(StaffDefinition staff)
    {
        ValiderBornes(staff.Nom, staff.MinCreation, staff.MaxCreation, staff.MaxLigue);
        await ValiderNomUniqueAsync(staff.RulesVersionId, staff.Nom, exclureId: null);

        staff.Nom = staff.Nom.Trim();
        db.StaffTypes.Add(staff);
        await db.SaveChangesAsync();
        logger.LogInformation("Staff « {Nom} » ajouté à la version {Version}", staff.Nom, staff.RulesVersionId);
        return staff;
    }

    public async Task ModifierStaffTypeAsync(StaffDefinition modifie)
    {
        var staff = await db.StaffTypes.FindAsync(modifie.Id)
            ?? throw new InvalidOperationException("Staff introuvable");

        ValiderBornes(modifie.Nom, modifie.MinCreation, modifie.MaxCreation, modifie.MaxLigue);
        await ValiderNomUniqueAsync(staff.RulesVersionId, modifie.Nom, exclureId: staff.Id);

        staff.Nom                  = modifie.Nom.Trim();
        staff.Description          = modifie.Description;
        staff.Ordre                = modifie.Ordre;
        staff.EstActif             = modifie.EstActif;
        staff.Cout                 = modifie.Cout;
        staff.CoutDepuisTypeEquipe = modifie.CoutDepuisTypeEquipe;
        staff.MinCreation          = modifie.MinCreation;
        staff.MaxCreation          = modifie.MaxCreation;
        staff.MaxLigue             = modifie.MaxLigue;

        await db.SaveChangesAsync();
    }

    public async Task SupprimerStaffTypeAsync(int id)
    {
        var staff = await db.StaffTypes.FindAsync(id)
            ?? throw new InvalidOperationException("Staff introuvable");

        // Les copies de ligue survivent (FK SetNull) : supprimer une définition
        // ne doit pas vider le staff des ligues déjà lancées.
        db.StaffTypes.Remove(staff);
        await db.SaveChangesAsync();
        logger.LogInformation("Staff « {Nom} » supprimé de la version {Version}", staff.Nom, staff.RulesVersionId);
    }

    // ── Copie vers une ligue ──────────────────────────────────────────────────

    /// <summary>
    /// Recopie le staff d'une version de règles dans une ligue. Appelé à la
    /// création de la ligue. Idempotent : les types déjà copiés sont ignorés.
    /// </summary>
    public async Task CopierVersLigueAsync(int ligueId, int rulesVersionId)
    {
        var deja = await db.LeagueStaffTypes
            .Where(l => l.LeagueId == ligueId)
            .Select(l => l.Nom)
            .ToListAsync();

        var source = await GetStaffTypesAsync(rulesVersionId);
        var ajoutes = 0;

        foreach (var s in source)
        {
            if (deja.Any(n => string.Equals(n, s.Nom, StringComparison.OrdinalIgnoreCase))) continue;

            db.LeagueStaffTypes.Add(new LeagueStaffType
            {
                LeagueId             = ligueId,
                StaffTypeId          = s.Id,
                Nom                  = s.Nom,
                Description          = s.Description,
                Ordre                = s.Ordre,
                EstActif             = s.EstActif,
                // Le coût des relances vient de la race : ne pas le figer ici,
                // sinon toutes les races paieraient le même prix dans la ligue.
                Cout                 = s.CoutDepuisTypeEquipe ? 0 : s.Cout,
                CoutDepuisTypeEquipe = s.CoutDepuisTypeEquipe,
                MinCreation          = s.MinCreation,
                MaxCreation          = s.MaxCreation,
                MaxLigue             = s.MaxLigue
            });
            ajoutes++;
        }

        if (ajoutes > 0) await db.SaveChangesAsync();
        logger.LogInformation("Ligue {Id} : {Nb} type(s) de staff copiés depuis la version {Version}",
            ligueId, ajoutes, rulesVersionId);
    }

    public async Task<List<LeagueStaffType>> GetStaffLigueAsync(int ligueId, bool actifsSeulement = false)
    {
        var q = db.LeagueStaffTypes.Where(l => l.LeagueId == ligueId);
        if (actifsSeulement) q = q.Where(l => l.EstActif);
        return await q.OrderBy(l => l.Ordre).ThenBy(l => l.Nom).ToListAsync();
    }

    public async Task ModifierStaffLigueAsync(LeagueStaffType modifie)
    {
        var staff = await db.LeagueStaffTypes.FindAsync(modifie.Id)
            ?? throw new InvalidOperationException("Staff de ligue introuvable");

        ValiderBornes(modifie.Nom, modifie.MinCreation, modifie.MaxCreation, modifie.MaxLigue);

        staff.EstActif    = modifie.EstActif;
        staff.Cout        = modifie.Cout;
        staff.MinCreation = modifie.MinCreation;
        staff.MaxCreation = modifie.MaxCreation;
        staff.MaxLigue    = modifie.MaxLigue;

        // Un plafond abaissé bloque les ACHATS mais ne force aucune revente :
        // les équipes déjà au-dessus conservent leur staff.
        await db.SaveChangesAsync();
    }

    // ── Quantités détenues par une équipe ─────────────────────────────────────

    public async Task<List<TeamStaff>> GetStaffEquipeAsync(int teamId) =>
        await db.TeamStaffs
            .Include(t => t.LeagueStaffType)
            .Where(t => t.TeamId == teamId)
            .OrderBy(t => t.LeagueStaffType.Ordre).ThenBy(t => t.LeagueStaffType.Nom)
            .ToListAsync();

    /// <summary>Quantité détenue pour un type donné (0 si l'équipe n'en a pas).</summary>
    public async Task<int> GetQuantiteAsync(int teamId, int leagueStaffTypeId) =>
        await db.TeamStaffs
            .Where(t => t.TeamId == teamId && t.LeagueStaffTypeId == leagueStaffTypeId)
            .Select(t => (int?)t.Quantite)
            .FirstOrDefaultAsync() ?? 0;

    /// <summary>
    /// Fixe la quantité d'un staff pour une équipe, en vérifiant les bornes.
    /// </summary>
    /// <param name="aLaCreation">
    /// Vrai pendant la composition initiale de l'équipe : ce sont alors les
    /// bornes MinCreation/MaxCreation qui s'appliquent. Faux ensuite : seul le
    /// plafond de ligue (MaxLigue) limite, et il est DUR.
    /// </param>
    public async Task DefinirQuantiteAsync(int teamId, int leagueStaffTypeId, int quantite, bool aLaCreation)
    {
        if (quantite < 0)
            throw new InvalidOperationException("Une quantité de staff ne peut pas être négative.");

        var type = await db.LeagueStaffTypes.FindAsync(leagueStaffTypeId)
            ?? throw new InvalidOperationException("Staff de ligue introuvable");

        if (!type.EstActif && quantite > 0)
            throw new InvalidOperationException($"« {type.Nom} » n'est pas disponible dans cette ligue.");

        if (aLaCreation)
        {
            if (quantite < type.MinCreation)
                throw new InvalidOperationException(
                    $"« {type.Nom} » : minimum {type.MinCreation} à la création de l'équipe.");
            if (quantite > type.MaxCreation)
                throw new InvalidOperationException(
                    $"« {type.Nom} » : maximum {type.MaxCreation} à la création de l'équipe.");
        }

        if (type.MaxLigue is int plafond && quantite > plafond)
            throw new InvalidOperationException(
                $"« {type.Nom} » : plafond de {plafond} atteint pour cette ligue.");

        var ligne = await db.TeamStaffs
            .FirstOrDefaultAsync(t => t.TeamId == teamId && t.LeagueStaffTypeId == leagueStaffTypeId);

        if (ligne is null)
        {
            if (quantite == 0) return;
            db.TeamStaffs.Add(new TeamStaff
            {
                TeamId = teamId, LeagueStaffTypeId = leagueStaffTypeId, Quantite = quantite
            });
        }
        else
        {
            ligne.Quantite = quantite;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Applique un plafond DUR à une quantité : utilisé aussi bien pour un achat
    /// que pour un gain issu d'un résultat de match (variation de fans).
    /// Le plancher à 1 des fans dévoués est conservé via <paramref name="minimum"/>.
    /// </summary>
    public static int Ecreter(int valeur, int minimum, int? maxLigue)
    {
        var v = Math.Max(minimum, valeur);
        if (maxLigue is int plafond) v = Math.Min(v, plafond);
        return v;
    }

    /// <summary>
    /// Coût unitaire effectif d'un staff pour une équipe donnée : le prix des
    /// relances dépend de la race (TeamType.CoutRelance), pas de la ligue.
    /// </summary>
    public static int CoutUnitaire(LeagueStaffType type, TeamType? teamType) =>
        type.CoutDepuisTypeEquipe ? teamType?.CoutRelance ?? 50_000 : type.Cout;

    // ── Validation ────────────────────────────────────────────────────────────

    private static void ValiderBornes(string nom, int min, int max, int? maxLigue)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new InvalidOperationException("Le nom du staff est obligatoire.");
        if (nom.Trim().Length > 100)
            throw new InvalidOperationException("Le nom du staff ne peut pas dépasser 100 caractères.");
        if (min < 0 || max < 0)
            throw new InvalidOperationException("Les bornes ne peuvent pas être négatives.");
        if (min > max)
            throw new InvalidOperationException("Le minimum à la création ne peut pas dépasser le maximum.");
        if (maxLigue is int plafond)
        {
            if (plafond < 0)
                throw new InvalidOperationException("Le plafond de ligue ne peut pas être négatif.");
            if (plafond < max)
                throw new InvalidOperationException(
                    "Le plafond de ligue ne peut pas être inférieur au maximum autorisé à la création.");
        }
    }

    private async Task ValiderNomUniqueAsync(int rulesVersionId, string nom, int? exclureId)
    {
        var propre = nom.Trim();
        var existe = await db.StaffTypes.AnyAsync(s =>
            s.RulesVersionId == rulesVersionId
            && s.Id != (exclureId ?? 0)
            && s.Nom.ToLower() == propre.ToLower());

        if (existe)
            throw new InvalidOperationException($"Un staff « {propre} » existe déjà dans cette version.");
    }
}
