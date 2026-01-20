using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class MapNavigationManager(
    NavigationManager navigationManager,
    IReportService reportService,
    IAlertService alertService,
    ILogger<MapNavigationManager> logger) : IMapNavigationManager
{
    /// <inheritdoc />
    public async Task<MapNavigationState> GetNavigationStateAsync()
    {
        logger.LogDebug("Parsing navigation state from URI: {Uri}", navigationManager.Uri);
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        int? selectedHours = null;
        int? focusReportId = null;
        double? initialLat = null;
        double? initialLng = null;
        double? initialRadius = null;
        var reportNotFound = false;

        if (query.TryGetValue("hours", out var hoursStr) && int.TryParse(hoursStr, out var h))
        {
            selectedHours = h;
            logger.LogTrace("Navigation state: hours={Hours}", selectedHours);
        }

        if (query.TryGetValue("reportId", out var reportIdStr))
        {
            logger.LogDebug("Navigation state: reportId={ReportIdStr} found in query", reportIdStr);
            if (Guid.TryParse(reportIdStr, out var rGuid))
            {
                var result = await reportService.GetReportByExternalIdAsync(rGuid);
                if (result.IsSuccess)
                {
                    var report = result.Value!;
                    focusReportId = report.Id;
                    initialLat = report.Latitude;
                    initialLng = report.Longitude;
                    logger.LogTrace("Resolved reportId {ReportIdStr} to report ID {ReportId}", reportIdStr, focusReportId);
                }
                else
                {
                    logger.LogWarning("Navigation state: reportId {ReportIdStr} not found", reportIdStr);
                    reportNotFound = true;
                }
            }
            else if (int.TryParse(reportIdStr, out var rId))
            {
                focusReportId = rId;
            }
        }

        if (query.TryGetValue("alertId", out var alertIdStr) && Guid.TryParse(alertIdStr, out var aGuid))
        {
            logger.LogDebug("Navigation state: alertId={AlertIdStr} found in query", alertIdStr);
            var result = await alertService.GetAlertByExternalIdAsync(aGuid);
            if (result.IsSuccess)
            {
                var alert = result.Value!;
                initialLat = alert.Latitude;
                initialLng = alert.Longitude;
                initialRadius = alert.RadiusKm;
                logger.LogTrace("Resolved alertId {AlertIdStr} to alert ID {AlertId}", alertIdStr, alert.Id);
            }
            else
            {
                logger.LogWarning("Navigation state: alertId {AlertIdStr} not found", alertIdStr);
            }
        }

        if (!initialLat.HasValue && query.TryGetValue("lat", out var latStr) && double.TryParse(latStr, CultureInfo.InvariantCulture, out var lat))
        {
            initialLat = lat;
        }
        if (!initialLng.HasValue && query.TryGetValue("lng", out var lngStr) && double.TryParse(lngStr, CultureInfo.InvariantCulture, out var lng))
        {
            initialLng = lng;
        }
        if (!initialRadius.HasValue && query.TryGetValue("radius", out var radiusStr) && double.TryParse(radiusStr, CultureInfo.InvariantCulture, out var radius))
        {
            initialRadius = radius;
        }

        return new MapNavigationState
        {
            SelectedHours = selectedHours,
            FocusReportId = focusReportId,
            InitialLat = initialLat,
            InitialLng = initialLng,
            InitialRadius = initialRadius,
            ReportNotFound = reportNotFound,
        };
    }
}
