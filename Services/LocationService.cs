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
    IReportProcessingService reportProcessingService,
    ISettingsService settingsService,
    ISubmissionValidator validator,
    IStringLocalizer<App> L) : ILocationService
{
    public event Action<LocationReport?>? OnReportAdded;

    public async Task<LocationReport> AddLocationReportAsync(LocationReport report)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();

        // Anti-spam validation
        await validator.ValidateLocationReportCooldownAsync(report.ReporterIdentifier, settings.ReportCooldownMinutes);
        validator.ValidateNoLinks(report.Message, "Links_Error");

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

        OnReportAdded?.Invoke(report);

        // Process alerts in the background to not block the reporter
        _ = Task.Run(async () => await reportProcessingService.ProcessReportAsync(report));

        return report;
    }

    public async Task<LocationReport?> GetReportByExternalIdAsync(Guid externalId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);
    }

    public async Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        return await context.LocationReports
            .AsNoTracking()
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
        
        // Simple bounding box calculation
        var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(latitude, longitude, radiusKm);

        var reports = await context.LocationReports
            .AsNoTracking()
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
            .AsNoTracking()
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
            OnReportAdded?.Invoke(null);
        }
    }
}
