using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AlertServiceTests
{
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();

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
    public async Task CreateAlert_ShouldCreateActiveAlertAndEncryptEmail()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
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
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        
        using (var context = new ApplicationDbContext(options))
        {
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
        }

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
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        
        int alertId;
        using (var context = new ApplicationDbContext(options))
        {
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
            alertId = alert.Id;
        }

        // Act
        var result = await service.DeactivateAlertAsync(alertId);

        // Assert
        Assert.True(result);
        using (var context = new ApplicationDbContext(options))
        {
            var deactivatedAlert = await context.Alerts.FindAsync(alertId);
            Assert.False(deactivatedAlert!.IsActive);
        }
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldNotReturnExpiredAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        
        var validAlertId = 0;
        using (var context = new ApplicationDbContext(options))
        {
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
            validAlertId = validAlert.Id;
        }

        // Act
        var results = await service.GetActiveAlertsAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(validAlertId, results[0].Id);
    }

    [Fact]
    public async Task GetMatchingAlerts_ShouldReturnAlertsWithinRadius()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        
        using (var context = new ApplicationDbContext(options))
        {
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
        }

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
    public async Task GetActiveAlerts_ShouldFilterByUserIdentifier()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        var userId1 = "user1";
        var userId2 = "user2";

        using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40, Longitude = -74, RadiusKm = 5, IsActive = true,
                UserIdentifier = userId1, CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc"
            });
            context.Alerts.Add(new Alert
            {
                Latitude = 41, Longitude = -75, RadiusKm = 5, IsActive = true,
                UserIdentifier = userId2, CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetActiveAlertsAsync(userId1);

        // Assert
        Assert.Single(results);
        Assert.Equal(userId1, results[0].UserIdentifier);
    }

    [Fact]
    public void DecryptEmail_ShouldReturnNullOnFailure()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
        
        // Act
        var result = service.DecryptEmail("invalid-encrypted-data");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAlert_ShouldCapRadiusAt160_9()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new AlertService(factory, _dataProtectionProvider);
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
        using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FindAsync(result.Id);
            Assert.Equal(160.9, savedAlert!.RadiusKm);
        }
    }
}
