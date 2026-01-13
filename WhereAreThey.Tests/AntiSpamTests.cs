using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AntiSpamTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContextAsync(default))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        mock.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationDbContext(options));
        return mock.Object;
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenTooFarFromReporter()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new LocationService(factory, _serviceProviderMock.Object, _loggerMock.Object, _configurationMock.Object);
        
        var report = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 41.0, // ~111km away
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report));
        Assert.Contains("five miles", ex.Message);
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenReportingTooFrequently()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new LocationService(factory, _serviceProviderMock.Object, _loggerMock.Object, _configurationMock.Object);
        
        var report1 = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        var report2 = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        // Act
        await service.AddLocationReportAsync(report1);
        
        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report2));
        Assert.Contains("five minutes", ex.Message);
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new LocationService(factory, _serviceProviderMock.Object, _loggerMock.Object, _configurationMock.Object);
        
        var report = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA",
            Message = "Check out my site: https://spam.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report));
        Assert.Contains("Links are not allowed", ex.Message);
    }

    [Fact]
    public async Task GetRecentReports_ShouldBeLimitedToSixHours()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new LocationService(factory, _serviceProviderMock.Object, _loggerMock.Object, _configurationMock.Object);
        
        using (var context = new ApplicationDbContext(options))
        {
            // Report from 12 hours ago
            context.LocationReports.Add(new LocationReport 
            { 
                Latitude = 40.0, Longitude = -74.0, 
                Timestamp = DateTime.UtcNow.AddHours(-12) 
            });
            // Report from 2 hours ago
            context.LocationReports.Add(new LocationReport 
            { 
                Latitude = 41.0, Longitude = -75.0, 
                Timestamp = DateTime.UtcNow.AddHours(-2) 
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(24);

        // Assert
        Assert.Single(results);
        Assert.Equal(41.0, results[0].Latitude);
    }
}
