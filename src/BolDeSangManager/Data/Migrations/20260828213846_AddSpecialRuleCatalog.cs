using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialRuleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpecialRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialRules_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamTypeSpecialRules",
                columns: table => new
                {
                    TeamTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionsChoix = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTypeSpecialRules", x => new { x.TeamTypeId, x.SpecialRuleId });
                    table.ForeignKey(
                        name: "FK_TeamTypeSpecialRules_SpecialRules_SpecialRuleId",
                        column: x => x.SpecialRuleId,
                        principalTable: "SpecialRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamTypeSpecialRules_TeamTypes_TeamTypeId",
                        column: x => x.TeamTypeId,
                        principalTable: "TeamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialRules_RulesVersionId_Nom",
                table: "SpecialRules",
                columns: new[] { "RulesVersionId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamTypeSpecialRules_SpecialRuleId",
                table: "TeamTypeSpecialRules",
                column: "SpecialRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamTypeSpecialRules");

            migrationBuilder.DropTable(
                name: "SpecialRules");
        }
    }
}
