using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Components.Pages;
using Radzen;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class MapInteractionService : IMapInteractionService
{
    private readonly IMapService _mapService;
    private readonly IMapStateService _stateService;
    private readonly DialogService _dialogService;
    private readonly IAdminService _adminService;

    public MapInteractionService(
        IMapService mapService,
        IMapStateService stateService,
        DialogService dialogService,
        IAdminService adminService)
    {
        _mapService = mapService;
        _stateService = stateService;
        _dialogService = dialogService;
        _adminService = adminService;
    }

    /// <inheritdoc />
    public async Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick)
    {
        var zoom = await _mapService.GetZoomLevelAsync();
        var searchRadiusKm = CalculateSearchRadius(zoom, isMarkerClick);

        var nearbyReports = _stateService.FindNearbyReports(lat, lng, searchRadiusKm);
        var nearbyAlerts = _stateService.FindNearbyAlerts(lat, lng, searchRadiusKm);

        if (nearbyReports.Any() || nearbyAlerts.Any())
        {
            // If it's a single report, highlight it immediately for better feedback
            if (nearbyReports.Count == 1 && !nearbyAlerts.Any())
            {
                await _mapService.SelectReportAsync(nearbyReports[0].Id);
            }

            var isAdmin = await _adminService.IsAdminAsync();

            // Tapping on a blob or marker reveals area details
            var result = await _dialogService.OpenAsync<ReportDetailsDialog>(isAdmin ? "ADMIN AREA DETAILS" : "AREA DETAILS",
                new Dictionary<string, object> 
                { 
                    { "Reports", nearbyReports },
                    { "Alerts", nearbyAlerts }
                },
                DialogConfigs.Default);

            if (result == true) // If an alert was deleted
            {
                await _stateService.LoadAlertsAsync();
            }
            
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false)
    {
        var effectiveIsAdmin = isAdmin || await _adminService.IsAdminAsync();
        
        var report = new LocationReport
        {
            Latitude = lat,
            Longitude = lng,
            ReporterIdentifier = effectiveIsAdmin ? (userIdentifier ?? "system") : userIdentifier,
            ReporterLatitude = lat,
            ReporterLongitude = lng,
            IsEmergency = false
        };

        await _mapService.ShowGhostPinAsync(lat, lng);
        try
        {
            await _dialogService.OpenAsync<ReportDialog>(effectiveIsAdmin ? "ADMIN REPORT" : "REPORT HERE",
                new Dictionary<string, object> 
                { 
                    { "Report", report },
                    { "UpdateReportLocation", false }
                },
                DialogConfigs.Default);
        }
        finally
        {
            await _mapService.HideGhostPinAsync();
        }
    }

    /// <inheritdoc />
    public double CalculateSearchRadius(double zoom, bool isMarkerClick)
    {
        // - Marker clicks (isMarkerClick): 50m
        // - High zoom map clicks: 200m tap tolerance for near-misses on mobile
        // - Low zoom map clicks: 5km area search for heatmap blobs
        if (isMarkerClick)
        {
            return 0.05;
        }
        
        if (zoom >= 15)
        {
            return 0.2;
        }
        
        return 5.0;
    }
}
