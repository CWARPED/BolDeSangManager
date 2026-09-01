using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class RenommerPalierAPartirDuTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JusquAuTour",
                table: "PaliersPointsLigue",
                newName: "APartirDuTour");

            migrationBuilder.RenameIndex(
                name: "IX_PaliersPointsLigue_LeagueId_JusquAuTour",
                table: "PaliersPointsLigue",
                newName: "IX_PaliersPointsLigue_LeagueId_APartirDuTour");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "APartirDuTour",
                table: "PaliersPointsLigue",
                newName: "JusquAuTour");

            migrationBuilder.RenameIndex(
                name: "IX_PaliersPointsLigue_LeagueId_APartirDuTour",
                table: "PaliersPointsLigue",
                newName: "IX_PaliersPointsLigue_LeagueId_JusquAuTour");
        }
    }
}
