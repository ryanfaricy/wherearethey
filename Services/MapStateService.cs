using WhereAreThey.Helpers;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <summary>
/// Service responsible for maintaining the state of the map, including reports and alerts.
/// Handles real-time updates and synchronization with the underlying map implementation.
/// </summary>
public class MapStateService : IMapStateService
{
    private readonly IReportService _reportService;
    private readonly IAlertService _alertService;
    private readonly IEventService _eventService;
    private readonly IMapService _mapService;
    private readonly Timer? _pruneTimer;
    private string? _userIdentifier;
    private bool _isAdmin;
    private int? _lastLoadedHours;

    /// <inheritdoc />
    public List<Report> Reports { get; private set; } = [];

    /// <inheritdoc />
    public List<Alert> Alerts { get; private set; } = [];

    private bool _mapInitialized;
    /// <inheritdoc />
    public bool MapInitialized 
    { 
        get => _mapInitialized; 
        set
        {
            if (_mapInitialized == value)
            {
                return;
            }

            _mapInitialized = value;
            if (!_mapInitialized)
            {
                return;
            }

            // When the map initializes, ensure it has the latest data
            _ = _mapService.UpdateHeatMapAsync(Reports);
            if (Alerts?.Any() == true)
            {
                _ = _mapService.UpdateAlertsAsync(Alerts);
            }
        }
    }

    /// <inheritdoc />
    public event Action? OnStateChanged;

