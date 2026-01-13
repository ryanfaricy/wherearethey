using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class SettingsService(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    private SystemSettings? _cachedSettings;
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);

    public async Task<SystemSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null && DateTime.UtcNow - _lastUpdate < _cacheDuration)
        {
            return _cachedSettings;
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        _cachedSettings = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync() ?? new SystemSettings();
        _lastUpdate = DateTime.UtcNow;

        return _cachedSettings;
    }

    public async Task UpdateSettingsAsync(SystemSettings settings)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var existing = await context.Settings.FirstOrDefaultAsync();
        
        if (existing == null)
        {
            context.Settings.Add(settings);
        }
        else
        {
            existing.ReportExpiryHours = settings.ReportExpiryHours;
            existing.ReportCooldownMinutes = settings.ReportCooldownMinutes;
            existing.MaxReportDistanceMiles = settings.MaxReportDistanceMiles;
            existing.MapboxToken = settings.MapboxToken;
            existing.DonationsEnabled = settings.DonationsEnabled;
        }

        await context.SaveChangesAsync();
        _cachedSettings = settings;
        _lastUpdate = DateTime.UtcNow;
    }
}
