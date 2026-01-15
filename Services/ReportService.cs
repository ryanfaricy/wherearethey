using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Events;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <summary>
/// Service for managing the lifecycle of location reports.
/// </summary>
public class ReportService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IMediator mediator,
    ISettingsService settingsService,
    IEventService eventService,
    IValidator<LocationReport> validator,
    ILogger<ReportService> logger) : IReportService
{
    /// <summary>
    /// Validates and adds a new location report to the database.
    /// </summary>
    public async Task<LocationReport> AddReportAsync(LocationReport report)
    {
        try
        {
            await validator.ValidateAndThrowAsync(report);

            await using var context = await contextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.Timestamp = DateTime.UtcNow;
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();

            // Notify global event bus
            eventService.NotifyReportAdded(report);

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
            
            // Notify global event bus
            eventService.NotifyReportDeleted(id);
        }
    }
}
