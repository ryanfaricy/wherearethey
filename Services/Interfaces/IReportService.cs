using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing location reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Creates a new report.
    /// </summary>
    /// <param name="report">The report to create.</param>
    /// <returns>A Result containing the created report or an error message.</returns>
    Task<Result<Report>> CreateReportAsync(Report report);

    /// <summary>
    /// Gets a report by its external identifier.
    /// </summary>
    /// <param name="externalId">The external GUID of the report.</param>
    /// <returns>A Result containing the report if found; otherwise, a failure result.</returns>
    Task<Result<Report>> GetReportByExternalIdAsync(Guid externalId);

    /// <summary>
    /// Gets a report by its internal identifier.
    /// </summary>
    /// <param name="id">The internal ID of the report.</param>
    /// <returns>A Result containing the report if found; otherwise, a failure result.</returns>
    Task<Result<Report>> GetReportByIdAsync(int id);

    /// <summary>
    /// Gets recent reports within an optional time frame.
    /// </summary>
    /// <param name="hours">The number of hours back to look. If null, uses the system default.</param>
    /// <returns>A list of recent reports.</returns>
    Task<List<Report>> GetRecentReportsAsync(int? hours = null);

    /// <summary>
    /// Gets the most recent reports regardless of time frame.
    /// </summary>
    /// <param name="count">The maximum number of reports to retrieve.</param>
    /// <returns>A list of the most recent reports.</returns>
    Task<List<Report>> GetTopRecentReportsAsync(int count);

    /// <summary>
    /// Gets all reports for administrative purposes.
    /// </summary>
    /// <returns>A list of all reports.</returns>
    Task<List<Report>> GetAllReportsAsync();

    /// <summary>
    /// Deletes a report.
    /// </summary>
    /// <param name="id">The internal ID of the report.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteReportAsync(int id);

    /// <summary>
    /// Updates an existing report.
    /// </summary>
    /// <param name="report">The report with updated values.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> UpdateReportAsync(Report report);
}
