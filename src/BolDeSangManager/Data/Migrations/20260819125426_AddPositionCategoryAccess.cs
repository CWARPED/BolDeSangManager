using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionCategoryAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ ORDRE IMPORTANT : EF génère les DropColumn en premier, ce qui perdrait les
            // accès historiques. On crée les tables, on convertit les chaînes « GAF », et
            // SEULEMENT ENSUITE on supprime les colonnes (déplacées en fin de méthode).

            migrationBuilder.CreateTable(
                name: "PlayerPositionCategoryAccesses",
                columns: table => new
                {
                    PlayerPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillCategoryDefId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstPrincipale = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPositionCategoryAccesses", x => new { x.PlayerPositionId, x.SkillCategoryDefId });
                    table.ForeignKey(
                        name: "FK_PlayerPositionCategoryAccesses_PlayerPositions_PlayerPositionId",
                        column: x => x.PlayerPositionId,
                        principalTable: "PlayerPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerPositionCategoryAccesses_SkillCategories_SkillCategoryDefId",
                        column: x => x.SkillCategoryDefId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PoolPositionCategoryAccesses",
                columns: table => new
                {
                    PoolPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillCategoryDefId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstPrincipale = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolPositionCategoryAccesses", x => new { x.PoolPositionId, x.SkillCategoryDefId });
                    table.ForeignKey(
                        name: "FK_PoolPositionCategoryAccesses_PoolPositions_PoolPositionId",
                        column: x => x.PoolPositionId,
                        principalTable: "PoolPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoolPositionCategoryAccesses_SkillCategories_SkillCategoryDefId",
                        column: x => x.SkillCategoryDefId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositionCategoryAccesses_SkillCategoryDefId",
                table: "PlayerPositionCategoryAccesses",
                column: "SkillCategoryDefId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolPositionCategoryAccesses_SkillCategoryDefId",
                table: "PoolPositionCategoryAccesses",
                column: "SkillCategoryDefId");

            // ─────────────────────────────────────────────────────────────────
            // BACKFILL : conversion des accès historiques stockés en chaînes de
            // lettres (« GAF ») vers la relation vers SkillCategories.
            //
            // Chaque lettre est rapprochée du Code d'une catégorie de la MÊME version
            // de règles que le poste. SQLite n'ayant pas de fonction pour itérer sur
            // les caractères, on joint sur les catégories dont le code (1 caractère à
            // ce stade — R2b introduit seulement l'affichage à 2) est contenu dans la
            // chaîne. Une catégorie présente en principal est exclue du secondaire.
            // ─────────────────────────────────────────────────────────────────

            // Postes d'équipe — accès principaux
            migrationBuilder.Sql(@"
                INSERT INTO PlayerPositionCategoryAccesses (PlayerPositionId, SkillCategoryDefId, EstPrincipale)
                SELECT p.Id, sc.Id, 1
                FROM PlayerPositions p
                JOIN TeamTypes tt      ON tt.Id = p.TeamTypeId
                JOIN SkillCategories sc ON sc.RulesVersionId = tt.RulesVersionId
                WHERE p.CompetencesPrincipales IS NOT NULL
                  AND length(sc.Code) = 1
                  AND instr(upper(p.CompetencesPrincipales), upper(sc.Code)) > 0;");

            // Postes d'équipe — accès secondaires (hors ceux déjà principaux)
            migrationBuilder.Sql(@"
                INSERT INTO PlayerPositionCategoryAccesses (PlayerPositionId, SkillCategoryDefId, EstPrincipale)
                SELECT p.Id, sc.Id, 0
                FROM PlayerPositions p
                JOIN TeamTypes tt      ON tt.Id = p.TeamTypeId
                JOIN SkillCategories sc ON sc.RulesVersionId = tt.RulesVersionId
                WHERE p.CompetencesSecondaires IS NOT NULL
                  AND length(sc.Code) = 1
                  AND instr(upper(p.CompetencesSecondaires), upper(sc.Code)) > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM PlayerPositionCategoryAccesses a
                      WHERE a.PlayerPositionId = p.Id AND a.SkillCategoryDefId = sc.Id
                  );");

            // Réserve — accès principaux
            migrationBuilder.Sql(@"
                INSERT INTO PoolPositionCategoryAccesses (PoolPositionId, SkillCategoryDefId, EstPrincipale)
                SELECT pp.Id, sc.Id, 1
                FROM PoolPositions pp
                JOIN SkillCategories sc ON sc.RulesVersionId = pp.RulesVersionId
                WHERE pp.CompetencesPrincipales IS NOT NULL
                  AND length(sc.Code) = 1
                  AND instr(upper(pp.CompetencesPrincipales), upper(sc.Code)) > 0;");

            // Réserve — accès secondaires
            migrationBuilder.Sql(@"
                INSERT INTO PoolPositionCategoryAccesses (PoolPositionId, SkillCategoryDefId, EstPrincipale)
                SELECT pp.Id, sc.Id, 0
                FROM PoolPositions pp
                JOIN SkillCategories sc ON sc.RulesVersionId = pp.RulesVersionId
                WHERE pp.CompetencesSecondaires IS NOT NULL
                  AND length(sc.Code) = 1
                  AND instr(upper(pp.CompetencesSecondaires), upper(sc.Code)) > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM PoolPositionCategoryAccesses a
                      WHERE a.PoolPositionId = pp.Id AND a.SkillCategoryDefId = sc.Id
                  );");

            // Les colonnes texte ne sont supprimées qu'APRÈS la conversion.
            migrationBuilder.DropColumn(
                name: "CompetencesPrincipales",
                table: "PoolPositions");

            migrationBuilder.DropColumn(
                name: "CompetencesSecondaires",
                table: "PoolPositions");

            migrationBuilder.DropColumn(
                name: "CompetencesPrincipales",
                table: "PlayerPositions");

            migrationBuilder.DropColumn(
                name: "CompetencesSecondaires",
                table: "PlayerPositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerPositionCategoryAccesses");

            migrationBuilder.DropTable(
                name: "PoolPositionCategoryAccesses");

            migrationBuilder.AddColumn<string>(
                name: "CompetencesPrincipales",
                table: "PoolPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetencesSecondaires",
                table: "PoolPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetencesPrincipales",
                table: "PlayerPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompetencesSecondaires",
                table: "PlayerPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
