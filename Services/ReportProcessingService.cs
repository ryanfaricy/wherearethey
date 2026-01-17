using System.Globalization;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ReportProcessingService(
    IAlertService alertService,
    IEmailService emailService,
    IGeocodingService geocodingService,
    IOptions<AppOptions> appOptions,
    ISettingsService settingsService,
    ILocationService locationService,
    IEmailTemplateService emailTemplateService,
    ILogger<ReportProcessingService> logger) : IReportProcessingService
{
    /// <inheritdoc />
    public async Task ProcessReportAsync(LocationReport report)
    {
        try
        {
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

            var emails = new List<Email>();
            foreach (var alert in matchingAlerts)
            {
                var emailAddress = alertService.DecryptEmail(alert.EncryptedEmail);
                if (!string.IsNullOrEmpty(emailAddress))
                {
                    var subject = report.IsEmergency ? "EMERGENCY: Report in your area!" : "Alert: New report in your area";

                    var viewModel = new AlertEmailViewModel
                    {
                        AlertMessage = alert.Message,
                        Address = address,
                        LocalTimeStr = localTimeStr,
                        IsEmergency = report.IsEmergency,
                        ReportMessage = report.Message,
                        MapThumbnailHtml = mapThumbnailHtml,
                        Latitude = report.Latitude.ToString("F4", CultureInfo.InvariantCulture),
                        Longitude = report.Longitude.ToString("F4", CultureInfo.InvariantCulture),
                        HeatMapUrl = heatMapUrl,
                    };

                    var body = await emailTemplateService.RenderTemplateAsync("AlertEmail", viewModel);

                    emails.Add(new Email { To = emailAddress, Subject = subject, Body = body });
                }
                else if (!string.IsNullOrEmpty(alert.EncryptedEmail))
                {
                    logger.LogWarning("Failed to decrypt email for alert {AlertId}. The encryption keys may have changed.", alert.Id);
                }
            }

            if (emails.Count > 0)
            {
                await emailService.SendEmailsAsync(emails);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing alerts for report {ReportId}", report.Id);
            throw;
        }
    }
}
