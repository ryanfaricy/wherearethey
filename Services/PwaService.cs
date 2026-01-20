using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class PwaService(IJSRuntime jsRuntime, ILogger<PwaService> logger) : IPwaService
{
    /// <inheritdoc />
    public async Task<string> RequestPushPermissionAsync()
    {
        logger.LogInformation("Requesting push notification permission");
        return await jsRuntime.InvokeAsync<string>("pwaFunctions.requestPushPermission");
    }

    /// <inheritdoc />
    public async Task<PushSubscriptionModel?> GetPushSubscriptionAsync()
    {
        logger.LogDebug("Checking for existing push subscription");
        return await jsRuntime.InvokeAsync<PushSubscriptionModel?>("pwaFunctions.getPushSubscription");
    }

    /// <inheritdoc />
    public async Task<PushSubscriptionModel?> SubscribeUserAsync(string vapidPublicKey)
    {
        logger.LogInformation("Subscribing user to push notifications");
        return await jsRuntime.InvokeAsync<PushSubscriptionModel?>("pwaFunctions.subscribeUser", vapidPublicKey);
    }
    
    /// <inheritdoc />
    public async Task<bool> IsPwaAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isPwa");
    }

    /// <inheritdoc />
    public async Task<bool> IsIOSAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isIOS");
    }

    /// <inheritdoc />
    public async Task<bool> IsPushSupportedAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isPushSupported");
    }
}
