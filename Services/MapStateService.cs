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
        _eventService.OnSettingsChanged += settings => _cachedExpiryHours = settings.ReportExpiryHours;

        _ = InitializeSettingsAsync();
        _pruneTimer = new Timer(_ => _ = PruneOldReportsAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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
        _isAllLoaded = false;
        
        var allReports = await _reportService.GetRecentReportsAsync(hours, _isAdmin && ShowDeleted);
        Reports = allReports.Where(ShouldShowReport).ToList();
        
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
        Reports = allReports.Where(ShouldShowReport).ToList();
        
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
            var allAlerts = await _alertService.GetAllAlertsAsync();
            Alerts = allAlerts.Where(a => VisibilityPolicy.ShouldShow(a, _isAdmin, ShowDeleted)).ToList();
        }
        else if (!string.IsNullOrEmpty(_userIdentifier))
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
        if (!ShouldShowReport(report))
        {
            return;
        }

        Reports.Insert(0, report);
        if (MapInitialized)
        {
            _ = _mapService.AddSingleReportAsync(report);
        }
        OnStateChanged?.Invoke();
    }

    private void HandleReportUpdated(Report report)
    {
        var index = Reports.FindIndex(r => r.Id == report.Id);
        
        if (!ShouldShowReport(report))
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
        if (!VisibilityPolicy.ShouldShow(alert, _isAdmin, ShowDeleted))
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
            if (VisibilityPolicy.ShouldShow(alert, _isAdmin, ShowDeleted))
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
        else if (VisibilityPolicy.ShouldShow(alert, _isAdmin, ShowDeleted))
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
