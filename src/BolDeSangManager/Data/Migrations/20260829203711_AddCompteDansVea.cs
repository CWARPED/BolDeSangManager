using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddCompteDansVea : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Purement additive : deux <c>AddColumn</c>, aucun Drop/Alter.
        ///
        /// ⚠️ Le défaut C# (<c>= true</c>) ne s'applique qu'aux nouvelles
        /// instances : les lignes déjà en base prennent le défaut SQL. Il est
        /// donc posé à <c>true</c> — sinon TOUT le staff existant sortirait de
        /// la VEA d'un coup — puis un backfill exclut les seuls fans dévoués,
        /// qui sont le sujet de la correction.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompteDansVea",
                table: "StaffTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CompteDansVea",
                table: "LeagueStaffTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // Les fans dévoués mesurent le public, pas la puissance de l'équipe :
            // les compter gonflait la VEA et faussait les coups de pouce.
            migrationBuilder.Sql(
                "UPDATE StaffTypes SET CompteDansVea = 0 WHERE Nom = 'Fans dévoués';");
            migrationBuilder.Sql(
                "UPDATE LeagueStaffTypes SET CompteDansVea = 0 WHERE Nom = 'Fans dévoués';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompteDansVea",
                table: "StaffTypes");

            migrationBuilder.DropColumn(
                name: "CompteDansVea",
                table: "LeagueStaffTypes");
        }
    }
}
