using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class LocationServiceTests
{
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
        var service = new LocationService(context);
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
        var service = new LocationService(context);
        
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
    public async Task GetReportsInRadius_ShouldReturnNearbyReports()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LocationService(context);
        
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
}
