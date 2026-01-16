using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for broadcasting and subscribing to application-wide events.
/// </summary>
public interface IEventService
{
    /// <summary>Raised when a new location report is added.</summary>
    event Action<LocationReport> OnReportAdded;
    /// <summary>Raised when a location report is updated.</summary>
    event Action<LocationReport> OnReportUpdated;
    /// <summary>Raised when a location report is deleted.</summary>
    event Action<int> OnReportDeleted;
    /// <summary>Raised when new feedback is submitted.</summary>
    event Action<Feedback> OnFeedbackAdded;
    /// <summary>Raised when feedback is deleted.</summary>
    event Action<int> OnFeedbackDeleted;
    /// <summary>Raised when a new donation is added.</summary>
    event Action<Donation> OnDonationAdded;
    /// <summary>Raised when a donation is updated.</summary>
    event Action<Donation> OnDonationUpdated;
    /// <summary>Raised when a new alert is added.</summary>
    event Action<Alert> OnAlertAdded;
    /// <summary>Raised when an alert is updated.</summary>
    event Action<Alert> OnAlertUpdated;
    /// <summary>Raised when an alert is deleted.</summary>
    event Action<int> OnAlertDeleted;
    /// <summary>Raised when system settings are changed.</summary>
    event Action<SystemSettings> OnSettingsChanged;
    /// <summary>Raised when an administrative login attempt occurs.</summary>
    event Action<AdminLoginAttempt> OnAdminLoginAttempt;
    /// <summary>Raised when an email address is verified.</summary>
    event Action<string> OnEmailVerified;
    /// <summary>Raised when the number of active connections changes.</summary>
    event Action OnConnectionCountChanged;
    /// <summary>Raised when the application theme is changed.</summary>
    event Action OnThemeChanged;

    /// <summary>Notifies subscribers that a report has been added.</summary>
    void NotifyReportAdded(LocationReport report);
    /// <summary>Notifies subscribers that a report has been updated.</summary>
    void NotifyReportUpdated(LocationReport report);
    /// <summary>Notifies subscribers that a report has been deleted.</summary>
    void NotifyReportDeleted(int id);
    /// <summary>Notifies subscribers that feedback has been added.</summary>
    void NotifyFeedbackAdded(Feedback feedback);
    /// <summary>Notifies subscribers that feedback has been deleted.</summary>
    void NotifyFeedbackDeleted(int id);
    /// <summary>Notifies subscribers that a donation has been added.</summary>
    void NotifyDonationAdded(Donation donation);
    /// <summary>Notifies subscribers that a donation has been updated.</summary>
    void NotifyDonationUpdated(Donation donation);
    /// <summary>Notifies subscribers that an alert has been added.</summary>
    void NotifyAlertAdded(Alert alert);
    /// <summary>Notifies subscribers that an alert has been updated.</summary>
    void NotifyAlertUpdated(Alert alert);
    /// <summary>Notifies subscribers that an alert has been deleted.</summary>
    void NotifyAlertDeleted(int id);
    /// <summary>Notifies subscribers that settings have changed.</summary>
    void NotifySettingsChanged(SystemSettings settings);
    /// <summary>Notifies subscribers of an admin login attempt.</summary>
    void NotifyAdminLoginAttempt(AdminLoginAttempt attempt);
    /// <summary>Notifies subscribers that an email has been verified.</summary>
    void NotifyEmailVerified(string emailHash);
    /// <summary>Notifies subscribers that the connection count has changed.</summary>
    void NotifyConnectionCountChanged();
    /// <summary>Notifies subscribers that the theme has changed.</summary>
    void NotifyThemeChanged();
}
