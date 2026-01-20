using System.Globalization;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ReportProcessingService(
    IAlertService alertService,
    IEmailService emailService,
    IWebPushService webPushService,
    IGeocodingService geocodingService,
    IOptions<AppOptions> appOptions,
    ISettingsService settingsService,
    ILocationService locationService,
    IEmailTemplateService emailTemplateService,
    ILogger<ReportProcessingService> logger) : IReportProcessingService
{
    /// <inheritdoc />
    public async Task ProcessReportAsync(Report report, string? baseUrl = null)
    {
        try
        {
            var settings = await settingsService.GetSettingsAsync();

            var matchingAlerts = await alertService.GetMatchingAlertsAsync(report.Latitude, report.Longitude);
            
            var actualBaseUrl = (baseUrl ?? appOptions.Value.BaseUrl).TrimEnd('/');

            // Approximate address
            var address = await geocodingService.ReverseGeocodeAsync(report.Latitude, report.Longitude);

            // Determine local time
            var localTimeStr = locationService.GetFormattedLocalTime(report.Latitude, report.Longitude, report.CreatedAt);

            // Map thumbnail
            var mapThumbnailHtml = "";
            var heatMapUrl = $"{actualBaseUrl}/?reportId={report.ExternalId}";

            if (!string.IsNullOrEmpty(settings.MapboxToken))
            {
                var mapUrl = $"{actualBaseUrl}/api/map/proxy?reportId={report.ExternalId}";
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

            // Web Push Notifications
            var pushRecipients = matchingAlerts
                .Where(a => a.UsePush && !string.IsNullOrEmpty(a.UserIdentifier))
                .Select(a => a.UserIdentifier!)
                .Distinct()
                .ToList();

            if (pushRecipients.Count > 0)
            {
                var pushTitle = report.IsEmergency ? "🚨 EMERGENCY REPORT" : "New Report in Your Area";
                var pushMessage = string.IsNullOrEmpty(report.Message) 
                    ? $"New report at {address ?? report.LocationDisplay()}" 
                    : report.Message;
                
                foreach (var userId in pushRecipients)
                {
                    var subscriptions = await alertService.GetPushSubscriptionsAsync(userId);
                    if (subscriptions.Count > 0)
                    {
                        await webPushService.SendNotificationsAsync(subscriptions, pushTitle, pushMessage, heatMapUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing alerts for report {ReportId}", report.Id);
            throw;
        }
    }
}
