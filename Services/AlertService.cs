using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AlertService
{
    private readonly ApplicationDbContext _context;

    public AlertService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Alert> CreateAlertAsync(Alert alert)
    {
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
        return alert;
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
