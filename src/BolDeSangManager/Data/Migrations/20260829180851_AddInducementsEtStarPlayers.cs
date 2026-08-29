using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddInducementsEtStarPlayers : Migration
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
                    Ligues = table.Column<string>(type: "TEXT", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inducements");

            migrationBuilder.DropTable(
                name: "StarPlayers");
        }
    }
}
