using System.Globalization;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Models;
using TimeZoneConverter;
using GeoTimeZone;

namespace WhereAreThey.Services;

public class ReportProcessingService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ISettingsService settingsService,
    ILogger<ReportProcessingService> logger) : IReportProcessingService
{
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
            
            var baseUrl = configuration["BaseUrl"] ?? "https://www.aretheyhere.com";

            // Approximate address
            var address = await geocodingService.ReverseGeocodeAsync(report.Latitude, report.Longitude);

            // Determine local time
            string localTimeStr;
            try
            {
                var tzResult = TimeZoneLookup.GetTimeZone(report.Latitude, report.Longitude);
                var tzInfo = TZConvert.GetTimeZoneInfo(tzResult.Result);
                
                var utcTime = report.Timestamp.Kind == DateTimeKind.Utc 
                    ? report.Timestamp 
                    : DateTime.SpecifyKind(report.Timestamp, DateTimeKind.Utc);
                
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tzInfo);
                localTimeStr = $"{localTime:g} ({tzResult.Result})";
            }
            catch
            {
                localTimeStr = $"{report.Timestamp:g} UTC";
            }

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
                        <p><strong>Location:</strong> {report.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {report.Longitude.ToString("F4", CultureInfo.InvariantCulture)}</p>
                        <p><strong>Time:</strong> {localTimeStr}</p>
                        {(report.IsEmergency ? "<p style='color: red; font-weight: bold;'>THIS IS MARKED AS AN EMERGENCY</p>" : "")}
                        {(string.IsNullOrEmpty(report.Message) ? "" : $"<p><strong>Message:</strong> {report.Message}</p>")}
                        {mapThumbnailHtml}
                        <hr/>
                        <p><a href='{heatMapUrl}'>View on Heat Map</a></p>
                        <small>You received this because you set up an alert on AreTheyHere.</small>";

                    await emailService.SendEmailAsync(email!, subject, body);
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
