using BolDeSangManager.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Services;

public class AuthorizationService(
    ApplicationDbContext db,
    UserManager<Data.ApplicationUser> userManager)
    : IAuthorizationService
{
    public async Task<bool> EstAdminAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.IsInRoleAsync(user, "Admin");
    }

    public async Task<bool> EstGrandCommissaireAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.IsInRoleAsync(user, "GrandCommissaire");
    }

    public async Task<bool> EstCommissaireDeLigueAsync(string userId, int ligueId)
    {
        if (string.IsNullOrEmpty(userId) || ligueId <= 0) return false;
        return await db.LeagueCommissioners
            .AnyAsync(lc => lc.UserId == userId && lc.LeagueId == ligueId);
    }

    public async Task<bool> PeutGererLigueAsync(string userId, int ligueId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        if (await EstAdminAsync(userId)) return true;
        if (await EstGrandCommissaireAsync(userId)) return true;
        return await EstCommissaireDeLigueAsync(userId, ligueId);
    }

    public async Task<bool> PeutEditerDonneesAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        if (await EstAdminAsync(userId)) return true;
        return await EstGrandCommissaireAsync(userId);
    }

    public Task<bool> PeutGererSettingsAsync(string userId)
        => EstAdminAsync(userId);
}
