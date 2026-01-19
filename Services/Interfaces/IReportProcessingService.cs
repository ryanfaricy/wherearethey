using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for background processing of location reports, such as matching alerts and sending notifications.
/// </summary>
public interface IReportProcessingService
{
    /// <summary>
    /// Processes a report in the background.
    /// </summary>
    /// <param name="report">The report to process.</param>
    Task ProcessReportAsync(Report report);
}
