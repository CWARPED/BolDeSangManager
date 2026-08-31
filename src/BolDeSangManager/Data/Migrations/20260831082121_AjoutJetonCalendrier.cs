using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AjoutJetonCalendrier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JetonCalendrier",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JetonCalendrier",
                table: "AspNetUsers");
        }
    }
}
