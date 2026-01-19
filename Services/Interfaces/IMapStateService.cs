using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Maintains the live state of reports and alerts for the map, 
/// handling real-time updates from the event service.
/// </summary>
public interface IMapStateService : IDisposable
{
    /// <summary>
    /// The current list of active reports.
    /// </summary>
    List<Report> Reports { get; }

    /// <summary>
    /// The current list of active alerts.
    /// </summary>
    List<Alert> Alerts { get; }

    /// <summary>
    /// Indicates if the map has been initialized.
    /// </summary>
    bool MapInitialized { get; set; }

    /// <summary>
    /// Whether to show soft-deleted items (Admin only).
    /// </summary>
    bool ShowDeleted { get; set; }

    /// <summary>
    /// Triggered when reports or alerts are updated.
    /// </summary>
    event Action OnStateChanged;

    /// <summary>
    /// Initializes the state service for a specific user.
    /// </summary>
    Task InitializeAsync(string? userIdentifier, bool isAdmin = false);

    /// <summary>
    /// Loads reports for the specified time window.
    /// </summary>
    Task LoadReportsAsync(int? hours = null);

    /// <summary>
    /// Loads alerts (user-specific or all for admin).
    /// </summary>
    Task LoadAlertsAsync();

    /// <summary>
    /// Finds reports near a coordinate.
    /// </summary>
    List<Report> FindNearbyReports(double lat, double lng, double radiusKm);

    /// <summary>
    /// Finds alerts near a coordinate.
    /// </summary>
    List<Alert> FindNearbyAlerts(double lat, double lng, double radiusKm);
}
