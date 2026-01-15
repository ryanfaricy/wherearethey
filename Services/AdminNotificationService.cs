using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AdminNotificationService : IAdminNotificationService
{
    public event Action<LocationReport>? OnReportAdded;
    public event Action<int>? OnReportDeleted;
    public event Action<Feedback>? OnFeedbackAdded;
    public event Action<int>? OnFeedbackDeleted;
    public event Action<Donation>? OnDonationAdded;
    public event Action<Donation>? OnDonationUpdated;
    public event Action<Alert>? OnAlertAdded;
    public event Action<int>? OnAlertDeleted;
    public event Action<SystemSettings>? OnSettingsChanged;
    public event Action<AdminLoginAttempt>? OnAdminLoginAttempt;
    public event Action<string>? OnEmailVerified;

    public void NotifyReportAdded(LocationReport report) => OnReportAdded?.Invoke(report);
    public void NotifyReportDeleted(int id) => OnReportDeleted?.Invoke(id);
    public void NotifyFeedbackAdded(Feedback feedback) => OnFeedbackAdded?.Invoke(feedback);
    public void NotifyFeedbackDeleted(int id) => OnFeedbackDeleted?.Invoke(id);
    public void NotifyDonationAdded(Donation donation) => OnDonationAdded?.Invoke(donation);
    public void NotifyDonationUpdated(Donation donation) => OnDonationUpdated?.Invoke(donation);
    public void NotifyAlertAdded(Alert alert) => OnAlertAdded?.Invoke(alert);
    public void NotifyAlertDeleted(int id) => OnAlertDeleted?.Invoke(id);
    public void NotifySettingsChanged(SystemSettings settings) => OnSettingsChanged?.Invoke(settings);
    public void NotifyAdminLoginAttempt(AdminLoginAttempt attempt) => OnAdminLoginAttempt?.Invoke(attempt);
    public void NotifyEmailVerified(string emailHash) => OnEmailVerified?.Invoke(emailHash);
}
