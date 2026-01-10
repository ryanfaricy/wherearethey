using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AlertServiceTests
{
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAlert_ShouldCreateActiveAlertAndEncryptEmail()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context, _dataProtectionProvider);
        var email = "test@example.com";
        var alert = new Alert
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            RadiusKm = 5.0,
            Message = "Test alert"
        };

        // Act
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.True(result.IsActive);
        Assert.NotNull(result.EncryptedEmail);
        Assert.NotEqual(email, result.EncryptedEmail);
        Assert.Equal(email, service.DecryptEmail(result.EncryptedEmail));
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldOnlyReturnActiveAlerts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context, _dataProtectionProvider);
        
        var activeAlert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EncryptedEmail = "encrypted"
        };
        
        var inactiveAlert = new Alert
        {
            Latitude = 41.0,
            Longitude = -75.0,
            RadiusKm = 5.0,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            EncryptedEmail = "encrypted"
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
        var service = new AlertService(context, _dataProtectionProvider);
        
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EncryptedEmail = "encrypted"
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
        var service = new AlertService(context, _dataProtectionProvider);
        
        var expiredAlert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            EncryptedEmail = "encrypted"
        };
        
        var validAlert = new Alert
        {
            Latitude = 41.0,
            Longitude = -75.0,
            RadiusKm = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            EncryptedEmail = "encrypted"
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

    [Fact]
    public async Task GetMatchingAlerts_ShouldReturnAlertsWithinRadius()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context, _dataProtectionProvider);
        
        // Alert at (40, -74) with 10km radius
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 10.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EncryptedEmail = "encrypted"
        };
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        // Point at (40.01, -74.01) is ~1.4km away
        var matchesNear = await service.GetMatchingAlertsAsync(40.01, -74.01);
        
        // Point at (41.0, -75.0) is ~140km away
        var matchesFar = await service.GetMatchingAlertsAsync(41.0, -75.0);

        // Assert
        Assert.Single(matchesNear);
        Assert.Empty(matchesFar);
    }

    [Fact]
    public async Task CreateAlert_ShouldCapRadiusAt160_9()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new AlertService(context, _dataProtectionProvider);
        var email = "test@example.com";
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 200.0,
            Message = "Too big radius"
        };

        // Act
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.Equal(160.9, result.RadiusKm);
        var savedAlert = await context.Alerts.FindAsync(result.Id);
        Assert.Equal(160.9, savedAlert!.RadiusKm);
    }
}
