using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBaremeXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "XpBonusMvp",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "XpParElimination",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "XpParInterception",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "XpParPasse",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "XpParTouchdown",
                table: "RulesVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "XpBonusMvp",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "XpParElimination",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "XpParInterception",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "XpParPasse",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "XpParTouchdown",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            // Le touchdown vaut 5 en Dungeon Bowl : rattraper les versions et les
            // ligues déjà en base, que les valeurs par défaut ci-dessus ont mises à 3.
            migrationBuilder.Sql(@"
                UPDATE RulesVersions
                   SET XpParTouchdown = 5
                 WHERE GameId IN (SELECT Id FROM Games WHERE Type = 1);");

            migrationBuilder.Sql(@"
                UPDATE Leagues
                   SET XpParTouchdown = 5
                 WHERE GameId IN (SELECT Id FROM Games WHERE Type = 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XpBonusMvp",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParElimination",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParInterception",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParPasse",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpParTouchdown",
                table: "RulesVersions");

            migrationBuilder.DropColumn(
                name: "XpBonusMvp",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParElimination",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParInterception",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParPasse",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "XpParTouchdown",
                table: "Leagues");
        }
    }
}
