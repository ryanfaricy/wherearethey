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
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId);

        return report != null ? Result<Report>.Success(report) : Result<Report>.Failure("Report not found.");
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetRecentReportsAsync(int? hours = null)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();
        
        // Use the global expiry setting if no hours provided
        var actualHours = hours ?? settings.ReportExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-actualHours);
        return await context.Reports
            .AsNoTracking()
            .Where(r => r.DeletedAt == null && r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Report>> GetAllReportsAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteReportAsync(int id)
    {
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
