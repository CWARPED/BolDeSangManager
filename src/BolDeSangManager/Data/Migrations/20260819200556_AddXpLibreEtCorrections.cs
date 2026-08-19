using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddXpLibreEtCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "XpDepensee",
                table: "PlayerImprovements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "XpCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AncienneValeur = table.Column<int>(type: "INTEGER", nullable: false),
                    NouvelleValeur = table.Column<int>(type: "INTEGER", nullable: false),
                    Motif = table.Column<string>(type: "TEXT", nullable: false),
                    CorrigeParId = table.Column<string>(type: "TEXT", nullable: true),
                    CorrigeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpCorrections_AspNetUsers_CorrigeParId",
                        column: x => x.CorrigeParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_XpCorrections_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XpCorrections_CorrigeParId",
                table: "XpCorrections",
                column: "CorrigeParId");

            migrationBuilder.CreateIndex(
                name: "IX_XpCorrections_TeamPlayerId",
                table: "XpCorrections",
                column: "TeamPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XpCorrections");

            migrationBuilder.DropColumn(
                name: "XpDepensee",
                table: "PlayerImprovements");
        }
    }
}
