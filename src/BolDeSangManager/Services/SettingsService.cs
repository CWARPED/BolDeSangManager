using BolDeSangManager.Data;
using BolDeSangManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class SettingsService(ApplicationDbContext db)
{
    public const string CleUrlExterne         = "UrlExterne";
    public const string CleEmailExpediteur    = "EmailExpediteur";
    public const string CleEmailNomExpediteur = "EmailNomExpediteur";
    public const string CleEmailMotDePasse    = "EmailMotDePasse";

    public async Task<string?> GetAsync(string cle)
    {
        var config = await db.AppConfigs.FirstOrDefaultAsync(c => c.Cle == cle);
        return config?.Valeur;
    }

    public async Task SetAsync(string cle, string valeur)
    {
        var config = await db.AppConfigs.FirstOrDefaultAsync(c => c.Cle == cle);
        if (config is null)
            db.AppConfigs.Add(new AppConfig { Cle = cle, Valeur = valeur });
        else
            config.Valeur = valeur;
        await db.SaveChangesAsync();
    }
}