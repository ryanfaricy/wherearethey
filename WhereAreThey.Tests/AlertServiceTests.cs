using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Validators;
using Xunit;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using MediatR;
using WhereAreThey.Events;

namespace WhereAreThey.Tests;

public class AlertServiceTests
{
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<ILogger<AlertService>> _loggerMock = new();
    private readonly Mock<IStringLocalizer<App>> _localizerMock = new();

    public AlertServiceTests()
    {
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        _localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => new LocalizedString(key, key));
    }

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

    private ISettingsService CreateSettingsService(IDbContextFactory<ApplicationDbContext> factory)
    {
        return new SettingsService(factory);
    }

    private IAlertService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var settingsService = CreateSettingsService(factory);
        var validator = new AlertValidator(factory, settingsService, _localizerMock.Object);
        return new AlertService(factory, _dataProtectionProvider, _emailServiceMock.Object, _mediatorMock.Object, _configurationMock.Object, _loggerMock.Object, settingsService, validator, _localizerMock.Object);
    }

    [Fact]
    public async Task CreateAlert_ShouldCreateActiveAlertAndEncryptEmail()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var email = "test@example.com";
        var alert = new Alert
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            RadiusKm = 5.0,
            Message = "Test alert",
            UserIdentifier = "test-user"
        };

        // Act
        var originalExternalId = alert.ExternalId;
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.NotEqual(originalExternalId, result.ExternalId);
        Assert.True(result.IsActive);
        Assert.False(result.IsVerified); // New alerts are not verified by default
        Assert.NotNull(result.EncryptedEmail);
        Assert.NotEqual(email, result.EncryptedEmail);
        Assert.Equal(email, service.DecryptEmail(result.EncryptedEmail));
        
        // Verify event was published
        _mediatorMock.Verify(x => x.Publish(
            It.Is<AlertCreatedEvent>(e => e.Alert.Id == result.Id && e.Email == email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldOnlyReturnActiveAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            var activeAlert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 5.0,
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                EncryptedEmail = "encrypted"
            };
            
            var inactiveAlert = new Alert
            {
                Latitude = 41.0,
                Longitude = -75.0,
                RadiusKm = 5.0,
                IsActive = false,
                IsVerified = true,
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
    public async Task GetActiveAlerts_ShouldNotReturnUnverifiedAlertsByDefault()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40.0, Longitude = -74.0, RadiusKm = 5.0, IsActive = true, IsVerified = false,
                CreatedAt = DateTime.UtcNow, EncryptedEmail = "encrypted"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var resultsDefault = await service.GetActiveAlertsAsync();
        var resultsAll = await service.GetActiveAlertsAsync(onlyVerified: false);

        // Assert
        Assert.Empty(resultsDefault);
        Assert.Single(resultsAll);
    }

    [Fact]
    public async Task VerifyEmail_ShouldMarkAlertsAsVerified()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var email = "verify@example.com";
        
        var alert = await service.CreateAlertAsync(new Alert { Latitude = 40, Longitude = -74, RadiusKm = 5, UserIdentifier = "test-user" }, email);
        Assert.False(alert.IsVerified);

        // Manually trigger verification email to create verification record
        await service.SendVerificationEmailAsync(email, AlertService.ComputeHash(email));

        string? token;
        using (var context = new ApplicationDbContext(options))
        {
            var verification = await context.EmailVerifications.FirstAsync();
            token = verification.Token;
        }

        // Act
        var result1 = await service.VerifyEmailAsync(token);
        var result2 = await service.VerifyEmailAsync(token);

        // Assert
        Assert.True(result1);
        Assert.True(result2); // Should still return true if already verified
        using (var context = new ApplicationDbContext(options))
        {
            var updatedAlert = await context.Alerts.FindAsync(alert.Id);
            Assert.True(updatedAlert!.IsVerified);
            
            var verification = await context.EmailVerifications.FirstAsync();
            Assert.NotNull(verification.VerifiedAt);
        }
    }

    [Fact]
    public async Task DeactivateAlert_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
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
        var service = CreateService(factory);
        
        var validAlertId = 0;
        using (var context = new ApplicationDbContext(options))
        {
            var expiredAlert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 5.0,
                IsActive = true,
                IsVerified = true,
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
                IsVerified = true,
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
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            var alert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 10.0,
                IsActive = true,
                IsVerified = true,
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
        var service = CreateService(factory);
        var userId1 = "user1";
        var userId2 = "user2";

        using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40, Longitude = -74, RadiusKm = 5, IsActive = true, IsVerified = true,
                UserIdentifier = userId1, CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc"
            });
            context.Alerts.Add(new Alert
            {
                Latitude = 41, Longitude = -75, RadiusKm = 5, IsActive = true, IsVerified = true,
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
        var service = CreateService(factory);
        
        // Act
        var result = service.DecryptEmail("invalid-encrypted-data");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAlertByExternalId_ShouldReturnCorrectAlert()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var alert = new Alert { Latitude = 40.0, Longitude = -74.0, RadiusKm = 5.0, ExternalId = Guid.NewGuid() };
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetAlertByExternalIdAsync(alert.ExternalId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(alert.Id, result.Id);
        Assert.Equal(alert.ExternalId, result.ExternalId);
    }

    [Fact]
    public async Task CreateAlert_ShouldCapRadiusAt160_9()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var email = "test@example.com";
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -74.0,
            RadiusKm = 200.0,
            Message = "Too big radius",
            UserIdentifier = "test-user"
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

    [Fact]
    public async Task CreateAlert_ShouldEnforceLimit()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var userId = "limitedUser";
        var email = "test@example.com";
        
        _localizerMock.Setup(l => l["Alert_Cooldown_Error"])
            .Returns(new LocalizedString("Alert_Cooldown_Error", "You can only create {0} alert zones every {1} minutes."));

        // Create 3 alerts (the default limit)
        for (int i = 0; i < 3; i++)
        {
            await service.CreateAlertAsync(new Alert
            {
                Latitude = 40, Longitude = -74, RadiusKm = 5,
                UserIdentifier = userId
            }, email);
        }

        // The 4th one should fail
        var alert4 = new Alert
        {
            Latitude = 40, Longitude = -74, RadiusKm = 5,
            UserIdentifier = userId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            service.CreateAlertAsync(alert4, email));
        
        _localizerMock.Verify(l => l["Alert_Cooldown_Error"], Times.AtLeastOnce);
    }
}
