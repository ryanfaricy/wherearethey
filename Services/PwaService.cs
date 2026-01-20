using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class PwaService(IJSRuntime jsRuntime) : IPwaService
{
    public async Task<string> RequestPushPermissionAsync()
    {
        return await jsRuntime.InvokeAsync<string>("pwaFunctions.requestPushPermission");
    }

    public async Task<PushSubscriptionModel?> GetPushSubscriptionAsync()
    {
        return await jsRuntime.InvokeAsync<PushSubscriptionModel?>("pwaFunctions.getPushSubscription");
    }

    public async Task<PushSubscriptionModel?> SubscribeUserAsync(string vapidPublicKey)
    {
        return await jsRuntime.InvokeAsync<PushSubscriptionModel?>("pwaFunctions.subscribeUser", vapidPublicKey);
    }
    
    public async Task<bool> IsPwaAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isPwa");
    }

    public async Task<bool> IsIOSAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isIOS");
    }

    public async Task<bool> IsPushSupportedAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("pwaFunctions.isPushSupported");
    }
}
