using Microsoft.Extensions.Localization;
using Radzen;
using WhereAreThey.Components;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class MapInteractionService(
    IMapService mapService,
    IMapStateService stateService,
    DialogService dialogService,
    IAdminService adminService,
    IStringLocalizer<App> L)
    : IMapInteractionService
{
    /// <inheritdoc />
    public async Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick, int? reportId = null, int? alertId = null)
    {
        var zoom = await mapService.GetZoomLevelAsync();
        var searchRadiusKm = CalculateSearchRadius(zoom, isMarkerClick);

        var nearbyReports = stateService.FindNearbyReports(lat, lng, searchRadiusKm);
        var nearbyAlerts = stateService.FindNearbyAlerts(lat, lng, searchRadiusKm);

        if (!nearbyReports.Any() && !nearbyAlerts.Any() && !reportId.HasValue && !alertId.HasValue)
        {
            return false;
        }

        // If it's an alert marker click (verified by alertId), open AlertsDialog directly
        if (alertId.HasValue)
        {
            await dialogService.OpenAsync<AlertsDialog>(L["ALERTS"],
                new Dictionary<string, object>
                {
                    { "SelectedAlertId", alertId.Value },
                },
                DialogConfigs.Default);
            return true;
        }

        // Favor explicit report marker click
        int? selectedReportId = reportId;
        if (!selectedReportId.HasValue && isMarkerClick && nearbyReports.Any())
        {
            // Pick the closest report if multiple are nearby
            selectedReportId = nearbyReports
                .OrderBy(r => GeoUtils.CalculateDistance(lat, lng, r.Latitude, r.Longitude))
                .First().Id;
        }
        else if (!selectedReportId.HasValue && nearbyReports.Count == 1)
        {
            selectedReportId = nearbyReports[0].Id;
        }

        if (selectedReportId.HasValue && !nearbyAlerts.Any() && nearbyReports.Count <= 1)
        {
            await mapService.SelectReportAsync(selectedReportId.Value);
        }

        int? selectedAlertId = null;
        if (isMarkerClick && nearbyAlerts.Any())
        {
             selectedAlertId = nearbyAlerts
                .OrderBy(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude))
                .First().Id;
        }
        else if (nearbyAlerts.Count == 1)
        {
            selectedAlertId = nearbyAlerts[0].Id;
        }

        var isAdmin = await adminService.IsAdminAsync();

        // Tapping on a blob or marker reveals area details
        var result = await dialogService.OpenAsync<ReportDetailsDialog>(isAdmin ? "ADMIN AREA DETAILS" : "AREA DETAILS",
            new Dictionary<string, object> 
            { 
                { "Reports", nearbyReports },
                { "Alerts", nearbyAlerts },
                { "SelectedReportId", selectedReportId },
                { "SelectedAlertId", selectedAlertId }
            },
            DialogConfigs.Default);

        if (result == true) // If an alert was deleted
        {
            await stateService.LoadAlertsAsync();
        }
            
        return true;

    }

    /// <inheritdoc />
    public async Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false)
    {
        var effectiveIsAdmin = isAdmin || await adminService.IsAdminAsync();
        
        var report = new LocationReport
        {
            Latitude = lat,
            Longitude = lng,
            ReporterIdentifier = effectiveIsAdmin ? (userIdentifier ?? "system") : userIdentifier,
            ReporterLatitude = lat,
            ReporterLongitude = lng,
            IsEmergency = false,
        };

        await mapService.ShowGhostPinAsync(lat, lng);
        try
        {
            await dialogService.OpenAsync<ReportDialog>(effectiveIsAdmin ? "ADMIN REPORT" : "REPORT HERE",
                new Dictionary<string, object> 
                { 
                    { "Report", report },
                    { "UpdateReportLocation", false },
                },
                DialogConfigs.Default);
        }
        finally
        {
            await mapService.HideGhostPinAsync();
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
