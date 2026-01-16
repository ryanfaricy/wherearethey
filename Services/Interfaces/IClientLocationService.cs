using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Manages client-side geolocation acquisition, including fallback and manual picking.
/// </summary>
public interface IClientLocationService
{
    /// <summary>
    /// Indicates if the service is currently trying to acquire a location.
    /// </summary>
    bool IsLocating { get; }

    /// <summary>
    /// Indicates if the manual pick button should be shown.
    /// </summary>
    bool ShowManualPick { get; }

    /// <summary>
    /// The last successfully acquired position.
    /// </summary>
    GeolocationPosition? LastKnownPosition { get; }

    /// <summary>
    /// The timestamp of the last location update.
    /// </summary>
    DateTime LastLocationUpdate { get; }

    /// <summary>
    /// Triggered when location state (IsLocating, ShowManualPick, etc.) changes.
    /// </summary>
    event Action OnStateChanged;

    /// <summary>
    /// Attempts to get the current location with a timeout and optional manual fallback.
    /// </summary>
    Task<GeolocationPosition?> GetLocationWithFallbackAsync(bool allowManual = true, bool showUI = true);

    /// <summary>
    /// Confirms a manual location pick (usually from the map center).
    /// </summary>
    void ConfirmManualPick(GeolocationPosition? position);

    /// <summary>
    /// Updates the last known position.
    /// </summary>
    void UpdateLastKnownPosition(GeolocationPosition position);
}
