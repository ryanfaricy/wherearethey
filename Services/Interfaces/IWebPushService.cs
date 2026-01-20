using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for sending web push notifications.
/// </summary>
public interface IWebPushService
{
    /// <summary>
    /// Sends a push notification to a single subscription.
    /// </summary>
    /// <param name="subscription">The subscription details.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="url">The URL to open when the notification is clicked (optional).</param>
    Task SendNotificationAsync(WebPushSubscription subscription, string title, string message, string? url = null);

    /// <summary>
    /// Sends push notifications to multiple subscriptions.
    /// </summary>
    /// <param name="subscriptions">The collection of subscriptions.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="url">The URL to open when the notification is clicked (optional).</param>
    Task SendNotificationsAsync(IEnumerable<WebPushSubscription> subscriptions, string title, string message, string? url = null);
}
