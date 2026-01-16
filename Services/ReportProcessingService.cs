using System.Globalization;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ReportProcessingService(
    IServiceProvider serviceProvider,
    IOptions<AppOptions> appOptions,
    ISettingsService settingsService,
    ILocationService locationService,
    ILogger<ReportProcessingService> logger) : IReportProcessingService
{
    /// <inheritdoc />
    public async Task ProcessReportAsync(LocationReport report)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var geocodingService = scope.ServiceProvider.GetRequiredService<IGeocodingService>();
            var settings = await settingsService.GetSettingsAsync();

            var matchingAlerts = await alertService.GetMatchingAlertsAsync(report.Latitude, report.Longitude);
            
            var baseUrl = appOptions.Value.BaseUrl;

            // Approximate address
            var address = await geocodingService.ReverseGeocodeAsync(report.Latitude, report.Longitude);

            // Determine local time
            var localTimeStr = locationService.GetFormattedLocalTime(report.Latitude, report.Longitude, report.Timestamp);

            // Map thumbnail
            var mapThumbnailHtml = "";
            var heatMapUrl = $"{baseUrl}/?reportId={report.ExternalId}";

            if (!string.IsNullOrEmpty(settings.MapboxToken))
            {
                var mapUrl = $"{baseUrl}/api/map/proxy?reportId={report.ExternalId}";
                mapThumbnailHtml = $"<p><a href='{heatMapUrl}'><img src='{mapUrl}' alt='Map Location' style='max-width: 100%; height: auto; border-radius: 8px;' /></a></p>";
            }

            foreach (var alert in matchingAlerts)
            {
                var email = alertService.DecryptEmail(alert.EncryptedEmail);
                if (!string.IsNullOrEmpty(email))
                {
                    var subject = report.IsEmergency ? "EMERGENCY: Report in your area!" : "Alert: New report in your area";
                    var body = $@"
                        <h3>New report near your alert area</h3>
                        {(string.IsNullOrEmpty(alert.Message) ? "" : $"<p><strong>Your Alert:</strong> {alert.Message}</p>")}
                        {(!string.IsNullOrEmpty(address) ? $"<p><strong>Approx. Address:</strong> {address}</p>" : "")}
                        <p><strong>Time:</strong> {localTimeStr}</p>
                        {(report.IsEmergency ? "<p style='color: red; font-weight: bold;'>THIS IS MARKED AS AN EMERGENCY</p>" : "")}
                        {(string.IsNullOrEmpty(report.Message) ? "" : $"<p><strong>Message:</strong> {report.Message}</p>")}
                        {mapThumbnailHtml}
                        <p><strong>Location:</strong> {report.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {report.Longitude.ToString("F4", CultureInfo.InvariantCulture)}</p>
                        <hr/>
                        <p><a href='{heatMapUrl}'>View on Heat Map</a></p>
                        <small>You received this because you set up an alert on AreTheyHere.</small>";

                    await emailService.SendEmailAsync(email, subject, body);
                }
                else if (!string.IsNullOrEmpty(alert.EncryptedEmail))
                {
                    logger.LogWarning("Failed to decrypt email for alert {AlertId}. The encryption keys may have changed.", alert.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing alerts for report {ReportId}", report.Id);
        }
    }
}
