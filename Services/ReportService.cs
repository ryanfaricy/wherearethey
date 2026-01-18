using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ReportService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IBackgroundJobClient backgroundJobClient,
    ISettingsService settingsService,
    IEventService eventService,
    IValidator<LocationReport> validator,
    ILogger<ReportService> logger) : IReportService
{
    /// <inheritdoc />
    public async Task<Result<LocationReport>> AddReportAsync(LocationReport report)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(report);
            if (!validationResult.IsValid)
            {
                return Result<LocationReport>.Failure(validationResult);
            }

            await using var context = await contextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.Timestamp = DateTime.UtcNow;
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();

            // Notify global event bus
            eventService.NotifyReportAdded(report);

            try
            {
                // Enqueue background processing (geocoding, alerts) using Hangfire
                backgroundJobClient.Enqueue<IReportProcessingService>(service => service.ProcessReportAsync(report));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enqueuing report processing job");
            }

            return Result<LocationReport>.Success(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding location report at {Lat}, {Lng}", report.Latitude, report.Longitude);
            return Result<LocationReport>.Failure("An error occurred while adding the location report.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<LocationReport>> GetReportByExternalIdAsync(Guid externalId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var report = await context.LocationReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);

        return report != null ? Result<LocationReport>.Success(report) : Result<LocationReport>.Failure("Report not found.");
    }

    /// <inheritdoc />
    public async Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        return await context.LocationReports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null && r.Timestamp >= cutoff)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<LocationReport>> GetAllReportsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LocationReports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteReportAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var report = await context.LocationReports.FindAsync(id);
        if (report == null)
        {
            return Result.Failure("Report not found.");
        }

        report.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        // Notify global event bus
        eventService.NotifyReportDeleted(id);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateReportAsync(LocationReport report)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(report);
            if (!validationResult.IsValid)
            {
                return Result.Failure(validationResult);
            }

            await using var context = await contextFactory.CreateDbContextAsync();
            context.LocationReports.Update(report);
            await context.SaveChangesAsync();
            
            // Notify global event bus
            eventService.NotifyReportUpdated(report);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating report {ReportId}", report.Id);
            return Result.Failure("An error occurred while updating the report.");
        }
    }
}
