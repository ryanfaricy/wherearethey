using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IWebPushService
{
    Task SendNotificationAsync(WebPushSubscription subscription, string title, string message, string? url = null);
    Task SendNotificationsAsync(IEnumerable<WebPushSubscription> subscriptions, string title, string message, string? url = null);
}
