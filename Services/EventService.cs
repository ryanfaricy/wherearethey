using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class EventService : IEventService
{
    /// <inheritdoc />
    public event Action<LocationReport>? OnReportAdded;
    /// <inheritdoc />
    public event Action<LocationReport>? OnReportUpdated;
    /// <inheritdoc />
    public event Action<int>? OnReportDeleted;
    /// <inheritdoc />
    public event Action<Feedback>? OnFeedbackAdded;
    /// <inheritdoc />
    public event Action<int>? OnFeedbackDeleted;
    /// <inheritdoc />
    public event Action<Feedback>? OnFeedbackUpdated;
    /// <inheritdoc />
    public event Action<Donation>? OnDonationAdded;
    /// <inheritdoc />
    public event Action<Donation>? OnDonationUpdated;
    /// <inheritdoc />
    public event Action<int>? OnDonationDeleted;
    /// <inheritdoc />
    public event Action<Alert>? OnAlertAdded;
    /// <inheritdoc />
    public event Action<Alert>? OnAlertUpdated;
    /// <inheritdoc />
    public event Action<int>? OnAlertDeleted;
    /// <inheritdoc />
    public event Action<SystemSettings>? OnSettingsChanged;
    /// <inheritdoc />
    public event Action<AdminLoginAttempt>? OnAdminLoginAttempt;
    /// <inheritdoc />
    public event Action<string>? OnEmailVerified;
    /// <inheritdoc />
    public event Action? OnConnectionCountChanged;
    /// <inheritdoc />
    public event Action? OnThemeChanged;

    /// <inheritdoc />
    public void NotifyReportAdded(LocationReport report) => OnReportAdded?.Invoke(report);
    /// <inheritdoc />
    public void NotifyReportUpdated(LocationReport report) => OnReportUpdated?.Invoke(report);
    /// <inheritdoc />
    public void NotifyReportDeleted(int id) => OnReportDeleted?.Invoke(id);
    /// <inheritdoc />
    public void NotifyFeedbackAdded(Feedback feedback) => OnFeedbackAdded?.Invoke(feedback);
    /// <inheritdoc />
    public void NotifyFeedbackDeleted(int id) => OnFeedbackDeleted?.Invoke(id);
    /// <inheritdoc />
    public void NotifyFeedbackUpdated(Feedback feedback) => OnFeedbackUpdated?.Invoke(feedback);
    /// <inheritdoc />
    public void NotifyDonationAdded(Donation donation) => OnDonationAdded?.Invoke(donation);
    /// <inheritdoc />
    public void NotifyDonationUpdated(Donation donation) => OnDonationUpdated?.Invoke(donation);
    /// <inheritdoc />
    public void NotifyDonationDeleted(int id) => OnDonationDeleted?.Invoke(id);
    /// <inheritdoc />
    public void NotifyAlertAdded(Alert alert) => OnAlertAdded?.Invoke(alert);
    /// <inheritdoc />
    public void NotifyAlertUpdated(Alert alert) => OnAlertUpdated?.Invoke(alert);
    /// <inheritdoc />
    public void NotifyAlertDeleted(int id) => OnAlertDeleted?.Invoke(id);
    /// <inheritdoc />
    public void NotifySettingsChanged(SystemSettings settings) => OnSettingsChanged?.Invoke(settings);
    /// <inheritdoc />
    public void NotifyAdminLoginAttempt(AdminLoginAttempt attempt) => OnAdminLoginAttempt?.Invoke(attempt);
    /// <inheritdoc />
    public void NotifyEmailVerified(string emailHash) => OnEmailVerified?.Invoke(emailHash);
    /// <inheritdoc />
    public void NotifyConnectionCountChanged() => OnConnectionCountChanged?.Invoke();
    /// <inheritdoc />
    public void NotifyThemeChanged() => OnThemeChanged?.Invoke();
}
