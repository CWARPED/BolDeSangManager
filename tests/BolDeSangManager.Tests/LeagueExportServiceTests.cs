using System.Text;
using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class LeagueExportServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private LeagueExportService CreateService(ApplicationDbContext db) =>
        new(db, NullLogger<LeagueExportService>.Instance);

    // ─── Setup ────────────────────────────────────────────────────────────────

    private async Task<(League ligue, ApplicationUser commissaire)> SetupLigueAvecEquipesAsync()
    {
        await using var db = _factory.CreateContext();

        var commissaire = DataSeeder.CreateUser("expcomm");
        var coach1 = DataSeeder.CreateUser("expc1");
        var coach2 = DataSeeder.CreateUser("expc2");
        db.Users.AddRange(commissaire, coach1, coach2);
        await db.SaveChangesAsync();

        var (game, rv) = await DataSeeder.SeedGameAsync(db);
        var (teamType, position) = await DataSeeder.SeedTeamTypeAsync(db, game.Id);
        var ligue = await DataSeeder.SeedLeagueAsync(db, game.Id, rv.Id, commissaire.Id);

        var t1 = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach1.Id, teamType.Id, "Équipe Alpha");
        var t2 = await DataSeeder.SeedTeamAsync(db, ligue.Id, coach2.Id, teamType.Id, "Équipe Bêta");

        await DataSeeder.SeedPlayerAsync(db, t1.Id, position.Id, "Gromag", 1);
        await DataSeeder.SeedPlayerAsync(db, t2.Id, position.Id, "Skulkar", 1);

        return (ligue, commissaire);
    }

    // ─── ExportAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_ProduitsOctets()
    {
        var (ligue, _) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task Export_JsonContientNomLigue()
    {
        var (ligue, _) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains(ligue.Nom, json);
    }

    [Fact]
    public async Task Export_JsonContientNomsEquipes()
    {
        var (ligue, _) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Équipe Alpha", json);
        Assert.Contains("Équipe Bêta", json);
    }

    [Fact]
    public async Task Export_JsonContientNomsJoueurs()
    {
        var (ligue, _) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Gromag", json);
        Assert.Contains("Skulkar", json);
    }

    [Fact]
    public async Task Export_LigueInexistante_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ExportAsync(99999));
    }

    // ─── ImportAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Import_RecreeLaLigue()
    {
        var (ligue, commissaire) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var importedLigue = await svc.ImportAsync(new MemoryStream(bytes), commissaire.Id);

        Assert.NotNull(importedLigue);
        Assert.NotEqual(ligue.Id, importedLigue.Id);  // Nouvel ID
        Assert.Contains(ligue.Nom, importedLigue.Nom);
    }

    [Fact]
    public async Task Import_PreserveNombreEquipes()
    {
        var (ligue, commissaire) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var importedLigue = await svc.ImportAsync(new MemoryStream(bytes), commissaire.Id);

        await using var db2 = _factory.CreateContext();
        var nbEquipes = await db2.Teams.CountAsync(t => t.LeagueId == importedLigue.Id);
        Assert.Equal(2, nbEquipes);
    }

    [Fact]
    public async Task Import_PreserveJoueurs()
    {
        var (ligue, commissaire) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var importedLigue = await svc.ImportAsync(new MemoryStream(bytes), commissaire.Id);

        await using var db2 = _factory.CreateContext();
        var teamIds = await db2.Teams.Where(t => t.LeagueId == importedLigue.Id)
            .Select(t => t.Id).ToListAsync();
        var nbJoueurs = await db2.TeamPlayers.CountAsync(j => teamIds.Contains(j.TeamId));
        Assert.Equal(2, nbJoueurs);  // 1 joueur par équipe
    }

    [Fact]
    public async Task Import_StatutTermine()
    {
        var (ligue, commissaire) = await SetupLigueAvecEquipesAsync();
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var bytes = await svc.ExportAsync(ligue.Id);
        var importedLigue = await svc.ImportAsync(new MemoryStream(bytes), commissaire.Id);

        Assert.Equal(LeagueStatus.Termine, importedLigue.Statut);
    }

    [Fact]
    public async Task Import_JsonInvalide_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        var badJson = new MemoryStream(Encoding.UTF8.GetBytes("{ pas du json valide }"));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            svc.ImportAsync(badJson, "any-id"));
    }

    [Fact]
    public async Task Import_JeuInconnu_ThrowsException()
    {
        await using var db = _factory.CreateContext();
        var svc = CreateService(db);

        // JSON valide mais référence un jeu inexistant
        var json = """
            {
              "version": "1.0",
              "exporteeLe": "2025-01-01T00:00:00Z",
              "nomLigue": "Fantôme",
              "description": "",
              "gameNom": "JeuInconnu",
              "rulesVersionNom": null,
              "format": "RoundRobinAvecPlayoffs",
              "budgetDepart": 1000000,
              "nombreEquipesPlayoff": 4,
              "equipes": [],
              "matchs": []
            }
            """;
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ImportAsync(stream, "any-id"));
    }

    // ─── Cycle export → import (round-trip) ──────────────────────────────────

    [Fact]
    public async Task ExportImport_PreserveStatsEquipes()
    {
        var (ligue, commissaire) = await SetupLigueAvecEquipesAsync();

        // Modifier les stats d'une équipe avant export
        await using var db = _factory.CreateContext();
        var equipe = await db.Teams.FirstAsync(t => t.LeagueId == ligue.Id);
        equipe.NombreVictoires = 5;
        equipe.PointsLigue = 15;
        equipe.TouchdownsMarques = 12;
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var bytes = await svc.ExportAsync(ligue.Id);
        var importedLigue = await svc.ImportAsync(new MemoryStream(bytes), commissaire.Id);

        await using var db2 = _factory.CreateContext();
        var importedEquipe = await db2.Teams
            .FirstAsync(t => t.LeagueId == importedLigue.Id && t.Nom == equipe.Nom);
        Assert.Equal(5, importedEquipe.NombreVictoires);
        Assert.Equal(15, importedEquipe.PointsLigue);
        Assert.Equal(12, importedEquipe.TouchdownsMarques);
    }
}
