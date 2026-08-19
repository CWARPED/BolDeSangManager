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
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false)
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
                INSERT INTO SkillCategories (RulesVersionId, Nom, Code, Ordre)
                SELECT v.Id, c.Nom, c.Code, c.Ordre
                FROM RulesVersions v
                CROSS JOIN (
                    SELECT 0 AS Val, 'Agilité'   AS Nom, 'A' AS Code, 1 AS Ordre
                    UNION ALL SELECT 1, 'Force',     'F', 2
                    UNION ALL SELECT 2, 'Générale',  'G', 3
                    UNION ALL SELECT 3, 'Mutation',  'M', 4
                    UNION ALL SELECT 4, 'Passe',     'P', 5
                    UNION ALL SELECT 5, 'Scélérate', 'S', 6
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
