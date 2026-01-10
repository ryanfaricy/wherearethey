using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AlertServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAlert_ShouldCreateActiveAlert()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context);
        var alert = new Alert
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            RadiusKm = 5.0,
            Message = "Test alert"
        };

        // Act
        var result = await service.CreateAlertAsync(alert);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.True(result.IsActive);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldOnlyReturnActiveAlerts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context);
        
        var activeAlert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var inactiveAlert = new Alert
        {
            Latitude = 41.0,
            Longitude = -75.0,
            RadiusKm = 5.0,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Alerts.Add(activeAlert);
        context.Alerts.Add(inactiveAlert);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetActiveAlertsAsync();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsActive);
    }

    [Fact]
    public async Task DeactivateAlert_ShouldSetIsActiveToFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context);
        
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeactivateAlertAsync(alert.Id);

        // Assert
        Assert.True(result);
        var deactivatedAlert = await context.Alerts.FindAsync(alert.Id);
        Assert.False(deactivatedAlert!.IsActive);
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldNotReturnExpiredAlerts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context);
        
        var expiredAlert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        
        var validAlert = new Alert
        {
            Latitude = 41.0,
            Longitude = -75.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        context.Alerts.Add(expiredAlert);
        context.Alerts.Add(validAlert);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetActiveAlertsAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(validAlert.Id, results[0].Id);
    }
}
