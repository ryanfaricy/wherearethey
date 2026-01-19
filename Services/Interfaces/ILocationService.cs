using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for location-based calculations and time zone conversions.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Gets all reports within a specified radius of a location.
    /// </summary>
    /// <param name="latitude">The center latitude.</param>
    /// <param name="longitude">The center longitude.</param>
    /// <param name="radiusKm">The search radius in kilometers.</param>
    /// <returns>A list of reports within the radius.</returns>
    Task<List<Report>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm);

    /// <summary>
    /// Formats a UTC timestamp into a local time string based on coordinates.
    /// </summary>
    /// <param name="latitude">The latitude for time zone lookup.</param>
    /// <param name="longitude">The longitude for time zone lookup.</param>
    /// <param name="utcTimestamp">The UTC timestamp to convert.</param>
    /// <returns>A formatted local time string.</returns>
    string GetFormattedLocalTime(double latitude, double longitude, DateTime utcTimestamp);
}
