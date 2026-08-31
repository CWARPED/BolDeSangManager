using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBaremePointsLigue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsDefaite",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsNul",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PointsParAgression",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParDeviation",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParElimination",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParInterception",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParPasse",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParTouchdown",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsVictoire",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "XpParAgression",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "XpParDeviation",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombreDeTours",
                table: "MatchSheets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Agressions",
                table: "MatchPlayerRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Deviations",
                table: "MatchPlayerRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsDefaite",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsNul",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PointsParAgression",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParDeviation",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParElimination",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParInterception",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParPasse",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsParTouchdown",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsVictoire",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "XpParAgression",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "XpParDeviation",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PaliersPointsLigue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeagueId = table.Column<int>(type: "INTEGER", nullable: false),
                    JusquAuTour = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsVictoire = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsNul = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsDefaite = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaliersPointsLigue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaliersPointsLigue_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaliersPointsLigue_LeagueId_JusquAuTour",
                table: "PaliersPointsLigue",
                columns: new[] { "LeagueId", "JusquAuTour" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaliersPointsLigue");

            migrationBuilder.DropColumn(
                name: "PointsDefaite",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsNul",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParAgression",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParDeviation",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParElimination",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParInterception",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParPasse",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsParTouchdown",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "PointsVictoire",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParAgression",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParDeviation",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "NombreDeTours",
                table: "MatchSheets");

            migrationBuilder.DropColumn(
                name: "Agressions",
                table: "MatchPlayerRecords");

            migrationBuilder.DropColumn(
                name: "Deviations",
                table: "MatchPlayerRecords");

            migrationBuilder.DropColumn(
                name: "PointsDefaite",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsNul",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParAgression",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParDeviation",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParElimination",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParInterception",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParPasse",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsParTouchdown",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PointsVictoire",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParAgression",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParDeviation",
                table: "Leagues");
        }
    }
}