    public MapStateService(
        IReportService reportService,
        IAlertService alertService,
        IEventService eventService,
        IMapService mapService)
    {
        _reportService = reportService;
        _alertService = alertService;
        _eventService = eventService;
        _mapService = mapService;

        _eventService.OnEntityChanged += HandleEntityChanged;

        _pruneTimer = new Timer(_ => PruneOldReports(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void PruneOldReports()
    {
        if (_isAdmin || !_lastLoadedHours.HasValue)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddHours(-_lastLoadedHours.Value);
        var toRemove = Reports.Where(r => r.CreatedAt < cutoff).ToList();
        
        if (toRemove.Count == 0)
        {
            return;
        }

        foreach (var report in toRemove)
        {
            Reports.Remove(report);
            if (MapInitialized)
            {
                _ = _mapService.RemoveSingleReportAsync(report.Id);
            }
        }
        
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(string? userIdentifier, bool isAdmin = false)
    {
        _userIdentifier = userIdentifier;
        _isAdmin = isAdmin;
        await LoadAlertsAsync();
    }

    /// <inheritdoc />
    public async Task LoadReportsAsync(int? hours = null)
    {
        _lastLoadedHours = hours;
        var allReports = _isAdmin 
            ? await _reportService.GetAllReportsAsync() 
            : await _reportService.GetRecentReportsAsync(hours);
        
        // Even for admins, we only want to show non-deleted reports in the "real-time" state
        Reports = allReports.Where(r => r.DeletedAt == null).ToList();
        
        if (MapInitialized)
        {
            await _mapService.UpdateHeatMapAsync(Reports);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task LoadAlertsAsync()
    {
        if (!string.IsNullOrEmpty(_userIdentifier))
        {
            Alerts = await _alertService.GetActiveAlertsAsync(_userIdentifier, false);
        }
        else
        {
            Alerts = [];
        }

        if (MapInitialized)
        {
            await _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public List<Report> FindNearbyReports(double lat, double lng, double radiusKm)
    {
        return Reports
            .Where(r => GeoUtils.CalculateDistance(lat, lng, r.Latitude, r.Longitude) <= radiusKm)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    /// <inheritdoc />
    public List<Alert> FindNearbyAlerts(double lat, double lng, double radiusKm)
    {
        return Alerts
            .Where(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude) <= radiusKm)
            .ToList();
    }

    private void HandleEntityChanged(object entity, EntityChangeType type)
    {
        if (entity is Report report)
        {
            switch (type)
            {
                case EntityChangeType.Added:
                    HandleReportAdded(report);
                    break;
                case EntityChangeType.Updated:
                    HandleReportUpdated(report);
                    break;
                case EntityChangeType.Deleted:
                    HandleReportDeleted(report.Id);
                    break;
            }
        }
        else if (entity is Alert alert)
        {
            switch (type)
            {
                case EntityChangeType.Added:
                    HandleAlertAdded(alert);
                    break;
                case EntityChangeType.Updated:
                    HandleAlertUpdated(alert);
                    break;
                case EntityChangeType.Deleted:
                    HandleAlertDeleted(alert.Id);
                    break;
            }
        }
    }

    private void HandleReportAdded(Report report)
    {
        if (!VisibilityPolicy.ShouldShow(report, _isAdmin))
        {
            return;
        }

        Reports.Insert(0, report);
        if (MapInitialized && report.DeletedAt == null)
        {
            _ = _mapService.AddSingleReportAsync(report);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleReportUpdated(Report report)
    {
        var index = Reports.FindIndex(r => r.Id == report.Id);
        
        if (!VisibilityPolicy.ShouldShow(report, _isAdmin))
        {
            if (index == -1)
            {
                return;
            }

            Reports.RemoveAt(index);
            if (MapInitialized)
            {
                _ = _mapService.RemoveSingleReportAsync(report.Id);
            }
            OnStateChanged?.Invoke();
            return;
        }

        if (index == -1)
        {
            // Might have been previously deleted or new
            HandleReportAdded(report);
            return;
        }

        Reports[index] = report;
        if (MapInitialized)
        {
            _ = UpdateReportOnMap(report);
        }
        OnStateChanged?.Invoke();
    }

    private async Task UpdateReportOnMap(Report report)
    {
        if (!VisibilityPolicy.ShouldShow(report, _isAdmin))
        {
            await _mapService.RemoveSingleReportAsync(report.Id);
        }
        else
        {
            await _mapService.RemoveSingleReportAsync(report.Id);
            await _mapService.AddSingleReportAsync(report);
        }
    }

    private void HandleReportDeleted(int id)
    {
        if (_isAdmin)
        {
            // For admins, we expect an Update event with DeletedAt set.
            // If we only get a Deleted event, we don't know the DeletedAt value, 
            // but we can at least mark it as deleted if we find it.
            var report = Reports.FirstOrDefault(r => r.Id == id);
            if (report is not { DeletedAt: null })
            {
                return;
            }

            report.DeletedAt = DateTime.UtcNow;
            OnStateChanged?.Invoke();
            return;
        }

        Reports.RemoveAll(r => r.Id == id);
        if (MapInitialized)
        {
            _ = _mapService.RemoveSingleReportAsync(id);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleAlertAdded(Alert alert)
    {
        if (alert.DeletedAt != null)
        {
            return;
        }

        // Don't show other users' alerts unless we are an admin
        if (alert.UserIdentifier != _userIdentifier && !_isAdmin)
        {
            return;
        }

        Alerts ??= [];
        Alerts.Insert(0, alert);
        if (MapInitialized)
        {
            _ = _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleAlertUpdated(Alert alert)
    {
        // Don't show other users' alerts unless we are an admin
        if (alert.UserIdentifier != _userIdentifier && !_isAdmin)
        {
            return;
        }

        var index = Alerts.FindIndex(a => a.Id == alert.Id);
        if (index != -1)
        {
            if (alert.DeletedAt == null)
            {
                Alerts[index] = alert;
            }
            else
            {
                Alerts.RemoveAt(index);
            }
            
            if (MapInitialized)
            {
                _ = _mapService.UpdateAlertsAsync(Alerts);
            }
            OnStateChanged?.Invoke();
        }
        else if (alert.DeletedAt == null)
        {
            HandleAlertAdded(alert);
        }
    }

    private void HandleAlertDeleted(int id)
    {
        if (_isAdmin)
        {
            return;
        }

        var alert = Alerts.FirstOrDefault(a => a.Id == id);
        if (alert == null)
        {
            return;
        }

        Alerts.Remove(alert);
        if (MapInitialized)
        {
            _ = _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _pruneTimer?.Dispose();
        _eventService.OnEntityChanged -= HandleEntityChanged;
    }
}
