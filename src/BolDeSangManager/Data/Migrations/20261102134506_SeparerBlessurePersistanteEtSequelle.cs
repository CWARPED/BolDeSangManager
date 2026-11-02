using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolDeSangManager.Migrations
{
    /// <summary>
    /// Ticket #26 — « une BP n'est pas une blessure avec Séquelle ».
    ///
    /// Avant ce correctif, <c>InjuryType.BlessurePersistante</c> (1) appliquait
    /// en réalité une SÉQUELLE : −1 sur la caractéristique choisie, rien d'autre.
    /// La Séquelle a désormais sa propre valeur (<c>Sequelle</c> = 4) et la BP
    /// redevient une vraie Blessure Persistante (aucune caractéristique).
    ///
    /// Argument de sûreté : le SEUL effet qu'une ligne « BlessurePersistante »
    /// portant une caractéristique ait jamais eu est la réduction de cette
    /// caractéristique — c'est exactement une Séquelle. On ne change que le nom
    /// du type : aucun profil de joueur n'est modifié (la réduction est déjà
    /// appliquée) et aucun RPM n'est posé après coup. Une ligne SANS
    /// caractéristique n'a jamais eu d'effet : on ne la devine pas, elle reste
    /// une BP.
    ///
    /// ⚠️ ONE-SHOT, valable pour les données antérieures à #26 uniquement.
    /// SQL rejoué mot pour mot par <c>BlessuresTests.SqlBackfill</c>.
    /// </summary>
    public partial class SeparerBlessurePersistanteEtSequelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE PlayerInjuries
SET Type = 4,
    Description = REPLACE(Description, 'Blessure persistante : ', 'Séquelle : ')
WHERE Type = 1 AND StatAffectee IS NOT NULL;
UPDATE MatchPlayerRecords
SET Blessure = 4
WHERE Blessure = 1 AND StatAffectee IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volontairement vide : revenir en arrière recréerait la confusion
            // BP/Séquelle que cette migration corrige.
        }
    }
}
