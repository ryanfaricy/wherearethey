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

    public async Task<Alert> CreateAlertAsync(Alert alert, string email)
    {
        alert.EncryptedEmail = _protector.Protect(email);
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public string? DecryptEmail(string? encryptedEmail)
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

    public async Task<List<Alert>> GetActiveAlertsAsync()
    {
        return await _context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();
    }

    public async Task<bool> DeactivateAlertAsync(int id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert == null) return false;

        alert.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
