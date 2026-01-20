using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ClientStorageService(IJSRuntime jsRuntime, ILogger<ClientStorageService> logger) : IClientStorageService
{
    /// <inheritdoc />
    public async Task<string?> GetUserIdentifierAsync()
    {
        logger.LogDebug("Retrieving user identifier from storage");
        return await jsRuntime.InvokeAsync<string?>("getUserIdentifier");
    }

    /// <inheritdoc />
    public async Task<bool> IsNewUserAsync()
    {
        logger.LogDebug("Checking if user is new");
        return await jsRuntime.InvokeAsync<bool>("isNewUser");
    }

    /// <inheritdoc />
    public async Task ClearNewUserFlagAsync()
    {
        logger.LogInformation("Clearing new user flag");
        await jsRuntime.InvokeVoidAsync("clearNewUserFlag");
    }

    /// <inheritdoc />
    public async Task<string?> GetItemAsync(string key)
    {
        logger.LogDebug("Getting item {Key} from localStorage", key);
        return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    /// <inheritdoc />
    public async Task SetItemAsync(string key, string value)
    {
        logger.LogDebug("Setting item {Key} in localStorage", key);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(string key)
    {
        logger.LogDebug("Removing item {Key} from localStorage", key);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    /// <inheritdoc />
    public async Task<GeolocationPosition?> GetLocationAsync()
    {
        logger.LogDebug("Requesting current location from browser");
        try
        {
            var position = await jsRuntime.InvokeAsync<GeolocationPosition>("getLocation");
            if (position != null)
            {
                logger.LogDebug("Location successfully retrieved: {Lat}, {Lng}", position.Coords.Latitude, position.Coords.Longitude);
            }
            return position;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve location from browser");
            return null;
        }
    }
}
