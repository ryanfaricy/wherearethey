using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using GeoTimeZone;
using TimeZoneConverter;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <summary>
/// Service for specialized location-based logic.
/// </summary>
public class LocationService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    ISettingsService settingsService,
    ILogger<LocationService> logger) : ILocationService
{
    /// <summary>
    /// Gets reports within a specific radius of a location.
    /// Uses bounding box filter followed by Haversine distance calculation for performance.
    /// </summary>
    public async Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var settings = await settingsService.GetSettingsAsync();

            // Use the global expiry setting
            var cutoff = DateTime.UtcNow.AddHours(-settings.ReportExpiryHours);
            
            // Step 1: Simple bounding box calculation to filter at DB level
            var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(latitude, longitude, radiusKm);

            var reports = await context.LocationReports
                .AsNoTracking()
                .Where(r => r.Timestamp >= cutoff &&
                           r.Latitude >= minLat && r.Latitude <= maxLat &&
                           r.Longitude >= minLon && r.Longitude <= maxLon)
                .ToListAsync();

            // Step 2: Filter by actual distance using Haversine formula in memory
            return reports.Where(r => GeoUtils.CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting reports in radius at {Lat}, {Lng}", latitude, longitude);
            throw;
        }
    }

    /// <summary>
    /// Determines the local time at a given coordinate and returns it as a formatted string.
    /// </summary>
    public string GetFormattedLocalTime(double latitude, double longitude, DateTime utcTimestamp)
    {
        try
        {
            var tzResult = TimeZoneLookup.GetTimeZone(latitude, longitude);
            var tzInfo = TZConvert.GetTimeZoneInfo(tzResult.Result);
            
            var utcTime = utcTimestamp.Kind == DateTimeKind.Utc 
                ? utcTimestamp 
                : DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc);
            
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzInfo);
            return $"{localTime:g} ({tzResult.Result})";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to determine local time for {Lat}, {Lng}", latitude, longitude);
            return $"{utcTimestamp:g} UTC";
        }
    }
}
