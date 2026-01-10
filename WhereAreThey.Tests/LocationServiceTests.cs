using Microsoft.AspNetCore.DataProtection;
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

public class LocationServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddLocationReport_ShouldAddReport()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        var report = new LocationReport
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            Message = "Test location",
            IsEmergency = false
        };

        // Act
        var result = await service.AddLocationReportAsync(report);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(40.7128, result.Latitude);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetRecentReports_ShouldReturnReportsWithinTimeRange()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        
        var oldReport = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            Timestamp = DateTime.UtcNow.AddHours(-48)
        };
        
        var recentReport = new LocationReport
        {
            Latitude = 41.0,
            Longitude = -75.0,
            Timestamp = DateTime.UtcNow.AddHours(-12)
        };

        context.LocationReports.Add(oldReport);
        context.LocationReports.Add(recentReport);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetRecentReportsAsync(24);

        // Assert
        Assert.Single(results);
        Assert.Equal(41.0, results[0].Latitude);
    }

    [Fact]
    public async Task GetRecentReports_ShouldReturnEmptyIfFutureCutoff()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        context.LocationReports.Add(new LocationReport { Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetRecentReportsAsync(-1); // hours = -1

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetReportsInRadius_ShouldReturnNearbyReports()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        
        // New York City coordinates
        var centerLat = 40.7128;
        var centerLon = -74.0060;
        
        var nearReport = new LocationReport
        {
            Latitude = 40.7580, // ~5 km away
            Longitude = -73.9855,
            Timestamp = DateTime.UtcNow
        };
        
        var farReport = new LocationReport
        {
            Latitude = 42.3601, // ~200 km away
            Longitude = -71.0589,
            Timestamp = DateTime.UtcNow
        };

        context.LocationReports.Add(nearReport);
        context.LocationReports.Add(farReport);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetReportsInRadiusAsync(centerLat, centerLon, 10.0);

        // Assert
        Assert.Single(results);
        Assert.Equal(nearReport.Latitude, results[0].Latitude);
    }

    [Fact]
    public async Task GetReportsInRadius_EdgeCases()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        var centerLat = 40.0;
        var centerLon = -74.0;
        var radiusKm = 10.0;

        // Point inside roughly 10km north
        // 1 degree lat approx 111km -> 10km is ~0.09 degrees
        var insideRadius = new LocationReport { Latitude = 40.0 + (9.9 / 111.0), Longitude = -74.0 };
        var justOutsideRadius = new LocationReport { Latitude = 40.0 + (10.2 / 111.0), Longitude = -74.0 };

        context.LocationReports.Add(insideRadius);
        context.LocationReports.Add(justOutsideRadius);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetReportsInRadiusAsync(centerLat, centerLon, radiusKm);

        // Assert
        Assert.Single(results);
        Assert.Equal(insideRadius.Latitude, results[0].Latitude);
    }

    [Fact]
    public async Task AddLocationReport_ShouldTriggerAlerts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var alertServiceMock = new Mock<AlertService>(context, new Moq.Mock<IDataProtectionProvider>().Object);
        var emailServiceMock = new Mock<IEmailService>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(AlertService))).Returns(alertServiceMock.Object);
        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IEmailService))).Returns(emailServiceMock.Object);

        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0 };
        var matchingAlert = new Alert { Latitude = 40.0, Longitude = -74.0, RadiusKm = 10.0, EncryptedEmail = "test" };

        alertServiceMock.Setup(x => x.GetMatchingAlertsAsync(It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new List<Alert> { matchingAlert });
        alertServiceMock.Setup(x => x.DecryptEmail(It.IsAny<string>())).Returns("test@example.com");

        // Act
        await service.AddLocationReportAsync(report);

        // Wait a bit for the background task
        await Task.Delay(200);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailAsync(
            It.Is<string>(s => s == "test@example.com"),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AddLocationReport_ShouldNotCrashOnAlertError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        
        // This will cause a NullReferenceException or similar when trying to resolve services from scope
        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(AlertService))).Throws(new Exception("Mock error"));

        var service = new LocationService(context, _serviceProviderMock.Object, _loggerMock.Object);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0 };

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.AddLocationReportAsync(report));
        Assert.Null(exception); // Should not throw

        // Wait for background task to finish and log error
        await Task.Delay(200);

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
