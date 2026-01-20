using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc cref="BaseService{T}" />
public class ReportService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IBackgroundJobClient backgroundJobClient,
    ISettingsService settingsService,
    IEventService eventService,
    IBaseUrlProvider baseUrlProvider,
    IValidator<Report> validator,
    ILogger<ReportService> logger) : BaseService<Report>(contextFactory, eventService, logger, validator), IReportService
{
    /// <inheritdoc />
    public async Task<Result<Report>> CreateReportAsync(Report report)
    {
        Logger.LogInformation("Creating new report at {Lat}, {Lng} by user {ReporterIdentifier}", report.Latitude, report.Longitude, report.ReporterIdentifier);
        try
        {
            var validationResult = await Validator!.ValidateAsync(report);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Report validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result<Report>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.CreatedAt = DateTime.UtcNow;
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            Logger.LogInformation("Report {ReportId} created with ExternalId {ExternalId}", report.Id, report.ExternalId);
            // Notify global event bus
            EventService.NotifyEntityChanged(report, EntityChangeType.Added);

            try
            {
                var baseUrl = baseUrlProvider.GetBaseUrl();
                Logger.LogDebug("Enqueuing background processing for report {ReportId}", report.Id);
                // Enqueue background processing (geocoding, alerts) using Hangfire
                backgroundJobClient.Enqueue<IReportProcessingService>(service => service.ProcessReportAsync(report, baseUrl));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error enqueuing report processing job for report {ReportId}", report.Id);
            }

            return Result<Report>.Success(report);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding report at {Lat}, {Lng}", report.Latitude, report.Longitude);
            return Result<Report>.Failure("An error occurred while adding the report.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Report>> GetReportByExternalIdAsync(Guid externalId)
    {
        Logger.LogDebug("Retrieving report by ExternalId {ExternalId}", externalId);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);

        if (report == null)
        {
            Logger.LogWarning("Report with ExternalId {ExternalId} not found", externalId);
            return Result<Report>.Failure("Report not found.");
        }

        return Result<Report>.Success(report);
    }

    /// <inheritdoc />
    public async Task<Result<Report>> GetReportByIdAsync(int id)
    {
        Logger.LogDebug("Retrieving report by ID {Id}", id);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            Logger.LogWarning("Report with ID {Id} not found", id);
            return Result<Report>.Failure("Report not found.");
        }

        return Result<Report>.Success(report);
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetRecentReportsAsync(int? hours = null, bool includeDeleted = false, Guid? includeExternalId = null)
    {
        Logger.LogDebug("Retrieving recent reports (hours: {Hours}, includeDeleted: {IncludeDeleted}, includeExternalId: {IncludeExternalId})", hours, includeDeleted, includeExternalId);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        
        var query = context.Reports.AsNoTracking().IgnoreQueryFilters();

        if (includeExternalId.HasValue)
        {
            // If we have a specific ID, we include it even if it's old or soft-deleted.
            // Other reports must still be recent and not soft-deleted (unless includeDeleted is true).
            query = query.Where(r => r.ExternalId == includeExternalId.Value || 
                                     (r.CreatedAt >= cutoff && (includeDeleted || r.DeletedAt == null)));
        }
        else
        {
            query = query.Where(r => r.CreatedAt >= cutoff);
            if (!includeDeleted)
            {
                query = query.Where(r => r.DeletedAt == null);
            }
        }

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
            
        Logger.LogDebug("Found {Count} recent reports", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetTopRecentReportsAsync(int count)
    {
        Logger.LogDebug("Retrieving top {Count} recent reports", count);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var results = await context.Reports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

        Logger.LogDebug("Retrieved {Count} reports", results.Count);
        return results;
    }
}
