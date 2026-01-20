using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Manages map navigation state and URL parameters.
/// </summary>
public interface IMapNavigationManager
{
    /// <summary>
    /// Gets the current map navigation state from the URL.
    /// </summary>
    /// <returns>The map navigation state.</returns>
    Task<MapNavigationState> GetNavigationStateAsync();
}
