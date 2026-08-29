using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddLimiteRecrueGratuite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LimiteParApresMatch",
                table: "TeamTypeSpecialRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecrueGratuiteMatchId",
                table: "TeamPlayers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LimiteParApresMatch",
                table: "TeamTypeSpecialRules");

            migrationBuilder.DropColumn(
                name: "RecrueGratuiteMatchId",
                table: "TeamPlayers");
        }
    }
}
