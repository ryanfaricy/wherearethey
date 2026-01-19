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
    ILogger<ReportService> logger) : BaseService<Report>(contextFactory, eventService), IReportService
{
    /// <inheritdoc />
    public async Task<Result<Report>> CreateReportAsync(Report report)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(report);
            if (!validationResult.IsValid)
            {
                return Result<Report>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            report.ExternalId = Guid.NewGuid();
            report.CreatedAt = DateTime.UtcNow;
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            // Notify global event bus
            EventService.NotifyEntityChanged(report, EntityChangeType.Added);

            try
            {
                var baseUrl = baseUrlProvider.GetBaseUrl();
                // Enqueue background processing (geocoding, alerts) using Hangfire
                backgroundJobClient.Enqueue<IReportProcessingService>(service => service.ProcessReportAsync(report, baseUrl));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enqueuing report processing job");
            }

            return Result<Report>.Success(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding report at {Lat}, {Lng}", report.Latitude, report.Longitude);
            return Result<Report>.Failure("An error occurred while adding the report.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Report>> GetReportByExternalIdAsync(Guid externalId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);

        return report != null ? Result<Report>.Success(report) : Result<Report>.Failure("Report not found.");
    }

    /// <inheritdoc />
    public async Task<Result<Report>> GetReportByIdAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return report != null ? Result<Report>.Success(report) : Result<Report>.Failure("Report not found.");
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetRecentReportsAsync(int? hours = null, bool includeDeleted = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        
        var query = context.Reports.AsNoTracking();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        
        query = query.Where(r => r.CreatedAt >= cutoff);

        if (!includeDeleted)
        {
            // Redundant due to global filter but keeping for explicit clarity if filter is removed
            query = query.Where(r => r.DeletedAt == null);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetTopRecentReportsAsync(int count)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Reports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetAllReportsAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteReportAsync(int id, bool hardDelete = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            return Result.Failure("Report not found.");
        }

        // If it's already deleted and we are an admin, we hard delete it.
        // OR if hardDelete flag is explicitly set.
        if (hardDelete || report.DeletedAt != null)
        {
            return await HardDeleteAsync(id);
        }
        return await SoftDeleteAsync(id);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateReportAsync(Report report)
    {
        var validationResult = await validator.ValidateAsync(report);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult);
        }

        return await UpdateAsync(report);
    }
}
