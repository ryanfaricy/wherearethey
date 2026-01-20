namespace WhereAreThey.Services.Interfaces;

public interface IPwaService
{
    Task<string> RequestPushPermissionAsync();
    Task<object?> GetPushSubscriptionAsync();
    Task<object?> SubscribeUserAsync(string vapidPublicKey);
}
