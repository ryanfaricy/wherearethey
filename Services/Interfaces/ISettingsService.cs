using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing application-wide settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current system settings.
    /// </summary>
    /// <returns>The system settings.</returns>
    Task<SystemSettings> GetSettingsAsync();

    /// <summary>
    /// Updates the system settings.
    /// </summary>
    /// <param name="settings">The new settings to apply.</param>
    Task UpdateSettingsAsync(SystemSettings settings);
}
