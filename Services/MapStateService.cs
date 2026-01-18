using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
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
    public List<LocationReport> Reports { get; private set; } = [];

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
            if (!_isAdmin)
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

        _eventService.OnReportAdded += HandleReportAdded;
        _eventService.OnReportUpdated += HandleReportUpdated;
        _eventService.OnReportDeleted += HandleReportDeleted;
        _eventService.OnAlertAdded += HandleAlertAdded;
        _eventService.OnAlertUpdated += HandleAlertUpdated;
        _eventService.OnAlertDeleted += HandleAlertDeleted;

        _pruneTimer = new Timer(_ => PruneOldReports(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void PruneOldReports()
    {
        if (_isAdmin || !_lastLoadedHours.HasValue)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddHours(-_lastLoadedHours.Value);
        var toRemove = Reports.Where(r => r.Timestamp < cutoff).ToList();
        
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
        Reports = _isAdmin 
            ? await _reportService.GetAllReportsAsync() 
            : await _reportService.GetRecentReportsAsync(hours);
        
        if (MapInitialized)
        {
            await _mapService.UpdateHeatMapAsync(Reports);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task LoadAlertsAsync()
    {
        if (_isAdmin)
        {
            Alerts = []; // Admin heat map should not display alert zones
        }
        else if (!string.IsNullOrEmpty(_userIdentifier))
        {
            Alerts = await _alertService.GetActiveAlertsAsync(_userIdentifier, false);
        }

        if (MapInitialized && !_isAdmin)
        {
            await _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public List<LocationReport> FindNearbyReports(double lat, double lng, double radiusKm)
    {
        return Reports
            .Where(r => GeoUtils.CalculateDistance(lat, lng, r.Latitude, r.Longitude) <= radiusKm)
            .OrderByDescending(r => r.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public List<Alert> FindNearbyAlerts(double lat, double lng, double radiusKm)
    {
        return Alerts
            .Where(a => GeoUtils.CalculateDistance(lat, lng, a.Latitude, a.Longitude) <= radiusKm)
            .ToList();
    }

    private void HandleReportAdded(LocationReport report)
    {
        if (report.DeletedAt != null && !_isAdmin)
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

    private void HandleReportUpdated(LocationReport report)
    {
        var index = Reports.FindIndex(r => r.Id == report.Id);
        
        if (report.DeletedAt != null && !_isAdmin)
        {
            if (index != -1)
            {
                Reports.RemoveAt(index);
                if (MapInitialized)
                {
                    _ = _mapService.RemoveSingleReportAsync(report.Id);
                }
                OnStateChanged?.Invoke();
            }
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

    private async Task UpdateReportOnMap(LocationReport report)
    {
        if (report.DeletedAt != null && !_isAdmin)
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
            if (report != null && report.DeletedAt == null)
            {
                report.DeletedAt = DateTime.UtcNow;
                OnStateChanged?.Invoke();
            }
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

        if (_isAdmin || (!_isAdmin && alert.UserIdentifier != _userIdentifier))
        {
            return;
        }

        Alerts.Insert(0, alert);
        if (MapInitialized)
        {
            _ = _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleAlertUpdated(Alert alert)
    {
        if (_isAdmin || (!_isAdmin && alert.UserIdentifier != _userIdentifier))
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
        _eventService.OnReportAdded -= HandleReportAdded;
        _eventService.OnReportUpdated -= HandleReportUpdated;
        _eventService.OnReportDeleted -= HandleReportDeleted;
        _eventService.OnAlertAdded -= HandleAlertAdded;
        _eventService.OnAlertUpdated -= HandleAlertUpdated;
        _eventService.OnAlertDeleted -= HandleAlertDeleted;
    }
}
