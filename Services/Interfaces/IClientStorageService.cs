using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IClientStorageService
{
    Task<string?> GetUserIdentifierAsync();
    Task<bool> IsNewUserAsync();
    Task ClearNewUserFlagAsync();
    Task<string?> GetItemAsync(string key);
    Task SetItemAsync(string key, string value);
    Task RemoveItemAsync(string key);
    Task<GeolocationPosition?> GetLocationAsync();
}
