using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class LocationService
{
    private readonly ApplicationDbContext _context;

    public LocationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LocationReport> AddLocationReportAsync(LocationReport report)
    {
        report.Timestamp = DateTime.UtcNow;
        _context.LocationReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<List<LocationReport>> GetRecentReportsAsync(int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return await _context.LocationReports
            .Where(r => r.Timestamp >= cutoff)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm)
    {
        // Simple bounding box calculation (approximation)
        var latDelta = radiusKm / 111.0; // 1 degree latitude ≈ 111 km
        var lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

        var minLat = latitude - latDelta;
        var maxLat = latitude + latDelta;
        var minLon = longitude - lonDelta;
        var maxLon = longitude + lonDelta;

        var reports = await _context.LocationReports
            .Where(r => r.Latitude >= minLat && r.Latitude <= maxLat &&
                       r.Longitude >= minLon && r.Longitude <= maxLon)
            .ToListAsync();

        // Filter by actual distance using Haversine formula
        return reports.Where(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
            .ToList();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
