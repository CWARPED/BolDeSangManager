using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPresentationVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Presentation",
                table: "RulesVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Presentation",
                table: "RulesVersions");
        }
    }
}
