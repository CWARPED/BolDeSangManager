using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <summary>
    /// #19 — calendrier partagé décalé de 1 à 2 h.
    ///
    /// Jusqu'à v2026.11.02, l'image Docker n'embarquait aucune base de fuseaux :
    /// le serveur tournait en UTC et <c>ProgrammationDialog</c> (« heure locale
    /// → UTC ») stockait l'heure de PARIS telle quelle dans
    /// <c>Matches.DateProgrammee</c>. L'appli l'affichait juste (aucune
    /// conversion), mais le .ics et le lien Google la lisaient comme de l'UTC.
    /// L'image tourne désormais en Europe/Paris : on convertit les valeurs
    /// existantes en vrais instants UTC (−1 h en hiver, −2 h en heure d'été,
    /// bascules du dernier dimanche de mars à 02:00 et d'octobre à 03:00).
    ///
    /// Argument de sûreté : <c>ProgrammationDialog</c> est le SEUL écrivain de
    /// cette colonne, et l'image n'a jamais eu de fuseau avant cette version —
    /// toute valeur présente est donc une heure de Paris. Corroboré par le
    /// signalement #19 (décalage de 2 h en septembre = heure d'été).
    ///
    /// ⚠️ NON idempotente : sa sûreté repose sur __EFMigrationsHistory (une seule
    /// exécution). Ne jamais rejouer ce SQL à la main. Une instance hors Docker
    /// dont le serveur était déjà à l'heure de Paris (poste de dev) voit ses
    /// dates décalées à tort — sans importance pour des données de test.
    /// SQL rejoué mot pour mot par <c>ConversionDatesProgrammeesTests</c>.
    /// </summary>
    public partial class ConvertirDatesProgrammeesEnUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Matches
SET DateProgrammee = strftime('%Y-%m-%d %H:%M:%S', DateProgrammee,
    CASE
        WHEN DateProgrammee >= date(strftime('%Y', DateProgrammee) || '-04-01', 'weekday 0', '-7 days') || ' 02:00:00'
         AND DateProgrammee <  date(strftime('%Y', DateProgrammee) || '-11-01', 'weekday 0', '-7 days') || ' 03:00:00'
        THEN '-2 hours'
        ELSE '-1 hours'
    END)
WHERE DateProgrammee IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volontairement vide : revenir aux heures de Paris déguisées en UTC
            // recréerait le décalage du calendrier partagé.
        }
    }
}
