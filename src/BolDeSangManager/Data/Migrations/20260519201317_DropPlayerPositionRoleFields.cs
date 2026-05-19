using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class DropPlayerPositionRoleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionRole",
                table: "PlayerPositions");

            migrationBuilder.DropColumn(
                name: "RoleNom",
                table: "PlayerPositions");

            migrationBuilder.DropColumn(
                name: "RoleQuantiteMax",
                table: "PlayerPositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionRole",
                table: "PlayerPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleNom",
                table: "PlayerPositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleQuantiteMax",
                table: "PlayerPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
