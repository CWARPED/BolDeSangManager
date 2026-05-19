using BolDeSangManager.Services;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetAsync_CleInexistante_RetourneNull()
    {
        await using var db = _factory.CreateContext();
        var svc = new SettingsService(db);
        var result = await svc.GetAsync("CleQuiNExistePas");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_NouvelleCle_CreeLEntree()
    {
        await using var db1 = _factory.CreateContext();
        var svc = new SettingsService(db1);
        await svc.SetAsync("UrlExterne", "https://monapp.example.com");

        await using var db2 = _factory.CreateContext();
        var config = await db2.AppConfigs.FirstOrDefaultAsync(c => c.Cle == "UrlExterne");
        Assert.NotNull(config);
        Assert.Equal("https://monapp.example.com", config.Valeur);
    }

    [Fact]
    public async Task SetAsync_CleExistante_MetsAJourLaValeur()
    {
        await using var db1 = _factory.CreateContext();
        await new SettingsService(db1).SetAsync("UrlExterne", "https://ancienne.url");

        await using var db2 = _factory.CreateContext();
        await new SettingsService(db2).SetAsync("UrlExterne", "https://nouvelle.url");

        await using var db3 = _factory.CreateContext();
        var configs = await db3.AppConfigs.Where(c => c.Cle == "UrlExterne").ToListAsync();
        Assert.Single(configs);
        Assert.Equal("https://nouvelle.url", configs[0].Valeur);
    }

    [Fact]
    public async Task GetAsync_ApresSetAsync_RetourneLaValeur()
    {
        await using var db1 = _factory.CreateContext();
        await new SettingsService(db1).SetAsync(SettingsService.CleUrlExterne, "https://test.local");

        await using var db2 = _factory.CreateContext();
        var valeur = await new SettingsService(db2).GetAsync(SettingsService.CleUrlExterne);
        Assert.Equal("https://test.local", valeur);
    }
}
