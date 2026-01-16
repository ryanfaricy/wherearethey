using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class ClientStorageService(IJSRuntime jsRuntime) : IClientStorageService
{
    public async Task<string?> GetUserIdentifierAsync()
    {
        return await jsRuntime.InvokeAsync<string?>("getUserIdentifier");
    }

    public async Task<bool> IsNewUserAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("isNewUser");
    }

    public async Task ClearNewUserFlagAsync()
    {
        await jsRuntime.InvokeVoidAsync("clearNewUserFlag");
    }

    public async Task<string?> GetItemAsync(string key)
    {
        return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    public async Task SetItemAsync(string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task<GeolocationPosition?> GetLocationAsync()
    {
        try
        {
            return await jsRuntime.InvokeAsync<GeolocationPosition>("getLocation");
        }
        catch
        {
            return null;
        }
    }
}
