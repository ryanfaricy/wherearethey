using System.Globalization;
using GeoTimeZone;
using TimeZoneConverter;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

using Microsoft.Extensions.Localization;
using WhereAreThey.Components;

namespace WhereAreThey.Services;

public class LocationService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IServiceProvider serviceProvider, 
    ILogger<LocationService> logger, 
    IConfiguration configuration,
    SettingsService settingsService,
    IStringLocalizer<App> L)
{
    public event Action? OnReportAdded;

    public async Task<LocationReport> AddLocationReportAsync(LocationReport report)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();

        // Anti-spam: check cooldown
        var cooldownLimit = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
        
        if (string.IsNullOrEmpty(report.ReporterIdentifier))
        {
            throw new InvalidOperationException(L["Identifier_Error"]);
        }

        var hasRecentReport = await context.LocationReports
            .AnyAsync(r => r.ReporterIdentifier == report.ReporterIdentifier && r.Timestamp >= cooldownLimit);

        if (hasRecentReport)
        {
            throw new InvalidOperationException(string.Format(L["Cooldown_Error"], settings.ReportCooldownMinutes));
        }

        // Anti-spam: basic message validation
        if (!string.IsNullOrEmpty(report.Message))
        {
            if (report.Message.Contains("http://") || report.Message.Contains("https://") || report.Message.Contains("www."))
            {
                throw new InvalidOperationException(L["Links_Error"]);
            }
        }

        // Anti-spam: check distance
        if (report.ReporterLatitude.HasValue && report.ReporterLongitude.HasValue)
        {
            var distance = GeoUtils.CalculateDistance(report.Latitude, report.Longitude,
                report.ReporterLatitude.Value, report.ReporterLongitude.Value);

            // Convert miles to km for distance calculation (1 mile ≈ 1.60934 km)
            var maxDistanceKm = (double)settings.MaxReportDistanceMiles * 1.60934;
            if (distance > maxDistanceKm)
            {
                throw new InvalidOperationException(string.Format(L["Distance_Error"], settings.MaxReportDistanceMiles));
            }
        }
        else
        {
            // We require reporter location for non-emergency reports
            // Emergency reports might be allowed without location if it's a critical failure, 
            // but the rule says "a user can only make a report within five miles"
            throw new InvalidOperationException(L["Location_Verify_Error"]);
        }

        report.Timestamp = DateTime.UtcNow;
        context.LocationReports.Add(report);
        await context.SaveChangesAsync();

        OnReportAdded?.Invoke();

        // Process alerts in the background to not block the reporter
        _ = Task.Run(async () => await ProcessAlertsForReport(report));

        return report;
    }

    private async Task ProcessAlertsForReport(LocationReport report)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var geocodingService = scope.ServiceProvider.GetRequiredService<GeocodingService>();
            var settings = await settingsService.GetSettingsAsync();

            var matchingAlerts = await alertService.GetMatchingAlertsAsync(report.Latitude, report.Longitude);
            
            var baseUrl = configuration["BaseUrl"] ?? "https://aretheyhere.com";

            // Approximate address
            var address = await geocodingService.ReverseGeocodeAsync(report.Latitude, report.Longitude);

            // Determine local time
            string localTimeStr;
            try
            {
                var tzResult = TimeZoneLookup.GetTimeZone(report.Latitude, report.Longitude);
                var tzInfo = TZConvert.GetTimeZoneInfo(tzResult.Result);
                
                // Ensure we have a UTC DateTime before conversion to avoid Kind-related issues
                var utcTime = report.Timestamp.Kind == DateTimeKind.Utc 
                    ? report.Timestamp 
                    : DateTime.SpecifyKind(report.Timestamp, DateTimeKind.Utc);
                
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzInfo);
                localTimeStr = $"{localTime:g} ({tzResult.Result})";
            }
            catch
            {
                localTimeStr = $"{report.Timestamp:g} UTC";
            }

            // Map thumbnail
            var mapThumbnailHtml = "";
            var heatMapUrl = $"{baseUrl}/?reportId={report.ExternalId}";

            if (!string.IsNullOrEmpty(settings.MapboxToken))
            {
                var mapUrl = $"{baseUrl}/api/map/proxy?reportId={report.ExternalId}";
                mapThumbnailHtml = $"<p><a href='{heatMapUrl}'><img src='{mapUrl}' alt='Map Location' style='max-width: 100%; height: auto; border-radius: 8px;' /></a></p>";
            }

            foreach (var alert in matchingAlerts)
            {
                var email = alertService.DecryptEmail(alert.EncryptedEmail);
                if (!string.IsNullOrEmpty(email))
                {
                    var subject = report.IsEmergency ? "EMERGENCY: Report in your area!" : "Alert: New report in your area";
                    var body = $@"
                        <h3>New report near your alert area</h3>
                        {(string.IsNullOrEmpty(alert.Message) ? "" : $"<p><strong>Your Alert:</strong> {alert.Message}</p>")}
                        <p><strong>Location:</strong> {report.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {report.Longitude.ToString("F4", CultureInfo.InvariantCulture)}</p>
                        {(!string.IsNullOrEmpty(address) ? $"<p><strong>Approx. Address:</strong> {address}</p>" : "")}
                        <p><strong>Time:</strong> {localTimeStr}</p>
                        {(report.IsEmergency ? "<p style='color: red; font-weight: bold;'>THIS IS MARKED AS AN EMERGENCY</p>" : "")}
                        {(string.IsNullOrEmpty(report.Message) ? "" : $"<p><strong>Message:</strong> {report.Message}</p>")}
                        {mapThumbnailHtml}
                        <hr/>
                        <p><a href='{heatMapUrl}'>View on Heat Map</a></p>
                        <small>You received this because you set up an alert on AreTheyHere.</small>";

                    await emailService.SendEmailAsync(email!, subject, body);
                }
                else if (!string.IsNullOrEmpty(alert.EncryptedEmail))
                {
                    logger.LogWarning("Failed to decrypt email for alert {AlertId}. The encryption keys may have changed.", alert.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing alerts for report {ReportId}", report.Id);
        }
    }

    public async Task<LocationReport?> GetReportByExternalIdAsync(Guid externalId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports.FirstOrDefaultAsync(r => r.ExternalId == externalId);
    }

    public async Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        return await context.LocationReports
            .Where(r => r.Timestamp >= cutoff)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();

        // Use the global expiry setting
        var cutoff = DateTime.UtcNow.AddHours(-settings.ReportExpiryHours);
        
        // Simple bounding box calculation (approximation)
        var latDelta = radiusKm / 111.0; // 1 degree latitude ≈ 111 km
        var lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

        var minLat = latitude - latDelta;
        var maxLat = latitude + latDelta;
        var minLon = longitude - lonDelta;
        var maxLon = longitude + lonDelta;

        var reports = await context.LocationReports
            .Where(r => r.Timestamp >= cutoff &&
                       r.Latitude >= minLat && r.Latitude <= maxLat &&
                       r.Longitude >= minLon && r.Longitude <= maxLon)
            .ToListAsync();

        // Filter by actual distance using Haversine formula
        return reports.Where(r => GeoUtils.CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
            .ToList();
    }

    // Admin methods
    public async Task<List<LocationReport>> GetAllReportsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task DeleteReportAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var report = await context.LocationReports.FindAsync(id);
        if (report != null)
        {
            context.LocationReports.Remove(report);
            await context.SaveChangesAsync();
            OnReportAdded?.Invoke();
        }
    }
}
