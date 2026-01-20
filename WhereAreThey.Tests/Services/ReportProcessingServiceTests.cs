using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class ReportProcessingServiceTests
{
    private readonly Mock<ILogger<ReportProcessingService>> _loggerMock = new();
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IWebPushService> _webPushServiceMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<ILocationService> _locationServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();

    private IReportProcessingService CreateService()
    {
        _baseUrlProviderMock.Setup(x => x.GetBaseUrl()).Returns("https://test.com");

        return new ReportProcessingService(
            _alertServiceMock.Object,
            _emailServiceMock.Object,
            _webPushServiceMock.Object,
            _geocodingServiceMock.Object,
            _baseUrlProviderMock.Object,
            _settingsServiceMock.Object,
            _locationServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessReport_ShouldSendEmailsToMatchingAlerts()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, EncryptedEmail = "enc-email", Message = "Alert msg" };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");
        _geocodingServiceMock.Setup(g => g.ReverseGeocodeAsync(report.Latitude, report.Longitude))
            .ReturnsAsync("123 Test St");

        _emailTemplateServiceMock.Setup(t => t.RenderTemplateAsync("AlertEmail", It.IsAny<AlertEmailViewModel>()))
            .ReturnsAsync("Rendered email body with 123 Test St and Alert msg");

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _emailServiceMock.Verify(e => e.SendEmailsAsync(
            It.Is<IEnumerable<Email>>(emails => 
                emails.Count() == 1 && 
                emails.First().To == "test@example.com" &&
                emails.First().Subject.Contains("Alert") &&
                emails.First().Body.Contains("123 Test St") &&
                emails.First().Body.Contains("Alert msg"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessReport_ShouldNotSendEmailIfUseEmailIsFalse()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, EncryptedEmail = "enc-email", UseEmail = false };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _emailServiceMock.Verify(e => e.SendEmailsAsync(It.IsAny<IEnumerable<Email>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReport_ShouldThrowAndLogExceptions()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ThrowsAsync(new Exception("Test exception"));
        var service = CreateService();
        var report = new Report { Id = 1 };

        // Act
        var exception = await Record.ExceptionAsync(() => service.ProcessReportAsync(report));

        // Assert
        Assert.NotNull(exception);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing alerts")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessReport_ShouldUseProvidedBaseUrl()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, EncryptedEmail = "enc-email", Message = "Alert msg" };
        var customBaseUrl = "https://custom.com";

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");
        _geocodingServiceMock.Setup(g => g.ReverseGeocodeAsync(report.Latitude, report.Longitude))
            .ReturnsAsync("123 Test St");

        _emailTemplateServiceMock.Setup(t => t.RenderTemplateAsync("AlertEmail", It.IsAny<AlertEmailViewModel>()))
            .Callback<string, object>((_, model) => {
                var viewModel = (AlertEmailViewModel)model;
                Assert.StartsWith(customBaseUrl, viewModel.HeatMapUrl);
            })
            .ReturnsAsync("Rendered email body");

        // Act
        await service.ProcessReportAsync(report, customBaseUrl);

        // Assert
        _emailTemplateServiceMock.Verify(t => t.RenderTemplateAsync("AlertEmail", It.IsAny<AlertEmailViewModel>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReport_ShouldSendPushNotifications()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid(), Message = "Test Message" };
        var alert = new Alert { Id = 1, UserIdentifier = "user-123", UsePush = true };
        var subscription = new WebPushSubscription { Id = 1, UserIdentifier = "user-123", Endpoint = "https://fcm.googleapis.com/..." };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.GetPushSubscriptionsAsync("user-123"))
            .ReturnsAsync([subscription]);

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _webPushServiceMock.Verify(w => w.SendNotificationsAsync(
            It.Is<IEnumerable<WebPushSubscription>>(subs => subs.Count() == 1 && subs.First().UserIdentifier == "user-123"),
            It.Is<string>(t => t.Contains("New Report")),
            It.Is<string>(m => m == "Test Message"),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessReport_ShouldNotSendPushIfUsePushIsFalse()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid(), Message = "Test Message" };
        var alert = new Alert { Id = 1, UserIdentifier = "user-123", UsePush = false };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _webPushServiceMock.Verify(w => w.SendNotificationsAsync(It.IsAny<IEnumerable<WebPushSubscription>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReport_ShouldNotSendEmails_WhenGlobalToggleIsOff()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, EncryptedEmail = "enc-email", UseEmail = true };
        
        var settings = new SystemSettings { EmailNotificationsEnabled = false };
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _emailServiceMock.Verify(e => e.SendEmailsAsync(It.IsAny<IEnumerable<Email>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReport_ShouldNotSendPush_WhenGlobalToggleIsOff()
    {
        // Arrange
        var service = CreateService();
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, UserIdentifier = "user-123", UsePush = true };
        
        var settings = new SystemSettings { PushNotificationsEnabled = false };
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _webPushServiceMock.Verify(w => w.SendNotificationsAsync(It.IsAny<IEnumerable<WebPushSubscription>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
