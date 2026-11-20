using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCoupsDePouceEquipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamInducements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    InducementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantite = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInducements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInducements_Inducements_InducementId",
                        column: x => x.InducementId,
                        principalTable: "Inducements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamInducements_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInducements_InducementId",
                table: "TeamInducements",
                column: "InducementId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInducements_TeamId_InducementId",
                table: "TeamInducements",
                columns: new[] { "TeamId", "InducementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamInducements");
        }
    }
}
