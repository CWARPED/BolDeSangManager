using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamTypeKeywordLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamTypeKeywordLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    MotCle = table.Column<string>(type: "TEXT", nullable: false),
                    Max = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTypeKeywordLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamTypeKeywordLimits_TeamTypes_TeamTypeId",
                        column: x => x.TeamTypeId,
                        principalTable: "TeamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamTypeKeywordLimits_TeamTypeId",
                table: "TeamTypeKeywordLimits",
                column: "TeamTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamTypeKeywordLimits");
        }
    }
}
