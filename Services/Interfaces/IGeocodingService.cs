using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for geocoding and reverse geocoding addresses.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Searches for locations based on a text query.
    /// </summary>
    /// <param name="query">The address or place name to search for.</param>
    /// <returns>A list of matching geocoding results.</returns>
    Task<List<GeocodingResult>> SearchAsync(string query);

    /// <summary>
    /// Gets an approximate address for a given set of coordinates.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <returns>An approximate address string, or null if not found.</returns>
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude);
}
