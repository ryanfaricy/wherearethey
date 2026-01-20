using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for interacting with the JavaScript-based map component.
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Initializes the map.
    /// </summary>
    /// <param name="elementId">The ID of the HTML element to host the map.</param>
    /// <param name="initialLat">Initial latitude.</param>
    /// <param name="initialLng">Initial longitude.</param>
    /// <param name="reports">Initial list of reports to display.</param>
    /// <param name="objRef">DotNetObjectReference for callbacks.</param>
    /// <param name="alerts">Initial list of alerts to display.</param>
    /// <param name="translations">Translations object for UI text.</param>
    /// <param name="isAdmin">Whether the user is an administrator.</param>
    Task InitMapAsync(string elementId, double initialLat, double initialLng, List<Report> reports, object objRef, List<Alert> alerts, object translations, bool isAdmin = false);

    /// <summary>
    /// Updates the heatmap with new reports.
    /// </summary>
    /// <param name="reports">The list of reports.</param>
    /// <param name="shouldFitBounds">Whether to adjust map bounds to fit all reports.</param>
    Task UpdateHeatMapAsync(List<Report> reports, bool shouldFitBounds = true);

    /// <summary>
    /// Updates the alerts markers on the map.
    /// </summary>
    /// <param name="alerts">The list of alerts.</param>
    Task UpdateAlertsAsync(List<Alert> alerts);

    /// <summary>
    /// Adds a single report marker to the map.
    /// </summary>
    /// <param name="report">The report to add.</param>
    Task AddSingleReportAsync(Report report);

    /// <summary>
    /// Removes a single report marker from the map.
    /// </summary>
    /// <param name="reportId">The ID of the report to remove.</param>
    Task RemoveSingleReportAsync(int reportId);

    /// <summary>
    /// Pans to and optionally highlights a report.
    /// </summary>
    /// <param name="reportId">The ID of the report.</param>
    /// <param name="triggerClick">Whether to simulate a click on the marker.</param>
    Task FocusReportAsync(int reportId, bool triggerClick = true);

    /// <summary>
    /// Selects a report (highlights it).
    /// </summary>
    /// <param name="reportId">The ID of the report.</param>
    Task SelectReportAsync(int reportId);

    /// <summary>
    /// Gets the current state of the map (center, zoom, bounds).
    /// </summary>
    /// <returns>The map state.</returns>
    Task<MapState?> GetMapStateAsync();

    /// <summary>
    /// Sets the map view to a specific location and radius.
    /// </summary>
    /// <param name="lat">Target latitude.</param>
    /// <param name="lng">Target longitude.</param>
    /// <param name="radiusKm">Target radius in kilometers (optional).</param>
    Task SetMapViewAsync(double lat, double lng, double? radiusKm = null);

    /// <summary>
    /// Updates the user's current location marker on the map.
    /// </summary>
    /// <param name="lat">Latitude.</param>
    /// <param name="lng">Longitude.</param>
    /// <param name="accuracy">Accuracy in meters (optional).</param>
    Task UpdateUserLocationAsync(double lat, double lng, double? accuracy = null);

    /// <summary>
    /// Shows a "ghost" pin at a location (used for manual picking).
    /// </summary>
    /// <param name="lat">Latitude.</param>
    /// <param name="lng">Longitude.</param>
    Task ShowGhostPinAsync(double lat, double lng);

    /// <summary>
    /// Hides the ghost pin.
    /// </summary>
    Task HideGhostPinAsync();

    /// <summary>
    /// Updates the map's visual theme.
    /// </summary>
    /// <param name="theme">The theme name (optional).</param>
    Task UpdateMapThemeAsync(string? theme = null);

    /// <summary>
    /// Starts watching the user's location via the browser API.
    /// </summary>
    /// <param name="objRef">DotNetObjectReference for location updates.</param>
    /// <returns>A watch ID.</returns>
    Task<int> WatchLocationAsync(object objRef);

    /// <summary>
    /// Stops watching the user's location.
    /// </summary>
    /// <param name="watchId">The watch ID returned by WatchLocationAsync.</param>
    Task StopWatchingAsync(int watchId);

    /// <summary>
    /// Gets the current zoom level of the map.
    /// </summary>
    /// <returns>The zoom level.</returns>
    Task<double> GetZoomLevelAsync();

    /// <summary>
    /// Enables or disables alert creation mode (shows/hides ghost pin or other UI).
    /// </summary>
    /// <param name="enabled">True to enable; false to disable.</param>
    Task SetAlertCreationModeAsync(bool enabled);

    /// <summary>
    /// Destroys the map instance and cleans up resources.
    /// </summary>
    Task DestroyMapAsync();
}
