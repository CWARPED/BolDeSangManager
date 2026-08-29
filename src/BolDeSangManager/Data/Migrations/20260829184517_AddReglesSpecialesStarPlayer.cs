using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddReglesSpecialesStarPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReglesSpeciales",
                table: "StarPlayers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReglesSpeciales",
                table: "StarPlayers");
        }
    }
}
