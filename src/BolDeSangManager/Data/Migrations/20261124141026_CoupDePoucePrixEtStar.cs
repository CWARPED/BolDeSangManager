using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class CoupDePoucePrixEtStar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrixSaisi",
                table: "TeamInducements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StarPlayerId",
                table: "TeamInducements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstStarPlayer",
                table: "Inducements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TeamInducements_StarPlayerId",
                table: "TeamInducements",
                column: "StarPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamInducements_StarPlayers_StarPlayerId",
                table: "TeamInducements",
                column: "StarPlayerId",
                principalTable: "StarPlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamInducements_StarPlayers_StarPlayerId",
                table: "TeamInducements");

            migrationBuilder.DropIndex(
                name: "IX_TeamInducements_StarPlayerId",
                table: "TeamInducements");

            migrationBuilder.DropColumn(
                name: "PrixSaisi",
                table: "TeamInducements");

            migrationBuilder.DropColumn(
                name: "StarPlayerId",
                table: "TeamInducements");

            migrationBuilder.DropColumn(
                name: "EstStarPlayer",
                table: "Inducements");
        }
    }
}
