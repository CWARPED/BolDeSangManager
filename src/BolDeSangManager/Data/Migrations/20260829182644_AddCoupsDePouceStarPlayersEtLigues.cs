using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupsDePouceStarPlayersEtLigues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inducements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inducements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inducements_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    Mouvement = table.Column<int>(type: "INTEGER", nullable: false),
                    Force = table.Column<int>(type: "INTEGER", nullable: false),
                    Agilite = table.Column<string>(type: "TEXT", nullable: false),
                    CapacitePasse = table.Column<string>(type: "TEXT", nullable: false),
                    Armure = table.Column<string>(type: "TEXT", nullable: false),
                    Competences = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarPlayers_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThemedLeagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemedLeagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemedLeagues_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarPlayerThemedLeague",
                columns: table => new
                {
                    StarPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemedLeagueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarPlayerThemedLeague", x => new { x.StarPlayerId, x.ThemedLeagueId });
                    table.ForeignKey(
                        name: "FK_StarPlayerThemedLeague_StarPlayers_StarPlayerId",
                        column: x => x.StarPlayerId,
                        principalTable: "StarPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StarPlayerThemedLeague_ThemedLeagues_ThemedLeagueId",
                        column: x => x.ThemedLeagueId,
                        principalTable: "ThemedLeagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamTypeThemedLeague",
                columns: table => new
                {
                    TeamTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemedLeagueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTypeThemedLeague", x => new { x.TeamTypeId, x.ThemedLeagueId });
                    table.ForeignKey(
                        name: "FK_TeamTypeThemedLeague_TeamTypes_TeamTypeId",
                        column: x => x.TeamTypeId,
                        principalTable: "TeamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamTypeThemedLeague_ThemedLeagues_ThemedLeagueId",
                        column: x => x.ThemedLeagueId,
                        principalTable: "ThemedLeagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inducements_RulesVersionId_Nom",
                table: "Inducements",
                columns: new[] { "RulesVersionId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarPlayers_RulesVersionId_Nom",
                table: "StarPlayers",
                columns: new[] { "RulesVersionId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarPlayerThemedLeague_ThemedLeagueId",
                table: "StarPlayerThemedLeague",
                column: "ThemedLeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTypeThemedLeague_ThemedLeagueId",
                table: "TeamTypeThemedLeague",
                column: "ThemedLeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemedLeagues_RulesVersionId_Nom",
                table: "ThemedLeagues",
                columns: new[] { "RulesVersionId", "Nom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inducements");

            migrationBuilder.DropTable(
                name: "StarPlayerThemedLeague");

            migrationBuilder.DropTable(
                name: "TeamTypeThemedLeague");

            migrationBuilder.DropTable(
                name: "StarPlayers");

            migrationBuilder.DropTable(
                name: "ThemedLeagues");
        }
    }
}
