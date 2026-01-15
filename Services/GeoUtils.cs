namespace WhereAreThey.Services;

public static class GeoUtils
{
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

    public static (double minLat, double maxLat, double minLon, double maxLon) GetBoundingBox(double latitude, double longitude, double radiusKm)
    {
        var latDelta = radiusKm / 111.0; // 1 degree latitude ≈ 111 km
        var lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

        return (latitude - latDelta, latitude + latDelta, longitude - lonDelta, longitude + lonDelta);
    }
}
