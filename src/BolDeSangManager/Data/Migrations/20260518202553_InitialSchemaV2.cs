using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cle = table.Column<string>(type: "TEXT", nullable: false),
                    Valeur = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PseudoCoach = table.Column<string>(type: "TEXT", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Categorie = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EstElite = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstTrait = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameSpecifique = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RulesVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    EstActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RulesVersions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    ReglesSpeciales = table.Column<string>(type: "TEXT", nullable: false),
                    CoutRelance = table.Column<int>(type: "INTEGER", nullable: false),
                    Categorie = table.Column<int>(type: "INTEGER", nullable: false),
                    ReglesSpecialesLigue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamTypes_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CommissaireId = table.Column<string>(type: "TEXT", nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    BudgetDepart = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreEquipesPlayoff = table.Column<int>(type: "INTEGER", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leagues_AspNetUsers_CommissaireId",
                        column: x => x.CommissaireId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leagues_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leagues_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    QuantiteMax = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleNom = table.Column<string>(type: "TEXT", nullable: true),
                    RoleQuantiteMax = table.Column<int>(type: "INTEGER", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    Mouvement = table.Column<int>(type: "INTEGER", nullable: false),
                    Force = table.Column<int>(type: "INTEGER", nullable: false),
                    Agilite = table.Column<string>(type: "TEXT", nullable: false),
                    CapacitePasse = table.Column<string>(type: "TEXT", nullable: false),
                    Armure = table.Column<string>(type: "TEXT", nullable: false),
                    CompetencesPrincipales = table.Column<string>(type: "TEXT", nullable: false),
                    CompetencesSecondaires = table.Column<string>(type: "TEXT", nullable: false),
                    EstGrosBras = table.Column<bool>(type: "INTEGER", nullable: false),
                    DescriptionRole = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerPositions_TeamTypes_TeamTypeId",
                        column: x => x.TeamTypeId,
                        principalTable: "TeamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPositionSkills",
                columns: table => new
                {
                    PlayerPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPositionSkills", x => new { x.PlayerPositionId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_PlayerPositionSkills_PlayerPositions_PlayerPositionId",
                        column: x => x.PlayerPositionId,
                        principalTable: "PlayerPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerPositionSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    CoachId = table.Column<string>(type: "TEXT", nullable: false),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    DivisionId = table.Column<int>(type: "INTEGER", nullable: true),
                    TeamTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tresorerie = table.Column<int>(type: "INTEGER", nullable: false),
                    FansDevoues = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreRelances = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreCoachsAssistants = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreCheerleaders = table.Column<int>(type: "INTEGER", nullable: false),
                    Apothicaire = table.Column<bool>(type: "INTEGER", nullable: false),
                    PointsLigue = table.Column<int>(type: "INTEGER", nullable: false),
                    TouchdownsMarques = table.Column<int>(type: "INTEGER", nullable: false),
                    TouchdownsConcedes = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminationsInfligees = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreMatchsJoues = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreVictoires = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreDefaites = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreNuls = table.Column<int>(type: "INTEGER", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Teams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_TeamTypes_TeamTypeId",
                        column: x => x.TeamTypeId,
                        principalTable: "TeamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DivisionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ronde = table.Column<int>(type: "INTEGER", nullable: false),
                    EstPlayoff = table.Column<bool>(type: "INTEGER", nullable: false),
                    EquipeDomicileId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipeExterieurId = table.Column<int>(type: "INTEGER", nullable: false),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    DateProgrammee = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateJouee = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScoreDomicile = table.Column<int>(type: "INTEGER", nullable: true),
                    ScoreExterieur = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_EquipeDomicileId",
                        column: x => x.EquipeDomicileId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_EquipeExterieurId",
                        column: x => x.EquipeExterieurId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhaseDeReposValidations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValideLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseDeReposValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhaseDeReposValidations_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhaseDeReposValidations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsStarPlayer = table.Column<int>(type: "INTEGER", nullable: false),
                    ValeurActuelle = table.Column<int>(type: "INTEGER", nullable: false),
                    ModMouvement = table.Column<int>(type: "INTEGER", nullable: false),
                    ModForce = table.Column<int>(type: "INTEGER", nullable: false),
                    ModAgilite = table.Column<int>(type: "INTEGER", nullable: false),
                    ModCapacitePasse = table.Column<int>(type: "INTEGER", nullable: false),
                    ModArmure = table.Column<int>(type: "INTEGER", nullable: false),
                    ManqueSuivantMatch = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstMort = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstRetraite = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecruteLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPlayers_PlayerPositions_PlayerPositionId",
                        column: x => x.PlayerPositionId,
                        principalTable: "PlayerPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaisiParId = table.Column<string>(type: "TEXT", nullable: false),
                    SaisiLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TouchdownsDomicile = table.Column<int>(type: "INTEGER", nullable: false),
                    TouchdownsExterieur = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminationsDomicile = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminationsExterieur = table.Column<int>(type: "INTEGER", nullable: false),
                    GainsDomicile = table.Column<int>(type: "INTEGER", nullable: false),
                    GainsExterieur = table.Column<int>(type: "INTEGER", nullable: false),
                    VariationFansDomicile = table.Column<int>(type: "INTEGER", nullable: false),
                    VariationFansExterieur = table.Column<int>(type: "INTEGER", nullable: false),
                    InducementsDomicile = table.Column<string>(type: "TEXT", nullable: false),
                    InducementsExterieur = table.Column<string>(type: "TEXT", nullable: false),
                    ValideParCommissaire = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotesCommissaire = table.Column<string>(type: "TEXT", nullable: false),
                    ApresMatchDomicileValide = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApresMatchExterieurValide = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSheets_AspNetUsers_SaisiParId",
                        column: x => x.SaisiParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchSheets_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    CoachId = table.Column<string>(type: "TEXT", nullable: true),
                    AttribueLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueAwards_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeagueAwards_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueAwards_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LeagueAwards_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInjuries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StatAffectee = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInjuries_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerInjuries_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayerSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstCompetenceDepart = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnAttenteValidation = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayerSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPlayerSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayerSkills_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchPlayerRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchSheetId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstCoteDomicile = table.Column<bool>(type: "INTEGER", nullable: false),
                    Touchdowns = table.Column<int>(type: "INTEGER", nullable: false),
                    Completions = table.Column<int>(type: "INTEGER", nullable: false),
                    Interceptions = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminationsInfligees = table.Column<int>(type: "INTEGER", nullable: false),
                    EstMVP = table.Column<bool>(type: "INTEGER", nullable: false),
                    PspGagnes = table.Column<int>(type: "INTEGER", nullable: false),
                    Blessure = table.Column<int>(type: "INTEGER", nullable: true),
                    StatAffectee = table.Column<int>(type: "INTEGER", nullable: true),
                    AManqueLeMatch = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayerRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPlayerRecords_MatchSheets_MatchSheetId",
                        column: x => x.MatchSheetId,
                        principalTable: "MatchSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchPlayerRecords_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerImprovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Palier = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: true),
                    StatAmelioree = table.Column<int>(type: "INTEGER", nullable: true),
                    ValeurHausse = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliqueLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EnAttenteValidation = table.Column<bool>(type: "INTEGER", nullable: false),
                    MatchSheetId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerImprovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerImprovements_MatchSheets_MatchSheetId",
                        column: x => x.MatchSheetId,
                        principalTable: "MatchSheets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerImprovements_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerImprovements_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_LeagueId",
                table: "Divisions",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAwards_CoachId",
                table: "LeagueAwards",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAwards_LeagueId",
                table: "LeagueAwards",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAwards_TeamId",
                table: "LeagueAwards",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueAwards_TeamPlayerId",
                table: "LeagueAwards",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_CommissaireId",
                table: "Leagues",
                column: "CommissaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_GameId",
                table: "Leagues",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_RulesVersionId",
                table: "Leagues",
                column: "RulesVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DivisionId",
                table: "Matches",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_EquipeDomicileId",
                table: "Matches",
                column: "EquipeDomicileId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_EquipeExterieurId",
                table: "Matches",
                column: "EquipeExterieurId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRecords_MatchSheetId",
                table: "MatchPlayerRecords",
                column: "MatchSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRecords_TeamPlayerId",
                table: "MatchPlayerRecords",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSheets_MatchId",
                table: "MatchSheets",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchSheets_SaisiParId",
                table: "MatchSheets",
                column: "SaisiParId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDeReposValidations_LeagueId_TeamId",
                table: "PhaseDeReposValidations",
                columns: new[] { "LeagueId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDeReposValidations_TeamId",
                table: "PhaseDeReposValidations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerImprovements_MatchSheetId",
                table: "PlayerImprovements",
                column: "MatchSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerImprovements_SkillId",
                table: "PlayerImprovements",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerImprovements_TeamPlayerId",
                table: "PlayerImprovements",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInjuries_MatchId",
                table: "PlayerInjuries",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInjuries_TeamPlayerId",
                table: "PlayerInjuries",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositions_TeamTypeId",
                table: "PlayerPositions",
                column: "TeamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositionSkills_SkillId",
                table: "PlayerPositionSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_RulesVersions_GameId",
                table: "RulesVersions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_PlayerPositionId",
                table: "TeamPlayers",
                column: "PlayerPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_TeamId",
                table: "TeamPlayers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerSkills_SkillId",
                table: "TeamPlayerSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerSkills_TeamPlayerId",
                table: "TeamPlayerSkills",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CoachId",
                table: "Teams",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId",
                table: "Teams",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeagueId",
                table: "Teams",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamTypeId",
                table: "Teams",
                column: "TeamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTypes_GameId",
                table: "TeamTypes",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppConfigs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "LeagueAwards");

            migrationBuilder.DropTable(
                name: "MatchPlayerRecords");

            migrationBuilder.DropTable(
                name: "PhaseDeReposValidations");

            migrationBuilder.DropTable(
                name: "PlayerImprovements");

            migrationBuilder.DropTable(
                name: "PlayerInjuries");

            migrationBuilder.DropTable(
                name: "PlayerPositionSkills");

            migrationBuilder.DropTable(
                name: "TeamPlayerSkills");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "MatchSheets");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "TeamPlayers");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "PlayerPositions");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "TeamTypes");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "RulesVersions");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
