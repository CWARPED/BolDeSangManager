using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoolPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    QuantiteMax = table.Column<int>(type: "INTEGER", nullable: false),
                    Cout = table.Column<int>(type: "INTEGER", nullable: false),
                    Mouvement = table.Column<int>(type: "INTEGER", nullable: false),
                    Force = table.Column<int>(type: "INTEGER", nullable: false),
                    Agilite = table.Column<string>(type: "TEXT", nullable: false),
                    CapacitePasse = table.Column<string>(type: "TEXT", nullable: false),
                    Armure = table.Column<string>(type: "TEXT", nullable: false),
                    CompetencesPrincipales = table.Column<string>(type: "TEXT", nullable: false),
                    CompetencesSecondaires = table.Column<string>(type: "TEXT", nullable: false),
                    MotsCles = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolPositions_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoolPositionSkills",
                columns: table => new
                {
                    PoolPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolPositionSkills", x => new { x.PoolPositionId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_PoolPositionSkills_PoolPositions_PoolPositionId",
                        column: x => x.PoolPositionId,
                        principalTable: "PoolPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoolPositionSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoolPositions_RulesVersionId",
                table: "PoolPositions",
                column: "RulesVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolPositionSkills_SkillId",
                table: "PoolPositionSkills",
                column: "SkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoolPositionSkills");

            migrationBuilder.DropTable(
                name: "PoolPositions");
        }
    }
}
