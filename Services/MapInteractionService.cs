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
    public async Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick, int? reportId = null, int? alertId = null, bool alertCreationMode = false)
    {
        if (alertCreationMode)
        {
            return true; // Don't handle clicks in alert creation mode
        }

        var zoom = await mapService.GetZoomLevelAsync();
        var searchRadiusKm = CalculateSearchRadius(zoom, isMarkerClick);

        var nearbyReports = stateService.FindNearbyReports(lat, lng, searchRadiusKm);
        
        // For alerts, we want to know if we are actually clicking "on" or "inside" one
        List<Alert> nearbyAlerts;
        if (isMarkerClick || alertId.HasValue)
        {
            // For marker clicks or specific IDs, use standard search radius
            nearbyAlerts = stateService.FindNearbyAlerts(lat, lng, searchRadiusKm);
        }
        else
        {
            // For general map clicks, we only "hit" an alert if we are INSIDE it 
            // OR within a small tap tolerance (100m) of the center marker
            nearbyAlerts = stateService.Alerts
                .Where(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude) <= Math.Max(a.RadiusKm, 0.1))
                .ToList();
        }

        // If we didn't hit anything (no alerts, no reports, and not a specific marker),
        // we fallback to the Create Alert dialog (by returning false)
        if (!nearbyAlerts.Any() && !nearbyReports.Any() && !isMarkerClick && !reportId.HasValue && !alertId.HasValue)
        {
            return false;
        }

        // Favor explicit IDs if provided
        int? selectedReportId = reportId;
        int? selectedAlertId = alertId;

        // If no explicit ID but we have nearby items, pick the closest
        if (!selectedReportId.HasValue && !selectedAlertId.HasValue)
        {
            if (nearbyReports.Any())
            {
                selectedReportId = nearbyReports
                    .OrderBy(r => GeoUtils.CalculateDistance(lat, lng, r.Latitude, r.Longitude))
                    .First().Id;
            }
            
            if (nearbyAlerts.Any())
            {
                selectedAlertId = nearbyAlerts
                    .OrderBy(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude))
                    .First().Id;
            }
        }

        // If it's a marker click and we only have a single report/alert, we might want to highlight it on map
        if (selectedReportId.HasValue && !nearbyAlerts.Any() && nearbyReports.Count <= 1)
        {
            await mapService.SelectReportAsync(selectedReportId.Value);
        }

        var isAdmin = await adminService.IsAdminAsync();

        // Open ReportDetailsDialog for everything that hit the map (blobs or alert zones)
        var result = await dialogService.OpenAsync<ReportDetailsDialog>(isAdmin ? "ADMIN AREA DETAILS" : "AREA DETAILS",
            new Dictionary<string, object> 
            { 
                { "Reports", nearbyReports },
                { "Alerts", nearbyAlerts },
                { "SelectedReportId", selectedReportId },
                { "SelectedAlertId", selectedAlertId }
            },
            DialogConfigs.Default);

        if (result == true) // If an alert was deleted/updated
        {
            await stateService.LoadAlertsAsync();
        }
            
        return true;
    }

    /// <inheritdoc />
    public async Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false, bool alertCreationMode = false)
    {
        if (alertCreationMode)
        {
            return;
        }

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
        // - High zoom map clicks: 500m tap tolerance for near-misses on mobile
        // - Low zoom map clicks: 10km area search for heatmap blobs
        if (isMarkerClick)
        {
            return 0.05;
        }
        
        if (zoom >= 15)
        {
            return 0.5;
        }
        
        return 10.0;
    }
}
