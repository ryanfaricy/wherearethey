using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPush;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class WebPushService(
    ISettingsService settingsService,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBaseUrlProvider baseUrlProvider,
    ILogger<WebPushService> logger) : IWebPushService
{
    public async Task SendNotificationAsync(WebPushSubscription subscription, string title, string message, string? url = null)
    {
        await SendNotificationsAsync([subscription], title, message, url);
    }

    public async Task SendNotificationsAsync(IEnumerable<WebPushSubscription> subscriptions, string title, string message, string? url = null)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.VapidPublicKey) || string.IsNullOrEmpty(settings.VapidPrivateKey))
        {
            logger.LogWarning("VAPID keys are not configured. Web Push notifications skipped.");
            return;
        }

        var baseUrl = baseUrlProvider.GetBaseUrl();
        var vapidDetails = new VapidDetails(baseUrl, settings.VapidPublicKey, settings.VapidPrivateKey);
        var webPushClient = new WebPushClient();

        var payload = JsonSerializer.Serialize(new
        {
            title,
            message,
            url,
        });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails);
            }
            catch (WebPushException ex)
            {
                if (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogInformation("Push subscription {Id} is no longer valid. Deleting.", sub.Id);
                    await DeleteSubscriptionAsync(sub.Id);
                }
                else
                {
                    logger.LogError(ex, "Error sending push notification to subscription {Id}", sub.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error sending push notification to subscription {Id}", sub.Id);
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
