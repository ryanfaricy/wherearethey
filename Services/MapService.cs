using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class MapService(IJSRuntime jsRuntime, ILogger<MapService> logger) : IMapService
{
    /// <inheritdoc />
    public async Task InitMapAsync(string elementId, double initialLat, double initialLng, List<Report> reports, object objRef, List<Alert> alerts, object translations, bool isAdmin = false)
    {
        logger.LogInformation("Initializing map in element {ElementId} at {Lat}, {Lng}", elementId, initialLat, initialLng);
        await jsRuntime.InvokeVoidAsync("initHeatMap", elementId, initialLat, initialLng, reports, objRef, alerts, translations, isAdmin);
    }

    /// <inheritdoc />
    public async Task UpdateHeatMapAsync(List<Report> reports, bool shouldFitBounds = true)
    {
        logger.LogDebug("Updating heatmap with {Count} reports (shouldFitBounds: {ShouldFitBounds})", reports.Count, shouldFitBounds);
        await jsRuntime.InvokeVoidAsync("updateHeatMap", reports, shouldFitBounds);
    }

    /// <inheritdoc />
    public async Task UpdateAlertsAsync(List<Alert> alerts)
    {
        logger.LogDebug("Updating {Count} alerts on map", alerts.Count);
        await jsRuntime.InvokeVoidAsync("updateAlerts", alerts);
    }

    /// <inheritdoc />
    public async Task AddSingleReportAsync(Report report)
    {
        logger.LogDebug("Adding single report {ReportId} to map", report.Id);
        await jsRuntime.InvokeVoidAsync("addSingleReport", report);
    }

    /// <inheritdoc />
    public async Task RemoveSingleReportAsync(int reportId)
    {
        logger.LogDebug("Removing single report {ReportId} from map", reportId);
        await jsRuntime.InvokeVoidAsync("removeSingleReport", reportId);
    }

    /// <inheritdoc />
    public async Task FocusReportAsync(int reportId, bool triggerClick = true)
    {
        logger.LogDebug("Focusing report {ReportId} on map (triggerClick: {TriggerClick})", reportId, triggerClick);
        await jsRuntime.InvokeVoidAsync("focusReport", reportId, triggerClick);
    }

    /// <inheritdoc />
    public async Task SelectReportAsync(int reportId)
    {
        logger.LogDebug("Selecting report {ReportId} on map", reportId);
        await jsRuntime.InvokeVoidAsync("selectReport", reportId);
    }

    /// <inheritdoc />
    public async Task<MapState?> GetMapStateAsync()
    {
        return await jsRuntime.InvokeAsync<MapState?>("getMapState");
    }

    /// <inheritdoc />
    public async Task SetMapViewAsync(double lat, double lng, double? radiusKm = null)
    {
        logger.LogDebug("Setting map view to {Lat}, {Lng} (radius: {Radius}km)", lat, lng, radiusKm);
        await jsRuntime.InvokeVoidAsync("setMapView", lat, lng, radiusKm);
    }

    /// <inheritdoc />
    public async Task UpdateUserLocationAsync(double lat, double lng, double? accuracy = null)
    {
        await jsRuntime.InvokeVoidAsync("updateUserLocation", lat, lng, accuracy);
    }

    /// <inheritdoc />
    public async Task ShowGhostPinAsync(double lat, double lng)
    {
        logger.LogTrace("Showing ghost pin at {Lat}, {Lng}", lat, lng);
        await jsRuntime.InvokeVoidAsync("showGhostPin", lat, lng);
    }

    /// <inheritdoc />
    public async Task HideGhostPinAsync()
    {
        logger.LogTrace("Hiding ghost pin");
        await jsRuntime.InvokeVoidAsync("hideGhostPin");
    }

    /// <inheritdoc />
    public async Task UpdateMapThemeAsync(string? theme = null)
    {
        logger.LogInformation("Updating map theme to {Theme}", theme ?? "default");
        await jsRuntime.InvokeVoidAsync("updateMapTheme", theme);
    }

    /// <inheritdoc />
    public async Task<int> WatchLocationAsync(object objRef)
    {
        logger.LogInformation("Starting browser location watch");
        return await jsRuntime.InvokeAsync<int>("watchLocation", objRef);
    }

    /// <inheritdoc />
    public async Task StopWatchingAsync(int watchId)
    {
        logger.LogInformation("Stopping browser location watch {WatchId}", watchId);
        await jsRuntime.InvokeVoidAsync("stopWatching", watchId);
    }

    /// <inheritdoc />
    public async Task<double> GetZoomLevelAsync()
    {
        return await jsRuntime.InvokeAsync<double>("getZoomLevel");
    }

    /// <inheritdoc />
    public async Task SetAlertCreationModeAsync(bool enabled)
    {
        logger.LogDebug("Setting alert creation mode to {Enabled}", enabled);
        await jsRuntime.InvokeVoidAsync("setAlertCreationMode", enabled);
    }

    /// <inheritdoc />
    public async Task DestroyMapAsync()
    {
        logger.LogInformation("Destroying map instance");
        await jsRuntime.InvokeVoidAsync("destroyHeatMap");
    }
}
