using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class DatabaseCleanupService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ISettingsService settingsService,
    ILogger<DatabaseCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupDatabaseAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during database cleanup.");
            }

            // Run every 12 hours (longer interval is sufficient for cleanup)
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task CleanupDatabaseAsync()
    {
        logger.LogInformation("Starting database cleanup task...");
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();

        // 1. Delete reports older than DataRetentionDays
        var reportCutoff = DateTime.UtcNow.AddDays(-settings.DataRetentionDays);
        var oldReports = await context.LocationReports
            .IgnoreQueryFilters()
            .Where(r => r.CreatedAt < reportCutoff)
            .ExecuteDeleteAsync();
        
        if (oldReports > 0)
        {
            logger.LogInformation("Cleaned up {Count} old location reports.", oldReports);
        }

        // 2. Delete unverified email verifications older than 24 hours
        var verificationCutoff = DateTime.UtcNow.AddHours(-24);
        var oldVerifications = await context.EmailVerifications
            .Where(v => v.VerifiedAt == null && v.CreatedAt < verificationCutoff)
            .ExecuteDeleteAsync();
        
        if (oldVerifications > 0)
        {
            logger.LogInformation("Cleaned up {Count} unverified email verifications.", oldVerifications);
        }

        // 3. Delete soft-deleted alerts older than DataRetentionDays
        var alertCleanupCutoff = DateTime.UtcNow.AddDays(-settings.DataRetentionDays);
        var expiredAlerts = await context.Alerts
            .IgnoreQueryFilters()
            .Where(a => a.DeletedAt != null && a.DeletedAt < alertCleanupCutoff)
            .ExecuteDeleteAsync();
            
        if (expiredAlerts > 0)
        {
            logger.LogInformation("Cleaned up {Count} soft-deleted alerts.", expiredAlerts);
        }

        // 4. Delete old admin login attempts (older than 90 days)
        var loginAttemptCutoff = DateTime.UtcNow.AddDays(-90);
        var oldLoginAttempts = await context.AdminLoginAttempts
            .Where(a => a.CreatedAt < loginAttemptCutoff)
            .ExecuteDeleteAsync();
            
        if (oldLoginAttempts > 0)
        {
            logger.LogInformation("Cleaned up {Count} old admin login attempts.", oldLoginAttempts);
        }

        // 5. Delete old feedback (older than 1 year)
        var feedbackCutoff = DateTime.UtcNow.AddYears(-1);
        var oldFeedback = await context.Feedbacks
            .IgnoreQueryFilters()
            .Where(f => f.CreatedAt < feedbackCutoff)
            .ExecuteDeleteAsync();
            
        if (oldFeedback > 0)
        {
            logger.LogInformation("Cleaned up {Count} old feedback entries.", oldFeedback);
        }

        logger.LogInformation("Database cleanup task completed.");
    }
}
