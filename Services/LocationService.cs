using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Events;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

/// <summary>
/// Service for managing location reports.
/// </summary>
public class LocationService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IMediator mediator,
    ISettingsService settingsService,
    IAdminNotificationService adminNotificationService,
    IValidator<LocationReport> validator,
    ILogger<LocationService> logger,
    IStringLocalizer<App> L) : ILocationService
{
    /// <summary>
    /// Event triggered when a new report is added.
    /// </summary>
    public event Action<LocationReport>? OnReportAdded;

    /// <summary>
    /// Event triggered when a report is deleted.
    /// </summary>
    public event Action<int>? OnReportDeleted;

    /// <summary>
    /// Validates and adds a new location report to the database.
    /// </summary>
    /// <param name="report">The report to add.</param>
    /// <returns>The added report with its generated ID and timestamp.</returns>
    public async Task<LocationReport> AddLocationReportAsync(LocationReport report)
    {
        try
        {
            await validator.ValidateAndThrowAsync(report);

            await using var context = await contextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.Timestamp = DateTime.UtcNow;
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();

            // Notify real-time listeners (Blazor SignalR)
            OnReportAdded?.Invoke(report);
            
            // Notify admin UI
            adminNotificationService.NotifyReportAdded(report);

            try
            {
                // Publish event for background processing (geocoding, alerts)
                await mediator.Publish(new ReportAddedEvent(report));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing report added event");
            }

            return report;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding location report at {Lat}, {Lng}", report.Latitude, report.Longitude);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a report by its public External ID.
    /// </summary>
    public async Task<LocationReport?> GetReportByExternalIdAsync(Guid externalId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);
    }

    /// <summary>
    /// Gets recent reports based on the configured expiry hours.
    /// </summary>
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

    /// <summary>
    /// Gets reports within a specific radius of a location.
    /// Uses bounding box filter followed by Haversine distance calculation for performance.
    /// </summary>
    public async Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm)
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

    // Admin methods
    
    /// <summary>
    /// Gets all reports for administrative management.
    /// </summary>
    public async Task<List<LocationReport>> GetAllReportsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Deletes a report and notifies relevant services.
    /// </summary>
    public async Task DeleteReportAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var report = await context.LocationReports.FindAsync(id);
        if (report != null)
        {
            context.LocationReports.Remove(report);
            await context.SaveChangesAsync();
            
            // Notify real-time listeners
            OnReportDeleted?.Invoke(id);
            
            // Notify admin UI
            adminNotificationService.NotifyReportDeleted(id);
        }
    }
}
