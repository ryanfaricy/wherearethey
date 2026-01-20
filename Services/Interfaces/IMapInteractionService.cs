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
    /// <param name="lat">The latitude of the click.</param>
    /// <param name="lng">The longitude of the click.</param>
    /// <param name="isMarkerClick">True if a marker was clicked.</param>
    /// <param name="reportId">The ID of the report if a report marker was clicked.</param>
    /// <param name="alertId">The ID of the alert if an alert marker was clicked.</param>
    /// <param name="alertCreationMode">Whether the map is in alert creation mode.</param>
    /// <returns>True if the interaction was handled (e.g. dialog opened).</returns>
    Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick, int? reportId = null, int? alertId = null, bool alertCreationMode = false);

    /// <summary>
    /// Handles a right-click or long-press on the map to start a report.
    /// </summary>
    /// <param name="lat">The latitude of the context menu.</param>
    /// <param name="lng">The longitude of the context menu.</param>
    /// <param name="userIdentifier">The identifier of the user (optional).</param>
    /// <param name="isAdmin">Whether the user is an administrator.</param>
    /// <param name="alertCreationMode">Whether the map is in alert creation mode.</param>
    Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false, bool alertCreationMode = false);

    /// <summary>
    /// Calculates an appropriate search radius based on zoom level and interaction type.
    /// </summary>
    /// <param name="zoom">The current map zoom level.</param>
    /// <param name="isMarkerClick">True if the interaction was a marker click.</param>
    /// <param name="viewportRadiusKm">The radius of the viewport in kilometers (optional).</param>
    /// <returns>The calculated search radius in kilometers.</returns>
    double CalculateSearchRadius(double zoom, bool isMarkerClick, double? viewportRadiusKm = null);
}
