using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IPwaService
{
    Task<string> RequestPushPermissionAsync();
    Task<PushSubscriptionModel?> GetPushSubscriptionAsync();
    Task<PushSubscriptionModel?> SubscribeUserAsync(string vapidPublicKey);
    Task<bool> IsPwaAsync();
    Task<bool> IsIOSAsync();
    Task<bool> IsPushSupportedAsync();
}
