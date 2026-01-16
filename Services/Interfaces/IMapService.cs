using Microsoft.JSInterop;
using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IMapService
{
    Task InitMapAsync(string elementId, double initialLat, double initialLng, List<LocationReport> reports, object objRef, List<Alert> alerts, object translations);
    Task UpdateHeatMapAsync(List<LocationReport> reports, bool shouldFitBounds = true);
    Task UpdateAlertsAsync(List<Alert> alerts);
    Task AddSingleReportAsync(LocationReport report);
    Task RemoveSingleReportAsync(int reportId);
    Task FocusReportAsync(int reportId);
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
}
