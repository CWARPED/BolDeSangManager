using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <summary>
    /// Réparation de données — AUCUN changement de schéma.
    ///
    /// Certaines compétences pointaient vers une catégorie n'appartenant pas à
    /// leur propre version de règles (catégorie d'une autre version, ou catégorie
    /// supprimée depuis). Conséquence : le clonage d'une version échouait avec
    /// « FOREIGN KEY constraint failed », donc plus aucune nouvelle édition
    /// possible sans développeur.
    ///
    /// On rattache chaque compétence orpheline à la catégorie de SA version qui
    /// porte le nom standard correspondant à son ancien enum `Categorie`.
    /// La catégorie manquante est créée si besoin, pour ne perdre aucune ligne.
    /// </summary>
    public partial class ReparerCategoriesOrphelines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Garantir que chaque version possède les 6 catégories standard.
            //    (INSERT ... SELECT ... WHERE NOT EXISTS = idempotent)
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
                ) c
                WHERE NOT EXISTS (
                    SELECT 1 FROM SkillCategories sc
                    WHERE sc.RulesVersionId = v.Id AND sc.Nom = c.Nom
                );");

            // 2. Réaffecter UNIQUEMENT les compétences dont la catégorie
            //    n'appartient pas à leur version (ou n'existe plus).
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
                )
                WHERE NOT EXISTS (
                    SELECT 1 FROM SkillCategories sc2
                    WHERE sc2.Id = Skills.SkillCategoryDefId
                      AND sc2.RulesVersionId = Skills.RulesVersionId
                );");

            // 3. Même traitement pour les accès de catégorie des postes,
            //    au cas où un accès pointerait hors de sa version.
            migrationBuilder.Sql(@"
                DELETE FROM PlayerPositionCategoryAccesses
                WHERE NOT EXISTS (
                    SELECT 1 FROM SkillCategories sc
                    WHERE sc.Id = PlayerPositionCategoryAccesses.SkillCategoryDefId
                );");

            migrationBuilder.Sql(@"
                DELETE FROM PoolPositionCategoryAccesses
                WHERE NOT EXISTS (
                    SELECT 1 FROM SkillCategories sc
                    WHERE sc.Id = PoolPositionCategoryAccesses.SkillCategoryDefId
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Réparation de données : rien à annuler (on ne peut pas restaurer
            // des références incohérentes, et on ne le voudrait pas).
        }
    }
}
