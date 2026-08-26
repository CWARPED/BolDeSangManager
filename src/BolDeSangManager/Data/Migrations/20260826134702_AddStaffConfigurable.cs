using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariationFansDomicileAppliquee",
                table: "MatchSheets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariationFansExterieurAppliquee",
                table: "MatchSheets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StaffTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    EstActif = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    CoutDepuisTypeEquipe = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinCreation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCreation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLigue = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffTypes_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueStaffTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    StaffTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    EstActif = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    CoutDepuisTypeEquipe = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinCreation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCreation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLigue = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueStaffTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueStaffTypes_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueStaffTypes_StaffTypes_StaffTypeId",
                        column: x => x.StaffTypeId,
                        principalTable: "StaffTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamStaffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeagueStaffTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantite = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStaffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamStaffs_LeagueStaffTypes_LeagueStaffTypeId",
                        column: x => x.LeagueStaffTypeId,
                        principalTable: "LeagueStaffTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamStaffs_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueStaffTypes_LeagueId",
                table: "LeagueStaffTypes",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueStaffTypes_StaffTypeId",
                table: "LeagueStaffTypes",
                column: "StaffTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffTypes_RulesVersionId",
                table: "StaffTypes",
                column: "RulesVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_LeagueStaffTypeId",
                table: "TeamStaffs",
                column: "LeagueStaffTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_TeamId_LeagueStaffTypeId",
                table: "TeamStaffs",
                columns: new[] { "TeamId", "LeagueStaffTypeId" },
                unique: true);

            // ── Backfill ─────────────────────────────────────────────────────
            // Matérialise les cinq staff standard pour chaque version de règles
            // existante, les recopie dans chaque ligue, puis reporte les valeurs
            // des équipes live dans TeamStaffs.
            //
            // Les colonnes historiques de Teams (FansDevoues, NombreRelances…)
            // sont CONSERVÉES : rien n'est supprimé, aucune équipe ne casse.
            // Elles cessent simplement d'être lues par le code.
            //
            // Prix et bornes d'origine = valeurs LRB qui étaient codées en dur
            // dans TeamService / Rejoindre.razor / ApresMatch.razor.
            migrationBuilder.Sql(@"
INSERT INTO StaffTypes (RulesVersionId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT v.Id, 'Fans dévoués', 'Public fidèle de l''équipe. Influence l''affluence et les gains de match.', 1, 1, 10000, 0, 1, 9, NULL FROM RulesVersions v;

INSERT INTO StaffTypes (RulesVersionId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT v.Id, 'Relances', 'Relances d''équipe disponibles au début de chaque match. Leur prix dépend de la race.', 2, 1, 0, 1, 0, 8, 8 FROM RulesVersions v;

INSERT INTO StaffTypes (RulesVersionId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT v.Id, 'Coachs assistants', 'Chaque coach assistant aide à récupérer l''avantage de terrain.', 3, 1, 10000, 0, 0, 6, NULL FROM RulesVersions v;

INSERT INTO StaffTypes (RulesVersionId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT v.Id, 'Cheerleaders', 'Chaque cheerleader aide à récupérer l''avantage de terrain.', 4, 1, 10000, 0, 0, 6, NULL FROM RulesVersions v;

INSERT INTO StaffTypes (RulesVersionId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT v.Id, 'Apothicaire', 'Permet de relancer un jet de blessure une fois par match.', 5, 1, 50000, 0, 0, 1, 1 FROM RulesVersions v;
");

            // Copie dans chaque ligue, depuis la version de règles qu'elle utilise.
            migrationBuilder.Sql(@"
INSERT INTO LeagueStaffTypes (LeagueId, StaffTypeId, Nom, Description, Ordre, EstActif, Cout, CoutDepuisTypeEquipe, MinCreation, MaxCreation, MaxLigue)
SELECT l.Id, s.Id, s.Nom, s.Description, s.Ordre, s.EstActif,
       CASE WHEN s.CoutDepuisTypeEquipe = 1 THEN 0 ELSE s.Cout END,
       s.CoutDepuisTypeEquipe, s.MinCreation, s.MaxCreation, s.MaxLigue
FROM Leagues l
JOIN StaffTypes s ON s.RulesVersionId = l.RulesVersionId;
");

            // Report des quantités détenues. Seules les valeurs non nulles sont
            // insérées : une équipe sans cheerleader n'a pas besoin d'une ligne.
            migrationBuilder.Sql(@"
INSERT INTO TeamStaffs (TeamId, LeagueStaffTypeId, Quantite)
SELECT t.Id, lst.Id, t.FansDevoues
FROM Teams t JOIN LeagueStaffTypes lst ON lst.LeagueId = t.LeagueId AND lst.Nom = 'Fans dévoués'
WHERE t.FansDevoues > 0;

INSERT INTO TeamStaffs (TeamId, LeagueStaffTypeId, Quantite)
SELECT t.Id, lst.Id, t.NombreRelances
FROM Teams t JOIN LeagueStaffTypes lst ON lst.LeagueId = t.LeagueId AND lst.Nom = 'Relances'
WHERE t.NombreRelances > 0;

INSERT INTO TeamStaffs (TeamId, LeagueStaffTypeId, Quantite)
SELECT t.Id, lst.Id, t.NombreCoachsAssistants
FROM Teams t JOIN LeagueStaffTypes lst ON lst.LeagueId = t.LeagueId AND lst.Nom = 'Coachs assistants'
WHERE t.NombreCoachsAssistants > 0;

INSERT INTO TeamStaffs (TeamId, LeagueStaffTypeId, Quantite)
SELECT t.Id, lst.Id, t.NombreCheerleaders
FROM Teams t JOIN LeagueStaffTypes lst ON lst.LeagueId = t.LeagueId AND lst.Nom = 'Cheerleaders'
WHERE t.NombreCheerleaders > 0;

INSERT INTO TeamStaffs (TeamId, LeagueStaffTypeId, Quantite)
SELECT t.Id, lst.Id, 1
FROM Teams t JOIN LeagueStaffTypes lst ON lst.LeagueId = t.LeagueId AND lst.Nom = 'Apothicaire'
WHERE t.Apothicaire = 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamStaffs");

            migrationBuilder.DropTable(
                name: "LeagueStaffTypes");

            migrationBuilder.DropTable(
                name: "StaffTypes");

            migrationBuilder.DropColumn(
                name: "VariationFansDomicileAppliquee",
                table: "MatchSheets");

            migrationBuilder.DropColumn(
                name: "VariationFansExterieurAppliquee",
                table: "MatchSheets");
        }
    }
}
