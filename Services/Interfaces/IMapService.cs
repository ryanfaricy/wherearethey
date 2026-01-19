using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IMapService
{
    Task InitMapAsync(string elementId, double initialLat, double initialLng, List<Report> reports, object objRef, List<Alert> alerts, object translations, bool isAdmin = false);
    Task UpdateHeatMapAsync(List<Report> reports, bool shouldFitBounds = true);
    Task UpdateAlertsAsync(List<Alert> alerts);
    Task AddSingleReportAsync(Report report);
    Task RemoveSingleReportAsync(int reportId);
    Task FocusReportAsync(int reportId, bool triggerClick = true);
    Task SelectReportAsync(int reportId);
    Task<MapState?> GetMapStateAsync();
    Task SetMapViewAsync(double lat, double lng, double? radiusKm = null);
    Task UpdateUserLocationAsync(double lat, double lng, double? accuracy = null);
    Task ShowGhostPinAsync(double lat, double lng);
    Task HideGhostPinAsync();
    Task UpdateMapThemeAsync(string? theme = null);
    Task<int> WatchLocationAsync(object objRef);
    Task StopWatchingAsync(int watchId);
    Task<double> GetZoomLevelAsync();
    Task SetAlertCreationModeAsync(bool enabled);
    Task DestroyMapAsync();
}
