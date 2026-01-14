using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class ReportProcessingServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<ILogger<ReportProcessingService>> _loggerMock = new();
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();

    private IReportProcessingService CreateService(IServiceProvider serviceProvider)
    {
        return new ReportProcessingService(
            serviceProvider,
            _configurationMock.Object,
            _settingsServiceMock.Object,
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
            .ReturnsAsync(new List<Alert> { alert });
        _alertServiceMock.Setup(a => a.DecryptEmail(alert.EncryptedEmail)).Returns("test@example.com");
        _geocodingServiceMock.Setup(g => g.ReverseGeocodeAsync(report.Latitude, report.Longitude))
            .ReturnsAsync("123 Test St");

        // Act
        await service.ProcessReportAsync(report);

        // Assert
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            "test@example.com",
            It.Is<string>(s => s.Contains("Alert")),
            It.Is<string>(b => b.Contains("123 Test St") && b.Contains("Alert msg"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessReport_ShouldHandleExceptionsGracefully()
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
        Assert.Null(exception);
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
