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
    ILogger<ReportService> logger) : BaseService<LocationReport>(contextFactory, eventService), IReportService
{
    /// <inheritdoc />
    public async Task<Result<LocationReport>> CreateReportAsync(LocationReport report)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(report);
            if (!validationResult.IsValid)
            {
                return Result<LocationReport>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.CreatedAt = DateTime.UtcNow;
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();

            // Notify global event bus
            EventService.NotifyEntityChanged(report, EntityChangeType.Added);

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
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.LocationReports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);

        return report != null ? Result<LocationReport>.Success(report) : Result<LocationReport>.Failure("Report not found.");
    }

    /// <inheritdoc />
    public async Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        return await context.LocationReports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null && r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<LocationReport>> GetAllReportsAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteReportAsync(int id)
    {
        return await SoftDeleteAsync(id);
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

            await using var context = await ContextFactory.CreateDbContextAsync();
            var existing = await context.LocationReports
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == report.Id);
            
            if (existing == null)
            {
                return Result.Failure("Report not found.");
            }

            existing.CreatedAt = report.CreatedAt;
            existing.DeletedAt = report.DeletedAt;
            existing.ExternalId = report.ExternalId;
            existing.IsEmergency = report.IsEmergency;
            existing.Latitude = report.Latitude;
            existing.Longitude = report.Longitude;
            existing.Message = report.Message;
            existing.ReporterIdentifier = report.ReporterIdentifier;
            existing.ReporterLatitude = report.ReporterLatitude;
            existing.ReporterLongitude = report.ReporterLongitude;
            
            await context.SaveChangesAsync();
            
            // Notify global event bus
            EventService.NotifyEntityChanged(report, EntityChangeType.Updated);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating report {ReportId}", report.Id);
            return Result.Failure("An error occurred while updating the report.");
        }
    }
}
