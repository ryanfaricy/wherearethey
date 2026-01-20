using Microsoft.JSInterop;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class PwaService(IJSRuntime jsRuntime) : IPwaService
{
    public async Task<string> RequestPushPermissionAsync()
    {
        return await jsRuntime.InvokeAsync<string>("pwaFunctions.requestPushPermission");
    }

    public async Task<object?> GetPushSubscriptionAsync()
    {
        return await jsRuntime.InvokeAsync<object?>("pwaFunctions.getPushSubscription");
    }

    public async Task<object?> SubscribeUserAsync(string vapidPublicKey)
    {
        return await jsRuntime.InvokeAsync<object?>("pwaFunctions.subscribeUser", vapidPublicKey);
    }
}
