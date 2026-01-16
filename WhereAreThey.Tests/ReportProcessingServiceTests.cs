using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests;

public class ReportProcessingServiceTests
{
    private readonly Mock<ILogger<ReportProcessingService>> _loggerMock = new();
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<ILocationService> _locationServiceMock = new();

    private IReportProcessingService CreateService(IServiceProvider serviceProvider)
    {
        return new ReportProcessingService(
            serviceProvider,
            Options.Create(new AppOptions()),
            _settingsServiceMock.Object,
            _locationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessReport_ShouldSendEmailsToMatchingAlerts()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_alertServiceMock.Object);
        services.AddSingleton(_emailServiceMock.Object);
        services.AddSingleton(_geocodingServiceMock.Object);
        services.AddSingleton(_settingsServiceMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var service = CreateService(serviceProvider);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ExternalId = Guid.NewGuid() };
        var alert = new Alert { Id = 1, EncryptedEmail = "enc-email", Message = "Alert msg" };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _alertServiceMock.Setup(a => a.GetMatchingAlertsAsync(report.Latitude, report.Longitude))
            .ReturnsAsync([alert]);
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");
        _geocodingServiceMock.Setup(g => g.ReverseGeocodeAsync(report.Latitude, report.Longitude))
            .ReturnsAsync("123 Test St");

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
    public async Task ProcessReport_ShouldThrowAndLogExceptions()
    {
        // Arrange
        var services = new ServiceCollection();
        // This will cause an exception when trying to resolve services if not added
        var serviceProvider = services.BuildServiceProvider();

        var service = CreateService(serviceProvider);
        var report = new LocationReport { Id = 1 };

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
}
