using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AlertService(IDbContextFactory<ApplicationDbContext> contextFactory, IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");

    public virtual async Task<Alert> CreateAlertAsync(Alert alert, string email)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (alert.RadiusKm > 160.9)
        {
            alert.RadiusKm = 160.9;
        }
        
        alert.EncryptedEmail = _protector.Protect(email);
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();
        return alert;
    }

    public virtual string? DecryptEmail(string? encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail)) return null;
        try
        {
            return _protector.Unprotect(encryptedEmail);
        }
        catch
        {
            return null;
        }
    }

    public virtual async Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow));

        if (!string.IsNullOrEmpty(userIdentifier))
        {
            query = query.Where(a => a.UserIdentifier == userIdentifier);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<bool> DeactivateAlertAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var alert = await context.Alerts.FindAsync(id);
        if (alert == null) return false;

        alert.IsActive = false;
        await context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude)
    {
        // For performance, we could use a bounding box first if we had thousands of alerts
        // But for now, we'll fetch active ones and filter in memory as the number of active alerts is likely manageable
        var activeAlerts = await GetActiveAlertsAsync();
        
        return activeAlerts
            .Where(a => GeoUtils.CalculateDistance(latitude, longitude, a.Latitude, a.Longitude) <= a.RadiusKm)
            .ToList();
    }
}
