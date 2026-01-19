using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class EventServiceTests
{
    [Fact]
    public void NotifyReportAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
        object? receivedEntity = null;
        var receivedType = EntityChangeType.Updated;
        service.OnEntityChanged += (entity, type) => 
        {
            receivedEntity = entity;
            receivedType = type;
        };
        var report = new Report { Id = 1 };

        // Act
        service.NotifyEntityChanged(report, EntityChangeType.Added);

        // Assert
        Assert.Equal(report, receivedEntity);
        Assert.Equal(EntityChangeType.Added, receivedType);
    }

    [Fact]
    public void NotifyReportDeleted_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
        object? receivedEntity = null;
        var receivedType = EntityChangeType.Added;
        service.OnEntityChanged += (entity, type) => 
        {
            receivedEntity = entity;
            receivedType = type;
        };
        var report = new Report { Id = 123 };

        // Act
        service.NotifyEntityChanged(report, EntityChangeType.Deleted);

        // Assert
        Assert.Equal(report, receivedEntity);
        Assert.Equal(EntityChangeType.Deleted, receivedType);
    }

    [Fact]
    public void NotifyFeedbackAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
        object? receivedEntity = null;
        service.OnEntityChanged += (entity, type) => receivedEntity = entity;
        var feedback = new Feedback { Id = 1 };

        // Act
        service.NotifyEntityChanged(feedback, EntityChangeType.Added);

        // Assert
        Assert.Equal(feedback, receivedEntity);
    }

    [Fact]
    public void NotifyDonationAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
        object? receivedEntity = null;
        service.OnEntityChanged += (entity, type) => receivedEntity = entity;
        var donation = new Donation { Id = 1 };

        // Act
        service.NotifyEntityChanged(donation, EntityChangeType.Added);

        // Assert
        Assert.Equal(donation, receivedEntity);
    }

    [Fact]
    public void NotifyAlertAdded_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
        object? receivedEntity = null;
        service.OnEntityChanged += (entity, type) => receivedEntity = entity;
        var alert = new Alert { Id = 1 };

        // Act
        service.NotifyEntityChanged(alert, EntityChangeType.Added);

        // Assert
        Assert.Equal(alert, receivedEntity);
    }

    [Fact]
    public void NotifySettingsChanged_ShouldInvokeEvent()
    {
        // Arrange
        var service = new EventService();
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
        var service = new EventService();
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
        var service = new EventService();
        string? receivedHash = null;
        service.OnEmailVerified += h => receivedHash = h;

        // Act
        service.NotifyEmailVerified("abc");

        // Assert
        Assert.Equal("abc", receivedHash);
    }
}
