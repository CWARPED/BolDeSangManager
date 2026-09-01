using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTracabiliteCorrectionFeuille : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CorrigeeLe",
                table: "MatchSheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreAvantCorrectionDomicile",
                table: "MatchSheets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreAvantCorrectionExterieur",
                table: "MatchSheets",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrigeeLe",
                table: "MatchSheets");

            migrationBuilder.DropColumn(
                name: "ScoreAvantCorrectionDomicile",
                table: "MatchSheets");

            migrationBuilder.DropColumn(
                name: "ScoreAvantCorrectionExterieur",
                table: "MatchSheets");
        }
    }
}
