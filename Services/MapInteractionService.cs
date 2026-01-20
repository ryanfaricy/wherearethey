using Radzen;
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
    ILogger<MapInteractionService> logger)
    : IMapInteractionService
{
    /// <inheritdoc />
    public async Task<bool> HandleMapClickAsync(double lat, double lng, bool isMarkerClick, int? reportId = null, int? alertId = null, bool alertCreationMode = false)
    {
        logger.LogDebug("Handling map click at {Lat}, {Lng} (isMarkerClick: {IsMarkerClick}, reportId: {ReportId}, alertId: {AlertId}, alertCreationMode: {AlertCreationMode})",
            lat, lng, isMarkerClick, reportId, alertId, alertCreationMode);

        if (alertCreationMode)
        {
            return true; // Don't handle clicks in alert creation mode
        }

        var zoom = await mapService.GetZoomLevelAsync();
        var mapState = await mapService.GetMapStateAsync();
        var searchRadiusKm = CalculateSearchRadius(zoom, isMarkerClick, mapState?.RadiusKm);
        
        logger.LogTrace("Calculated search radius: {Radius}km for zoom {Zoom}", searchRadiusKm, zoom);

        var nearbyReports = stateService.FindNearbyReports(lat, lng, searchRadiusKm)
            .OrderBy(r => GeoUtils.CalculateDistance(lat, lng, r.Latitude, r.Longitude))
            .ToList();
        
        // Ensure the explicitly requested report is included and at the top
        if (reportId.HasValue)
        {
            var report = nearbyReports.FirstOrDefault(r => r.Id == reportId.Value) 
                         ?? stateService.Reports.FirstOrDefault(r => r.Id == reportId.Value);
            if (report != null)
            {
                if (nearbyReports.Any(r => r.Id == report.Id))
                {
                    nearbyReports.RemoveAll(r => r.Id == report.Id);
                }
                nearbyReports.Insert(0, report);
            }
        }

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

        // Sort alerts by distance
        nearbyAlerts = nearbyAlerts
            .OrderBy(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude))
            .ToList();

        // Ensure the explicitly requested alert is included and at the top
        if (alertId.HasValue)
        {
            var alert = nearbyAlerts.FirstOrDefault(a => a.Id == alertId.Value)
                        ?? stateService.Alerts.FirstOrDefault(a => a.Id == alertId.Value);
            if (alert != null)
            {
                if (nearbyAlerts.Any(a => a.Id == alert.Id))
                {
                    nearbyAlerts.RemoveAll(a => a.Id == alert.Id);
                }
                nearbyAlerts.Insert(0, alert);
            }
        }

        logger.LogDebug("Found {ReportCount} nearby reports and {AlertCount} nearby alerts", nearbyReports.Count, nearbyAlerts.Count);

        // If we didn't hit anything (no alerts, no reports, and not a specific marker),
        // we fallback to the Create Alert dialog (by returning false)
        if (!nearbyAlerts.Any() && !nearbyReports.Any() && !isMarkerClick && !reportId.HasValue && !alertId.HasValue)
        {
            logger.LogDebug("Nothing found at click location, returning false to trigger alert creation");
            return false;
        }

        // Favor explicit IDs if provided
        var selectedReportId = reportId;
        var selectedAlertId = alertId;

        // If no explicit ID but we have nearby items, pick the closest (which are now at index 0)
        if (!selectedReportId.HasValue && !selectedAlertId.HasValue)
        {
            if (nearbyReports.Any())
            {
                selectedReportId = nearbyReports.First().Id;
            }
            
            if (nearbyAlerts.Any())
            {
                selectedAlertId = nearbyAlerts.First().Id;
            }
        }

        // If it's a marker click and we only have a single report/alert, we might want to highlight it on map
        if (selectedReportId.HasValue && !nearbyAlerts.Any() && nearbyReports.Count <= 1)
        {
            await mapService.SelectReportAsync(selectedReportId.Value);
        }

        var isAdmin = await adminService.IsAdminAsync();

        logger.LogInformation("Opening details dialog for {ReportCount} reports and {AlertCount} alerts", nearbyReports.Count, nearbyAlerts.Count);

        // Open ReportDetailsDialog for everything that hit the map (blobs or alert zones)
        var result = await dialogService.OpenAsync<ReportDetailsDialog>(isAdmin ? "ADMIN AREA DETAILS" : "AREA DETAILS",
            new Dictionary<string, object> 
            { 
                { "Reports", nearbyReports },
                { "Alerts", nearbyAlerts },
                { "SelectedReportId", selectedReportId! },
                { "SelectedAlertId", selectedAlertId! },
            },
            DialogConfigs.Default);

        if (result == true) // If an alert was deleted/updated
        {
            logger.LogInformation("Alert changed in details dialog, reloading alerts");
            await stateService.LoadAlertsAsync();
        }
            
        return true;
    }

    /// <inheritdoc />
    public async Task HandleMapContextMenuAsync(double lat, double lng, string? userIdentifier = null, bool isAdmin = false, bool alertCreationMode = false)
    {
        logger.LogInformation("Handling map context menu at {Lat}, {Lng} (alertCreationMode: {AlertCreationMode})", lat, lng, alertCreationMode);
        if (alertCreationMode)
        {
            return;
        }

        var effectiveIsAdmin = isAdmin || await adminService.IsAdminAsync();
        
        var report = new Report
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
            logger.LogDebug("Opening report dialog for manual report");
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
    public double CalculateSearchRadius(double zoom, bool isMarkerClick, double? viewportRadiusKm = null)
    {
        // - Marker clicks (isMarkerClick): 100m
        // - Otherwise use the viewport radius if available (capped at 160km)
        // - High zoom map clicks: 1kM tap tolerance for near-misses on mobile
        // - Low zoom map clicks: 10km area search for heatmap blobs
        if (isMarkerClick)
        {
            return 0.1;
        }

        if (viewportRadiusKm.HasValue)
        {
            return Math.Min(viewportRadiusKm.Value, 160.0);
        }
        
        if (zoom >= 15)
        {
            return 1;
        }
        
        return 10.0;
    }
}
