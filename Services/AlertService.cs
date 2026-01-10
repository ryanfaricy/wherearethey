using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AlertService
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _protector;

    public AlertService(ApplicationDbContext context, IDataProtectionProvider provider)
    {
        _context = context;
        _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");
    }

    public virtual async Task<Alert> CreateAlertAsync(Alert alert, string email)
    {
        if (alert.RadiusKm > 160.9)
        {
            alert.RadiusKm = 160.9;
        }
        
        alert.EncryptedEmail = _protector.Protect(email);
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
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
            return "Error decrypting email";
        }
    }

    public virtual async Task<List<Alert>> GetActiveAlertsAsync()
    {
        return await _context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();
    }

    public virtual async Task<bool> DeactivateAlertAsync(int id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert == null) return false;

        alert.IsActive = false;
        await _context.SaveChangesAsync();
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
