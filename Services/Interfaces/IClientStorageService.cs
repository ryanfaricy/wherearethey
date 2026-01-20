using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for interacting with client-side storage (e.g., localStorage).
/// </summary>
public interface IClientStorageService
{
    /// <summary>
    /// Gets the unique user identifier from storage, or creates one if it doesn't exist.
    /// </summary>
    /// <returns>The user identifier.</returns>
    Task<string?> GetUserIdentifierAsync();

    /// <summary>
    /// Checks if the user is a first-time user.
    /// </summary>
    /// <returns>True if it's a new user; otherwise, false.</returns>
    Task<bool> IsNewUserAsync();

    /// <summary>
    /// Clears the flag indicating that the user is new.
    /// </summary>
    Task ClearNewUserFlagAsync();

    /// <summary>
    /// Gets an item from storage by its key.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <returns>The item value, or null if not found.</returns>
    Task<string?> GetItemAsync(string key);

    /// <summary>
    /// Sets an item in storage.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="value">The value to store.</param>
    Task SetItemAsync(string key, string value);

    /// <summary>
    /// Removes an item from storage.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    Task RemoveItemAsync(string key);

    /// <summary>
    /// Gets the last stored location from storage.
    /// </summary>
    /// <returns>The stored location, or null if not found.</returns>
    Task<GeolocationPosition?> GetLocationAsync();
}
