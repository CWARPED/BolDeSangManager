using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SkillCategoryDefId",
                table: "Skills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RulesVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCategories_RulesVersions_RulesVersionId",
                        column: x => x.RulesVersionId,
                        principalTable: "RulesVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_SkillCategoryDefId",
                table: "Skills",
                column: "SkillCategoryDefId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_RulesVersionId_Code",
                table: "SkillCategories",
                columns: new[] { "RulesVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_RulesVersionId_Nom",
                table: "SkillCategories",
                columns: new[] { "RulesVersionId", "Nom" },
                unique: true);

            // ─────────────────────────────────────────────────────────────────
            // BACKFILL : les catégories étaient un enum figé (0..5). On matérialise
            // les 6 catégories standard pour CHAQUE version de règles existante,
            // puis on rattache chaque compétence à la catégorie de sa propre version.
            // Sans cela, les Skills existants resteraient à SkillCategoryDefId = 0
            // et la clé étrangère ajoutée juste après échouerait.
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                INSERT INTO SkillCategories (RulesVersionId, Nom, Code)
                SELECT v.Id, c.Nom, c.Code
                FROM RulesVersions v
                CROSS JOIN (
                    SELECT 'Agilité'   AS Nom, 'A' AS Code
                    UNION ALL SELECT 'Force',     'F'
                    UNION ALL SELECT 'Générale',  'G'
                    UNION ALL SELECT 'Mutation',  'M'
                    UNION ALL SELECT 'Passe',     'P'
                    UNION ALL SELECT 'Scélérate', 'S'
                ) c;");

            migrationBuilder.Sql(@"
                UPDATE Skills
                SET SkillCategoryDefId = (
                    SELECT sc.Id
                    FROM SkillCategories sc
                    WHERE sc.RulesVersionId = Skills.RulesVersionId
                      AND sc.Nom = CASE Skills.Categorie
                            WHEN 0 THEN 'Agilité'
                            WHEN 1 THEN 'Force'
                            WHEN 2 THEN 'Générale'
                            WHEN 3 THEN 'Mutation'
                            WHEN 4 THEN 'Passe'
                            WHEN 5 THEN 'Scélérate'
                          END
                );");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_SkillCategories_SkillCategoryDefId",
                table: "Skills",
                column: "SkillCategoryDefId",
                principalTable: "SkillCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Skills_SkillCategories_SkillCategoryDefId",
                table: "Skills");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "IX_Skills_SkillCategoryDefId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "SkillCategoryDefId",
                table: "Skills");
        }
    }
}
