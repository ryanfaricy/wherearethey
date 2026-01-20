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
    private readonly ISettingsService _settingsService;
    private readonly Timer? _pruneTimer;
    private readonly Lock _lock = new();
    private string? _userIdentifier;
    private bool _isAdmin;
    private int? _lastLoadedHours;
    private bool _isAllLoaded;

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

    private bool _showDeleted;
    /// <inheritdoc />
    public bool ShowDeleted 
    { 
        get => _showDeleted;
        set
        {
            if (_showDeleted == value)
            {
                return;
            }

            _showDeleted = value;
            if (_isAdmin)
            {
                if (_isAllLoaded)
                {
                    _ = LoadAllReportsAsync();
                }
                else
                {
                    _ = LoadReportsAsync(_lastLoadedHours);
                }
                _ = LoadAlertsAsync();
            }
            OnStateChanged?.Invoke();
        }
    }

    private int _cachedExpiryHours = 24;

    /// <inheritdoc />
    public event Action? OnStateChanged;

    public MapStateService(
        IReportService reportService,
        IAlertService alertService,
        IEventService eventService,
        IMapService mapService,
        ISettingsService settingsService)
    {
        _reportService = reportService;
        _alertService = alertService;
        _eventService = eventService;
        _mapService = mapService;
        _settingsService = settingsService;

        _eventService.OnEntityChanged += HandleEntityChanged;
        _eventService.OnSettingsChanged += HandleSettingsChanged;

        _ = InitializeSettingsAsync();
        _pruneTimer = new Timer(x => _ = PruneOldReportsAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void HandleSettingsChanged(SystemSettings settings)
    {
        _cachedExpiryHours = settings.ReportExpiryHours;
    }

    private async Task InitializeSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            _cachedExpiryHours = settings.ReportExpiryHours;
        }
        catch
        {
            // Fallback to default
        }
    }

    private bool ShouldShowReport(Report report)
    {
        if (!VisibilityPolicy.ShouldShow(report, _isAdmin, ShowDeleted))
        {
            return false;
        }

        // Expiry check: only skip if we are in "All" mode as an admin
        if (_isAdmin && _isAllLoaded)
        {
            return true;
        }

        var hours = _lastLoadedHours ?? _cachedExpiryHours;
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return report.CreatedAt >= cutoff;
    }

    private bool ShouldShowAlert(Alert alert)
    {
        // Alert zones from other profiles should NEVER appear on ANY heatmap.
        // Also ensures the admin heatmap (where _userIdentifier is null) displays no alerts.
        if (string.IsNullOrEmpty(_userIdentifier) || alert.UserIdentifier != _userIdentifier)
        {
            return false;
        }

        return VisibilityPolicy.ShouldShow(alert, _isAdmin, ShowDeleted);
    }

    private async Task PruneOldReportsAsync()
    {
        if (_isAdmin && _isAllLoaded)
        {
            return;
        }

        var settings = await _settingsService.GetSettingsAsync();
        var hours = _lastLoadedHours ?? settings.ReportExpiryHours;
        _cachedExpiryHours = settings.ReportExpiryHours; // Sync cache while we're at it
        
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        
        List<Report> toRemove;
        lock (_lock)
        {
            toRemove = Reports.Where(r => r.CreatedAt < cutoff).ToList();
            
            if (toRemove.Count == 0)
            {
                return;
            }

            foreach (var report in toRemove)
            {
                Reports.Remove(report);
            }
        }

        if (MapInitialized)
        {
            foreach (var report in toRemove)
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
        if (!_isAdmin)
        {
            _showDeleted = false;
        }
        await LoadAlertsAsync();
    }

    /// <inheritdoc />
    public async Task LoadReportsAsync(int? hours = null)
    {
        _lastLoadedHours = hours;
        _isAllLoaded = false;
        
        var allReports = await _reportService.GetRecentReportsAsync(hours, _isAdmin && ShowDeleted);
        lock (_lock)
        {
            Reports = allReports.Where(ShouldShowReport).ToList();
        }
        
        if (MapInitialized)
        {
            await _mapService.UpdateHeatMapAsync(Reports);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task LoadAllReportsAsync()
    {
        _lastLoadedHours = null;
        _isAllLoaded = true;
        
        var allReports = await _reportService.GetAllReportsAsync();
        lock (_lock)
        {
            Reports = allReports.Where(ShouldShowReport).ToList();
        }
        
        if (MapInitialized)
        {
            await _mapService.UpdateHeatMapAsync(Reports);
        }
        OnStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task LoadAlertsAsync()
    {
        List<Alert> loadedAlerts = [];
        
        // Only load alerts if we have a user identifier (User heatmap).
        // Admin heatmap calls InitializeAsync with null identifier, correctly resulting in zero alerts.
        if (!string.IsNullOrEmpty(_userIdentifier))
        {
            // Fetch alerts for current user. Admins see their own deleted/unverified alerts if ShowDeleted is true.
            loadedAlerts = await _alertService.GetActiveAlertsAsync(
                _userIdentifier, 
                onlyVerified: !(_isAdmin && ShowDeleted),
                includeDeleted: _isAdmin && ShowDeleted) ?? [];
        }

        lock (_lock)
        {
            Alerts = loadedAlerts.Where(ShouldShowAlert).ToList();
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

    /// <inheritdoc />
    public void AddReportToState(Report report)
    {
        lock (_lock)
        {
            if (Reports.Any(r => r.Id == report.Id))
            {
                return;
            }
            Reports.Insert(0, report);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleEntityChanged(object entity, EntityChangeType type)
    {
        if (entity is Report report)
        {
            HandleReportChange(report, type);
        }
        else if (entity is Alert alert)
        {
            HandleAlertChange(alert, type);
        }
    }

    private void HandleReportChange(Report report, EntityChangeType type)
    {
        var changed = false;
        var shouldShow = type != EntityChangeType.Deleted && ShouldShowReport(report);
        var callRemoveOnMap = false;
        var callAddOnMap = false;
        var callUpdateOnMap = false;

        lock (_lock)
        {
            var index = Reports.FindIndex(r => r.Id == report.Id);
            if (shouldShow)
            {
                if (index != -1)
                {
                    Reports[index] = report;
                    callUpdateOnMap = true;
                }
                else
                {
                    Reports.Insert(0, report);
                    callAddOnMap = true;
                }
                changed = true;
            }
            else if (index != -1)
            {
                Reports.RemoveAt(index);
                callRemoveOnMap = true;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        if (MapInitialized)
        {
            if (callRemoveOnMap)
            {
                _ = _mapService.RemoveSingleReportAsync(report.Id);
            }
            else if (callAddOnMap)
            {
                _ = _mapService.AddSingleReportAsync(report);
            }
            else if (callUpdateOnMap)
            {
                _ = UpdateReportOnMap(report);
            }
        }
        OnStateChanged?.Invoke();
    }

    private async Task UpdateReportOnMap(Report report)
    {
        if (!VisibilityPolicy.ShouldShow(report, _isAdmin, ShowDeleted))
        {
            await _mapService.RemoveSingleReportAsync(report.Id);
        }
        else
        {
            await _mapService.RemoveSingleReportAsync(report.Id);
            await _mapService.AddSingleReportAsync(report);
        }
    }

    private void HandleAlertChange(Alert alert, EntityChangeType type)
    {
        var changed = false;
        var shouldShow = type != EntityChangeType.Deleted && ShouldShowAlert(alert);

        lock (_lock)
        {
            var index = Alerts.FindIndex(a => a.Id == alert.Id);
            if (shouldShow)
            {
                if (index != -1)
                {
                    Alerts[index] = alert;
                }
                else
                {
                    Alerts.Insert(0, alert);
                }

                changed = true;
            }
            else if (index != -1)
            {
                Alerts.RemoveAt(index);
                changed = true;
            }
        }

        if (changed)
        {
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
        GC.SuppressFinalize(this);
        
        _pruneTimer?.Dispose();
        _eventService.OnEntityChanged -= HandleEntityChanged;
        _eventService.OnSettingsChanged -= HandleSettingsChanged;
    }
}
