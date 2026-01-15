using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests;

public class AdminNotificationServiceTests
{
    [Fact]
    public void NotifyReportAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        LocationReport? receivedReport = null;
        service.OnReportAdded += report => receivedReport = report;
        var report = new LocationReport { Id = 1 };

        // Act
        service.NotifyReportAdded(report);

        // Assert
        Assert.Equal(report, receivedReport);
    }

    [Fact]
    public void NotifyReportDeleted_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        var receivedId = 0;
        service.OnReportDeleted += id => receivedId = id;

        // Act
        service.NotifyReportDeleted(123);

        // Assert
        Assert.Equal(123, receivedId);
    }

    [Fact]
    public void NotifyFeedbackAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        Feedback? receivedFeedback = null;
        service.OnFeedbackAdded += f => receivedFeedback = f;
        var feedback = new Feedback { Id = 1 };

        // Act
        service.NotifyFeedbackAdded(feedback);

        // Assert
        Assert.Equal(feedback, receivedFeedback);
    }

    [Fact]
    public void NotifyDonationAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        Donation? receivedDonation = null;
        service.OnDonationAdded += d => receivedDonation = d;
        var donation = new Donation { Id = 1 };

        // Act
        service.NotifyDonationAdded(donation);

        // Assert
        Assert.Equal(donation, receivedDonation);
    }

    [Fact]
    public void NotifyAlertAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        Alert? receivedAlert = null;
        service.OnAlertAdded += a => receivedAlert = a;
        var alert = new Alert { Id = 1 };

        // Act
        service.NotifyAlertAdded(alert);

        // Assert
        Assert.Equal(alert, receivedAlert);
    }

    [Fact]
    public void NotifySettingsChanged_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        SystemSettings? receivedSettings = null;
        service.OnSettingsChanged += s => receivedSettings = s;
        var settings = new SystemSettings { Id = 1 };

        // Act
        service.NotifySettingsChanged(settings);

        // Assert
        Assert.Equal(settings, receivedSettings);
    }

    [Fact]
    public void NotifyAdminLoginAttempt_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        AdminLoginAttempt? receivedAttempt = null;
        service.OnAdminLoginAttempt += a => receivedAttempt = a;
        var attempt = new AdminLoginAttempt { Id = 1 };

        // Act
        service.NotifyAdminLoginAttempt(attempt);

        // Assert
        Assert.Equal(attempt, receivedAttempt);
    }

    [Fact]
    public void NotifyEmailVerified_ShouldInvokeEvent()
    {
        // Arrange
        var service = new AdminNotificationService();
        string? receivedHash = null;
        service.OnEmailVerified += h => receivedHash = h;

        // Act
        service.NotifyEmailVerified("abc");

        // Assert
        Assert.Equal("abc", receivedHash);
    }
}
