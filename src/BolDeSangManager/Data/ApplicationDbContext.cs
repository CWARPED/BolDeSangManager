using BolDeSangManager.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<RulesVersion> RulesVersions => Set<RulesVersion>();
    public DbSet<TeamType> TeamTypes => Set<TeamType>();
    public DbSet<PlayerPosition> PlayerPositions => Set<PlayerPosition>();
    public DbSet<PlayerPositionSkill> PlayerPositionSkills => Set<PlayerPositionSkill>();
    public DbSet<PlayerPositionCategoryAccess> PlayerPositionCategoryAccesses => Set<PlayerPositionCategoryAccess>();
    public DbSet<PoolPosition> PoolPositions => Set<PoolPosition>();
    public DbSet<PoolPositionSkill> PoolPositionSkills => Set<PoolPositionSkill>();
    public DbSet<PoolPositionCategoryAccess> PoolPositionCategoryAccesses => Set<PoolPositionCategoryAccess>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillCategoryDef> SkillCategories => Set<SkillCategoryDef>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<EcheanceRonde> EcheancesRondes => Set<EcheanceRonde>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<TeamPlayerSkill> TeamPlayerSkills => Set<TeamPlayerSkill>();
    public DbSet<PlayerInjury> PlayerInjuries => Set<PlayerInjury>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchSheet> MatchSheets => Set<MatchSheet>();
    public DbSet<MatchPlayerRecord> MatchPlayerRecords => Set<MatchPlayerRecord>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<PlayerImprovement> PlayerImprovements => Set<PlayerImprovement>();
    public DbSet<XpCorrection> XpCorrections => Set<XpCorrection>();
    public DbSet<PhaseDeReposValidation> PhaseDeReposValidations => Set<PhaseDeReposValidation>();
    public DbSet<LeagueAward> LeagueAwards => Set<LeagueAward>();
    public DbSet<TeamTypeKeywordLimit> TeamTypeKeywordLimits => Set<TeamTypeKeywordLimit>();
    public DbSet<LeagueCommissioner> LeagueCommissioners => Set<LeagueCommissioner>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // PlayerPositionSkill — composite key
        builder.Entity<PlayerPositionSkill>()
            .HasKey(pps => new { pps.PlayerPositionId, pps.SkillId });

        // PoolPositionSkill — clé composite (miroir de PlayerPositionSkill)
        builder.Entity<PoolPositionSkill>()
            .HasKey(pps => new { pps.PoolPositionId, pps.SkillId });

        // PoolPosition → RulesVersion (cascade : suppression version => suppression pool)
        builder.Entity<PoolPosition>()
            .HasOne(p => p.RulesVersion)
            .WithMany(v => v.PoolPositions)
            .HasForeignKey(p => p.RulesVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // PoolPositionSkill → PoolPosition (cascade) et → Skill (restrict)
        builder.Entity<PoolPositionSkill>()
            .HasOne(pps => pps.PoolPosition)
            .WithMany(p => p.CompetencesDepart)
            .HasForeignKey(pps => pps.PoolPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PoolPositionSkill>()
            .HasOne(pps => pps.Skill)
            .WithMany()
            .HasForeignKey(pps => pps.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        // Match — deux FK vers Team, désactiver la suppression en cascade
        builder.Entity<Match>()
            .HasOne(m => m.EquipeDomicile)
            .WithMany()
            .HasForeignKey(m => m.EquipeDomicileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.EquipeExterieur)
            .WithMany()
            .HasForeignKey(m => m.EquipeExterieurId)
            .OnDelete(DeleteBehavior.Restrict);

        // Match — une seule MatchSheet par match
        builder.Entity<Match>()
            .HasOne(m => m.Feuille)
            .WithOne(f => f.Match)
            .HasForeignKey<MatchSheet>(f => f.MatchId);

        // League — FK vers ApplicationUser (Commissaire)
        builder.Entity<League>()
            .HasOne(l => l.Commissaire)
            .WithMany(u => u.LiguesCommissaireees)
            .HasForeignKey(l => l.CommissaireId)
            .OnDelete(DeleteBehavior.Restrict);

        // EcheanceRonde — une seule date par (ligue, ronde). Cascade : les
        // échéances n'ont aucun sens sans leur ligue.
        builder.Entity<EcheanceRonde>()
            .HasIndex(e => new { e.LeagueId, e.Ronde })
            .IsUnique();

        builder.Entity<EcheanceRonde>()
            .HasOne(e => e.League)
            .WithMany()
            .HasForeignKey(e => e.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Team — FK vers ApplicationUser (Coach)
        builder.Entity<Team>()
            .HasOne(t => t.Coach)
            .WithMany(u => u.Equipes)
            .HasForeignKey(t => t.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        // PlayerInjury — FK nullable vers Match
        builder.Entity<PlayerInjury>()
            .HasOne(pi => pi.Match)
            .WithMany(m => m.Blessures)
            .HasForeignKey(pi => pi.MatchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // MatchSheet — FK vers ApplicationUser (SaisiPar)
        builder.Entity<MatchSheet>()
            .HasOne(ms => ms.SaisiPar)
            .WithMany()
            .HasForeignKey(ms => ms.SaisiParId)
            .OnDelete(DeleteBehavior.Restrict);

        // Team - Division (nullable)
        builder.Entity<Team>()
            .HasOne(t => t.Division)
            .WithMany(d => d.Equipes)
            .HasForeignKey(t => t.DivisionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Match - Division (nullable)
        builder.Entity<Match>()
            .HasOne(m => m.Division)
            .WithMany(d => d.Matchs)
            .HasForeignKey(m => m.DivisionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // PlayerImprovement → TeamPlayer (cascade)
        builder.Entity<PlayerImprovement>()
            .HasOne(pi => pi.TeamPlayer)
            .WithMany(tp => tp.Improvements)
            .HasForeignKey(pi => pi.TeamPlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlayerImprovement → Skill (set null si skill supprimé)
        builder.Entity<PlayerImprovement>()
            .HasOne(pi => pi.Skill)
            .WithMany()
            .HasForeignKey(pi => pi.SkillId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // XpCorrection → TeamPlayer (cascade) : la trace suit le joueur
        builder.Entity<XpCorrection>()
            .HasOne(c => c.TeamPlayer)
            .WithMany()
            .HasForeignKey(c => c.TeamPlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // XpCorrection → auteur : on garde la trace même si le compte est supprimé
        builder.Entity<XpCorrection>()
            .HasOne(c => c.CorrigePar)
            .WithMany()
            .HasForeignKey(c => c.CorrigeParId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<XpCorrection>().Ignore(c => c.Ecart);

        // PhaseDeReposValidation → League (cascade)
        builder.Entity<PhaseDeReposValidation>()
            .HasOne(prv => prv.League)
            .WithMany(l => l.ValidationsRepos)
            .HasForeignKey(prv => prv.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        // PhaseDeReposValidation → Team (restrict pour éviter cascade circulaire)
        builder.Entity<PhaseDeReposValidation>()
            .HasOne(prv => prv.Team)
            .WithMany()
            .HasForeignKey(prv => prv.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // PhaseDeReposValidation — une seule validation par équipe par ligue
        builder.Entity<PhaseDeReposValidation>()
            .HasIndex(prv => new { prv.LeagueId, prv.TeamId })
            .IsUnique();

        // LeagueAward → League (cascade)
        builder.Entity<LeagueAward>()
            .HasOne(a => a.League)
            .WithMany(l => l.Awards)
            .HasForeignKey(a => a.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        // TeamTypeKeywordLimit → TeamType (cascade)
        builder.Entity<TeamTypeKeywordLimit>()
            .HasOne(l => l.TeamType)
            .WithMany(t => t.LimitesMotsCles)
            .HasForeignKey(l => l.TeamTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // LeagueAward → TeamPlayer / Team / Coach (tous optionnels, set null)
        builder.Entity<LeagueAward>()
            .HasOne(a => a.TeamPlayer)
            .WithMany()
            .HasForeignKey(a => a.TeamPlayerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LeagueAward>()
            .HasOne(a => a.Team)
            .WithMany()
            .HasForeignKey(a => a.TeamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LeagueAward>()
            .HasOne(a => a.Coach)
            .WithMany()
            .HasForeignKey(a => a.CoachId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // LeagueCommissioner — many-to-many League↔User
        builder.Entity<LeagueCommissioner>()
            .HasOne(lc => lc.League)
            .WithMany(l => l.CommissairesDeLigue)
            .HasForeignKey(lc => lc.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LeagueCommissioner>()
            .HasOne(lc => lc.User)
            .WithMany()
            .HasForeignKey(lc => lc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LeagueCommissioner>()
            .HasIndex(lc => new { lc.LeagueId, lc.UserId })
            .IsUnique();

        // TeamType → RulesVersion (Restrict pour éviter cascade circulaire avec Game→RulesVersion→TeamType)
        builder.Entity<TeamType>()
            .HasOne(t => t.RulesVersion)
            .WithMany()
            .HasForeignKey(t => t.RulesVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Skill → RulesVersion
        builder.Entity<Skill>()
            .HasOne(s => s.RulesVersion)
            .WithMany()
            .HasForeignKey(s => s.RulesVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // SkillCategoryDef → RulesVersion (cascade : les catégories suivent leur version)
        builder.Entity<SkillCategoryDef>()
            .HasOne(c => c.RulesVersion)
            .WithMany()
            .HasForeignKey(c => c.RulesVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unicité du nom et du code au sein d'une même version
        builder.Entity<SkillCategoryDef>()
            .HasIndex(c => new { c.RulesVersionId, c.Nom })
            .IsUnique();
        builder.Entity<SkillCategoryDef>()
            .HasIndex(c => new { c.RulesVersionId, c.Code })
            .IsUnique();

        // Skill → SkillCategoryDef : Restrict, une catégorie utilisée ne peut pas être supprimée
        builder.Entity<Skill>()
            .HasOne(s => s.SkillCategoryDef)
            .WithMany(c => c.Competences)
            .HasForeignKey(s => s.SkillCategoryDefId)
            .OnDelete(DeleteBehavior.Restrict);

        // Accès d'un poste aux catégories — clé composite (poste, catégorie)
        builder.Entity<PlayerPositionCategoryAccess>()
            .HasKey(a => new { a.PlayerPositionId, a.SkillCategoryDefId });

        builder.Entity<PlayerPositionCategoryAccess>()
            .HasOne(a => a.PlayerPosition)
            .WithMany(p => p.AccesCategories)
            .HasForeignKey(a => a.PlayerPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict : une catégorie référencée par un accès de poste ne peut pas être supprimée
        builder.Entity<PlayerPositionCategoryAccess>()
            .HasOne(a => a.SkillCategoryDef)
            .WithMany()
            .HasForeignKey(a => a.SkillCategoryDefId)
            .OnDelete(DeleteBehavior.Restrict);

        // Idem pour la Réserve
        builder.Entity<PoolPositionCategoryAccess>()
            .HasKey(a => new { a.PoolPositionId, a.SkillCategoryDefId });

        builder.Entity<PoolPositionCategoryAccess>()
            .HasOne(a => a.PoolPosition)
            .WithMany(p => p.AccesCategories)
            .HasForeignKey(a => a.PoolPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PoolPositionCategoryAccess>()
            .HasOne(a => a.SkillCategoryDef)
            .WithMany()
            .HasForeignKey(a => a.SkillCategoryDefId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
