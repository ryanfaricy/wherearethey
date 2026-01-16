using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for background processing of location reports, such as matching alerts and sending notifications.
/// </summary>
public interface IReportProcessingService
{
    /// <summary>
    /// Processes a new location report.
    /// </summary>
    /// <param name="report">The report to process.</param>
    Task ProcessReportAsync(LocationReport report);
}
