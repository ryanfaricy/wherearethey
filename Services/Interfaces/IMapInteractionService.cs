namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Orchestrates interactions with the map, such as clicks, context menus,
/// and showing details dialogs.
/// </summary>
public interface IMapInteractionService
{
    /// <summary>
    /// Handles a click on the map or a marker.
    /// </summary>
    /// <returns>True if the interaction was handled (e.g. dialog opened).</returns>
    Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick, int? reportId = null, int? alertId = null);

    /// <summary>
    /// Handles a right-click or long-press on the map to start a report.
    /// </summary>
    Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false);

    /// <summary>
    /// Calculates an appropriate search radius based on zoom level and interaction type.
    /// </summary>
    double CalculateSearchRadius(double zoom, bool isMarkerClick);
}
