using Microsoft.JSInterop;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class MapService(IJSRuntime jsRuntime) : IMapService
{
    public async Task InitMapAsync(string elementId, double initialLat, double initialLng, List<Report> reports, object objRef, List<Alert> alerts, object translations, bool isAdmin = false)
    {
        await jsRuntime.InvokeVoidAsync("initHeatMap", elementId, initialLat, initialLng, reports, objRef, alerts, translations, isAdmin);
    }

    public async Task UpdateHeatMapAsync(List<Report> reports, bool shouldFitBounds = true)
    {
        await jsRuntime.InvokeVoidAsync("updateHeatMap", reports, shouldFitBounds);
    }

    public async Task UpdateAlertsAsync(List<Alert> alerts)
    {
        await jsRuntime.InvokeVoidAsync("updateAlerts", alerts);
    }

    public async Task AddSingleReportAsync(Report report)
    {
        await jsRuntime.InvokeVoidAsync("addSingleReport", report);
    }

    public async Task RemoveSingleReportAsync(int reportId)
    {
        await jsRuntime.InvokeVoidAsync("removeSingleReport", reportId);
    }

    public async Task FocusReportAsync(int reportId, bool triggerClick = true)
    {
        await jsRuntime.InvokeVoidAsync("focusReport", reportId, triggerClick);
    }

    public async Task SelectReportAsync(int reportId)
    {
        await jsRuntime.InvokeVoidAsync("selectReport", reportId);
    }

    public async Task<MapState?> GetMapStateAsync()
    {
        return await jsRuntime.InvokeAsync<MapState?>("getMapState");
    }

    public async Task SetMapViewAsync(double lat, double lng, double? radiusKm = null)
    {
        await jsRuntime.InvokeVoidAsync("setMapView", lat, lng, radiusKm);
    }

    public async Task UpdateUserLocationAsync(double lat, double lng, double? accuracy = null)
    {
        await jsRuntime.InvokeVoidAsync("updateUserLocation", lat, lng, accuracy);
    }

    public async Task ShowGhostPinAsync(double lat, double lng)
    {
        await jsRuntime.InvokeVoidAsync("showGhostPin", lat, lng);
    }

    public async Task HideGhostPinAsync()
    {
        await jsRuntime.InvokeVoidAsync("hideGhostPin");
    }

    public async Task UpdateMapThemeAsync(string? theme = null)
    {
        await jsRuntime.InvokeVoidAsync("updateMapTheme", theme);
    }

    public async Task<int> WatchLocationAsync(object objRef)
    {
        return await jsRuntime.InvokeAsync<int>("watchLocation", objRef);
    }

    public async Task StopWatchingAsync(int watchId)
    {
        await jsRuntime.InvokeVoidAsync("stopWatching", watchId);
    }

    public async Task<double> GetZoomLevelAsync()
    {
        return await jsRuntime.InvokeAsync<double>("getZoomLevel");
    }

    public async Task SetAlertCreationModeAsync(bool enabled)
    {
        await jsRuntime.InvokeVoidAsync("setAlertCreationMode", enabled);
    }

    public async Task DestroyMapAsync()
    {
        await jsRuntime.InvokeVoidAsync("destroyHeatMap");
    }
}
