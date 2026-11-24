using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <inheritdoc />
    public partial class CocherCoupsDePouceStarPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Coups de pouce « Star player » déjà créés par l'association
            // (« Star Player 1 », « Star Player 2 », « Star Players » du seed) :
            // cochés une fois, pour que la recherche de star soit disponible dès
            // la mise à jour. Décision produit du 24/11/2026. Joué une seule fois
            // (__EFMigrationsHistory) : une case décochée ensuite en admin reste
            // décochée.
            migrationBuilder.Sql(
                "UPDATE Inducements SET EstStarPlayer = 1 WHERE lower(Nom) LIKE 'star player%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rien à défaire : la colonne disparaît avec la migration précédente.
        }
    }
}
