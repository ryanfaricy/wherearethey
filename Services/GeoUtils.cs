using WhereAreThey.Models;

namespace WhereAreThey.Services;

/// <summary>
/// Utility class for geographical calculations.
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Formats coordinates as a string.
    /// </summary>
    /// <param name="loc">The locatable object.</param>
    /// <param name="digits">Number of decimal digits.</param>
    /// <returns>A formatted string.</returns>
    public static string ToLocationString(this ILocatable loc, int digits = 2) 
        => $"{loc.Latitude.ToString($"F{digits}")}, {loc.Longitude.ToString($"F{digits}")}";

    /// <summary>
    /// Calculates the distance between two points on Earth using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of the first point.</param>
    /// <param name="lon1">Longitude of the first point.</param>
    /// <param name="lat2">Latitude of the second point.</param>
    /// <param name="lon2">Longitude of the second point.</param>
    /// <returns>The distance in kilometers.</returns>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371; // Earth's radius in kilometers
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    /// <summary>
    /// Calculates a bounding box around a point given a radius.
    /// </summary>
    /// <param name="latitude">The center latitude.</param>
    /// <param name="longitude">The center longitude.</param>
    /// <param name="radiusKm">The radius in kilometers.</param>
    /// <returns>A tuple containing min/max latitude and longitude.</returns>
    public static (double minLat, double maxLat, double minLon, double maxLon) GetBoundingBox(double latitude, double longitude, double radiusKm)
    {
        var latDelta = radiusKm / 111.0; // 1 degree latitude ≈ 111 km
        var lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

        return (latitude - latDelta, latitude + latDelta, longitude - lonDelta, longitude + lonDelta);
    }
}
