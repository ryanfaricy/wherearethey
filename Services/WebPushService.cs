using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPush;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class WebPushService(
    ISettingsService settingsService,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBaseUrlProvider baseUrlProvider,
    IOptions<EmailOptions> emailOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<WebPushService> logger) : IWebPushService
{
    /// <inheritdoc />
    public async Task SendNotificationAsync(WebPushSubscription subscription, string title, string message, string? url = null)
    {
        await SendNotificationsAsync([subscription], title, message, url);
    }

    /// <inheritdoc />
    public async Task SendNotificationsAsync(IEnumerable<WebPushSubscription> subscriptions, string title, string message, string? url = null)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.VapidPublicKey) || string.IsNullOrEmpty(settings.VapidPrivateKey))
        {
            logger.LogWarning("VAPID keys are not configured. Web Push notifications skipped.");
            return;
        }

        var contactEmail = emailOptions.Value.FromEmail;
        var subject = !string.IsNullOrEmpty(contactEmail) ? $"mailto:{contactEmail}" : baseUrlProvider.GetBaseUrl();
        var vapidDetails = new VapidDetails(subject, settings.VapidPublicKey, settings.VapidPrivateKey);
        
        using var httpClient = httpClientFactory.CreateClient();
        var webPushClient = new WebPushClient(httpClient);

        var payload = JsonSerializer.Serialize(new
        {
            title,
            message,
            url,
        });

        var subscriptionList = subscriptions.ToList();
        logger.LogInformation("Sending web push notifications to {Count} subscriptions. Title: {Title} (Subject: {Subject})", subscriptionList.Count, title, subject);

        foreach (var sub in subscriptionList)
        {
            try
            {
                logger.LogTrace("Dispatching push to endpoint {Endpoint}", sub.Endpoint);
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails);
            }
            catch (WebPushException ex)
            {
                if (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogInformation("Push subscription {Id} is no longer valid (Status: {Status}). Deleting.", sub.Id, ex.StatusCode);
                    await DeleteSubscriptionAsync(sub.Id);
                }
                else
                {
                    logger.LogError(ex, "Error sending push notification to subscription {Id}. Status: {Status} (Subject: {Subject})", sub.Id, ex.StatusCode, subject);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error sending push notification to subscription {Id} (Subject: {Subject})", sub.Id, subject);
            }
        }
    }

    private async Task DeleteSubscriptionAsync(int id)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var sub = await context.WebPushSubscriptions.FindAsync(id);
            if (sub != null)
            {
                logger.LogDebug("Removing invalid push subscription {Id} from database", id);
                context.WebPushSubscriptions.Remove(sub);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting invalid push subscription {Id}", id);
        }
    }
}
