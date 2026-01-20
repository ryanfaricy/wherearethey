using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class SettingsService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService,
    ILogger<SettingsService> logger) : ISettingsService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private SystemSettings? _cachedSettings;
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async Task<SystemSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null && DateTime.UtcNow - _lastUpdate < _cacheDuration)
        {
            return _cachedSettings;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_cachedSettings != null && DateTime.UtcNow - _lastUpdate < _cacheDuration)
            {
                return _cachedSettings;
            }

            logger.LogDebug("Fetching system settings from database");
            await using var context = await contextFactory.CreateDbContextAsync();
            _cachedSettings = await context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync() ?? new SystemSettings();
            
            // Auto-generate VAPID keys if missing
            if (string.IsNullOrEmpty(_cachedSettings.VapidPublicKey) || string.IsNullOrEmpty(_cachedSettings.VapidPrivateKey))
            {
                logger.LogInformation("VAPID keys missing, generating new keys");
                var keys = WebPush.VapidHelper.GenerateVapidKeys();
                _cachedSettings.VapidPublicKey = keys.PublicKey;
                _cachedSettings.VapidPrivateKey = keys.PrivateKey;
                
                // Save them back to DB
                var dbSettings = await context.Settings.FirstOrDefaultAsync();
                if (dbSettings != null)
                {
                    dbSettings.VapidPublicKey = _cachedSettings.VapidPublicKey;
                    dbSettings.VapidPrivateKey = _cachedSettings.VapidPrivateKey;
                    await context.SaveChangesAsync();
                }
            }

            _lastUpdate = DateTime.UtcNow;

            return _cachedSettings;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateSettingsAsync(SystemSettings settings)
    {
        logger.LogInformation("Updating system settings");
        await _semaphore.WaitAsync();
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var existing = await context.Settings.FirstOrDefaultAsync();
            
            if (existing == null)
            {
                logger.LogInformation("No existing settings found, adding new ones");
                context.Settings.Add(settings);
            }
            else
            {
                logger.LogDebug("Updating existing system settings");
                existing.ReportExpiryHours = settings.ReportExpiryHours;
                existing.ReportCooldownMinutes = settings.ReportCooldownMinutes;
                existing.AlertLimitCount = settings.AlertLimitCount;
                existing.MaxReportDistanceMiles = settings.MaxReportDistanceMiles;
                existing.MapboxToken = settings.MapboxToken;
                existing.DonationsEnabled = settings.DonationsEnabled;
                existing.EmailNotificationsEnabled = settings.EmailNotificationsEnabled;
                existing.PushNotificationsEnabled = settings.PushNotificationsEnabled;
                existing.DataRetentionDays = settings.DataRetentionDays;
            }

            await context.SaveChangesAsync();
            _cachedSettings = settings;
            _lastUpdate = DateTime.UtcNow;
            eventService.NotifySettingsChanged(settings);
            logger.LogInformation("System settings updated successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating system settings");
            return Result.Failure(ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
