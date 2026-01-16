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
    private string? _userIdentifier;
    private bool _isAdmin;

    /// <inheritdoc />
    public List<LocationReport> Reports { get; private set; } = new();

    /// <inheritdoc />
    public List<Alert> Alerts { get; private set; } = new();

    private bool _mapInitialized;
    /// <inheritdoc />
    public bool MapInitialized 
    { 
        get => _mapInitialized; 
        set 
        {
            if (_mapInitialized != value)
            {
                _mapInitialized = value;
                if (_mapInitialized)
                {
                    // When the map initializes, ensure it has the latest data
                    _ = _mapService.UpdateHeatMapAsync(Reports);
                    _ = _mapService.UpdateAlertsAsync(Alerts);
                }
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
            Alerts = (await _alertService.GetAllAlertsAdminAsync()).Where(a => a.IsActive).ToList();
        }
        else if (!string.IsNullOrEmpty(_userIdentifier))
        {
            Alerts = await _alertService.GetActiveAlertsAsync(_userIdentifier, false);
        }

        if (MapInitialized)
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
        Reports.Insert(0, report);
        if (MapInitialized)
        {
            _ = _mapService.AddSingleReportAsync(report);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleReportUpdated(LocationReport report)
    {
        var index = Reports.FindIndex(r => r.Id == report.Id);
        if (index != -1)
        {
            Reports[index] = report;
            if (MapInitialized)
            {
                _ = UpdateReportOnMap(report);
            }
            OnStateChanged?.Invoke();
        }
    }

    private async Task UpdateReportOnMap(LocationReport report)
    {
        await _mapService.RemoveSingleReportAsync(report.Id);
        await _mapService.AddSingleReportAsync(report);
    }

    private void HandleReportDeleted(int id)
    {
        Reports.RemoveAll(r => r.Id == id);
        if (MapInitialized)
        {
            _ = _mapService.RemoveSingleReportAsync(id);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleAlertAdded(Alert alert)
    {
        if (!alert.IsActive) return;
        if (!_isAdmin && alert.UserIdentifier != _userIdentifier) return;

        Alerts.Insert(0, alert);
        if (MapInitialized)
        {
            _ = _mapService.UpdateAlertsAsync(Alerts);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleAlertUpdated(Alert alert)
    {
        if (!_isAdmin && alert.UserIdentifier != _userIdentifier) return;

        var index = Alerts.FindIndex(a => a.Id == alert.Id);
        if (index != -1)
        {
            if (alert.IsActive)
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
        else if (alert.IsActive)
        {
            HandleAlertAdded(alert);
        }
    }

    private void HandleAlertDeleted(int id)
    {
        var alert = Alerts.FirstOrDefault(a => a.Id == id);
        if (alert != null)
        {
            Alerts.Remove(alert);
            if (MapInitialized)
            {
                _ = _mapService.UpdateAlertsAsync(Alerts);
            }
            OnStateChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _eventService.OnReportAdded -= HandleReportAdded;
        _eventService.OnReportUpdated -= HandleReportUpdated;
        _eventService.OnReportDeleted -= HandleReportDeleted;
        _eventService.OnAlertAdded -= HandleAlertAdded;
        _eventService.OnAlertUpdated -= HandleAlertUpdated;
        _eventService.OnAlertDeleted -= HandleAlertDeleted;
    }
}
