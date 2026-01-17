using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing location reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Adds a new location report.
    /// </summary>
    /// <param name="report">The report to add.</param>
    /// <returns>A Result containing the added report or an error message.</returns>
    Task<Result<LocationReport>> AddReportAsync(LocationReport report);

    /// <summary>
    /// Gets a report by its external identifier.
    /// </summary>
    /// <param name="externalId">The external GUID of the report.</param>
    /// <returns>A Result containing the report if found; otherwise, a failure result.</returns>
    Task<Result<LocationReport>> GetReportByExternalIdAsync(Guid externalId);

    /// <summary>
    /// Gets recent reports within an optional time frame.
    /// </summary>
    /// <param name="hours">The number of hours back to look. If null, uses the system default.</param>
    /// <returns>A list of recent reports.</returns>
    Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null);

    /// <summary>
    /// Gets all reports for administrative purposes.
    /// </summary>
    /// <returns>A list of all reports.</returns>
    Task<List<LocationReport>> GetAllReportsAsync();

    /// <summary>
    /// Deletes a location report.
    /// </summary>
    /// <param name="id">The internal ID of the report.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteReportAsync(int id);

    /// <summary>
    /// Updates an existing location report.
    /// </summary>
    /// <param name="report">The report with updated values.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> UpdateReportAsync(LocationReport report);
}
