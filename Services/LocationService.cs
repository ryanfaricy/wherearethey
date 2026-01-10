using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class LocationService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocationService> _logger;

    public event Action? OnReportAdded;

    public LocationService(ApplicationDbContext context, IServiceProvider serviceProvider, ILogger<LocationService> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<LocationReport> AddLocationReportAsync(LocationReport report)
    {
        report.Timestamp = DateTime.UtcNow;
        _context.LocationReports.Add(report);
        await _context.SaveChangesAsync();

        OnReportAdded?.Invoke();

        // Process alerts in the background to not block the reporter
        _ = Task.Run(async () => await ProcessAlertsForReport(report));

        return report;
    }

    private async Task ProcessAlertsForReport(LocationReport report)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var matchingAlerts = await alertService.GetMatchingAlertsAsync(report.Latitude, report.Longitude);
            
            foreach (var alert in matchingAlerts)
            {
                var email = alertService.DecryptEmail(alert.EncryptedEmail);
                if (!string.IsNullOrEmpty(email))
                {
                    var subject = report.IsEmergency ? "EMERGENCY: Report in your area!" : "Alert: New report in your area";
                    var body = $@"
                        <h3>New report near your alert area</h3>
                        <p><strong>Location:</strong> {report.Latitude:F4}, {report.Longitude:F4}</p>
                        <p><strong>Time:</strong> {report.Timestamp:g} UTC</p>
                        {(report.IsEmergency ? "<p style='color: red; font-weight: bold;'>THIS IS MARKED AS AN EMERGENCY</p>" : "")}
                        {(string.IsNullOrEmpty(report.Message) ? "" : $"<p><strong>Message:</strong> {report.Message}</p>")}
                        <hr/>
                        <p><a href='https://wherearethey.com/heatmap'>View on Heat Map</a></p>
                        <small>You received this because you set up an alert on WhereAreThey.</small>";

                    await emailService.SendEmailAsync(email, subject, body);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alerts for report {ReportId}", report.Id);
        }
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
        return reports.Where(r => GeoUtils.CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
            .ToList();
    }
}
