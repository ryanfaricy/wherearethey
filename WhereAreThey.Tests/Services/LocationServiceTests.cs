using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();

    private static DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        mock.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationDbContext(options));
        return mock.Object;
    }

    private ISettingsService CreateSettingsService(IDbContextFactory<ApplicationDbContext> factory)
    {
        return new SettingsService(factory, _eventServiceMock.Object);
    }

    private ILocationService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var settingsService = CreateSettingsService(factory);
        return new LocationService(factory, settingsService, _loggerMock.Object);
    }

    [Fact]
    public async Task GetReportsInRadius_ShouldReturnNearbyReports()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        // New York City coordinates
        var centerLat = 40.7128;
        var centerLon = -74.0060;
        
        var nearReport = new Report
        {
            Latitude = 40.7580, // ~5 km away
            Longitude = -73.9855,
            CreatedAt = DateTime.UtcNow,
        };
        
        var farReport = new Report
        {
            Latitude = 42.3601, // ~200 km away
            Longitude = -71.0589,
            CreatedAt = DateTime.UtcNow,
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.Reports.Add(nearReport);
            context.Reports.Add(farReport);
            await context.SaveChangesAsync();
        }

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
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var centerLat = 40.0;
        var centerLon = -74.0;
        var radiusKm = 10.0;

        // Point inside roughly 10km north
        // 1 degree lat approx 111km -> 10km is ~0.09 degrees
        var insideRadius = new Report { Latitude = 40.0 + (9.9 / 111.0), Longitude = -74.0, CreatedAt = DateTime.UtcNow };
        var justOutsideRadius = new Report { Latitude = 40.0 + (10.2 / 111.0), Longitude = -74.0, CreatedAt = DateTime.UtcNow };

        await using (var context = new ApplicationDbContext(options))
        {
            context.Reports.Add(insideRadius);
            context.Reports.Add(justOutsideRadius);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetReportsInRadiusAsync(centerLat, centerLon, radiusKm);

        // Assert
        Assert.Single(results);
        Assert.Equal(insideRadius.Latitude, results[0].Latitude);
    }

    [Fact]
    public void GetFormattedLocalTime_ShouldReturnCorrectLocalTime()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        // New York (Eastern Time)
        var lat = 40.7128;
        var lon = -74.0060;
        var utcTimestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc); // 12 PM UTC

        // Act
        var result = service.GetFormattedLocalTime(lat, lon, utcTimestamp);

        // Assert
        // Eastern Time is UTC-5 in January
        Assert.Contains("7:00 AM", result);
        Assert.Contains("America/New_York", result);
    }
}
