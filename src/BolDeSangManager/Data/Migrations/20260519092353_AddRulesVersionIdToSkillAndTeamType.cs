using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesVersionIdToSkillAndTeamType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameSpecifique",
                table: "Skills");

            migrationBuilder.AddColumn<int>(
                name: "RulesVersionId",
                table: "TeamTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RulesVersionId",
                table: "Skills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TeamTypes_RulesVersionId",
                table: "TeamTypes",
                column: "RulesVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_RulesVersionId",
                table: "Skills",
                column: "RulesVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_RulesVersions_RulesVersionId",
                table: "Skills",
                column: "RulesVersionId",
                principalTable: "RulesVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTypes_RulesVersions_RulesVersionId",
                table: "TeamTypes",
                column: "RulesVersionId",
                principalTable: "RulesVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Skills_RulesVersions_RulesVersionId",
                table: "Skills");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTypes_RulesVersions_RulesVersionId",
                table: "TeamTypes");

            migrationBuilder.DropIndex(
                name: "IX_TeamTypes_RulesVersionId",
                table: "TeamTypes");

            migrationBuilder.DropIndex(
                name: "IX_Skills_RulesVersionId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "RulesVersionId",
                table: "TeamTypes");

            migrationBuilder.DropColumn(
                name: "RulesVersionId",
                table: "Skills");

            migrationBuilder.AddColumn<int>(
                name: "GameSpecifique",
                table: "Skills",
                type: "INTEGER",
                nullable: true);
        }
    }
}
