using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for PWA-specific functionality, like push notifications and device detection.
/// </summary>
public interface IPwaService
{
    /// <summary>
    /// Requests push notification permission from the user.
    /// </summary>
    /// <returns>The permission status ("granted", "denied", "default").</returns>
    Task<string> RequestPushPermissionAsync();

    /// <summary>
    /// Gets the current push subscription if it exists.
    /// </summary>
    /// <returns>The push subscription model, or null if not subscribed.</returns>
    Task<PushSubscriptionModel?> GetPushSubscriptionAsync();

    /// <summary>
    /// Subscribes the user to push notifications.
    /// </summary>
    /// <param name="vapidPublicKey">The server's VAPID public key.</param>
    /// <returns>The new push subscription model, or null if subscription failed.</returns>
    Task<PushSubscriptionModel?> SubscribeUserAsync(string vapidPublicKey);

    /// <summary>
    /// Checks if the application is currently running as a PWA.
    /// </summary>
    /// <returns>True if running as PWA; otherwise, false.</returns>
    Task<bool> IsPwaAsync();

    /// <summary>
    /// Checks if the application is running on an iOS device.
    /// </summary>
    /// <returns>True if on iOS; otherwise, false.</returns>
    Task<bool> IsIOSAsync();

    /// <summary>
    /// Checks if push notifications are supported by the browser/platform.
    /// </summary>
    /// <returns>True if supported; otherwise, false.</returns>
    Task<bool> IsPushSupportedAsync();
}
