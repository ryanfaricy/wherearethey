using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface IAdminNotificationService
{
    event Action<LocationReport> OnReportAdded;
    event Action<int> OnReportDeleted;
    event Action<Feedback> OnFeedbackAdded;
    event Action<int> OnFeedbackDeleted;
    event Action<Donation> OnDonationAdded;
    event Action<Donation> OnDonationUpdated;
    event Action<Alert> OnAlertAdded;
    event Action<int> OnAlertDeleted;

    void NotifyReportAdded(LocationReport report);
    void NotifyReportDeleted(int id);
    void NotifyFeedbackAdded(Feedback feedback);
    void NotifyFeedbackDeleted(int id);
    void NotifyDonationAdded(Donation donation);
    void NotifyDonationUpdated(Donation donation);
    void NotifyAlertAdded(Alert alert);
    void NotifyAlertDeleted(int id);
}
