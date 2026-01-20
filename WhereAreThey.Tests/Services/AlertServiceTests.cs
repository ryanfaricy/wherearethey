using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests.Services;

public class AlertServiceTests
{
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<ILogger<AlertService>> _loggerMock = new();
    private readonly Mock<IStringLocalizer<App>> _localizerMock = new();

    public AlertServiceTests()
    {
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        _localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] _) => new LocalizedString(key, key));
        _emailTemplateServiceMock.Setup(t => t.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("Rendered email body");
        _baseUrlProviderMock.Setup(x => x.GetBaseUrl()).Returns("https://test.com");
    }

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

    private IAlertService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var settingsService = CreateSettingsService(factory);
        var validator = new AlertValidator(factory, settingsService, _localizerMock.Object);
        return new AlertService(factory, _dataProtectionProvider, _emailServiceMock.Object, _backgroundJobClientMock.Object, _eventServiceMock.Object, _baseUrlProviderMock.Object, _emailTemplateServiceMock.Object, _loggerMock.Object, validator);
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
            UserIdentifier = "test-user",
            UsePush = false, // Set to false to test email verification logic
        };

        // Act
        var originalExternalId = alert.ExternalId;
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.True(result.IsSuccess);
        var createdAlert = result.Value!;
        Assert.NotEqual(0, createdAlert.Id);
        Assert.NotEqual(originalExternalId, createdAlert.ExternalId);
        Assert.Null(createdAlert.DeletedAt);
        Assert.False(createdAlert.IsVerified); // New alerts are not verified by default
        Assert.NotNull(createdAlert.EncryptedEmail);
        Assert.NotEqual(email, createdAlert.EncryptedEmail);
        Assert.Equal(email, service.DecryptEmail(createdAlert.EncryptedEmail));
        
        // Verify background job was enqueued
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(IAlertService.SendVerificationEmailAsync) && (string)job.Args[0] == email),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task CreateAlert_ShouldHaveDefaultNotificationSettings()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var alert = new Alert
        {
            Latitude = 40.0,
            Longitude = -70.0,
            RadiusKm = 5.0,
            UserIdentifier = "test-user",
        };

        // Act
        var result = await service.CreateAlertAsync(alert, "test@example.com");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.UseEmail);
        Assert.True(result.Value!.UsePush);
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldOnlyReturnActiveAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            var activeAlert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 5.0,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                EncryptedEmail = "encrypted",
            };
            
            var inactiveAlert = new Alert
            {
                Latitude = 41.0,
                Longitude = -75.0,
                RadiusKm = 5.0,
                DeletedAt = DateTime.UtcNow,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                EncryptedEmail = "encrypted",
            };

            context.Alerts.Add(activeAlert);
            context.Alerts.Add(inactiveAlert);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetActiveAlertsAsync();

        // Assert
        Assert.Single(results);
        Assert.Null(results[0].DeletedAt);
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldNotReturnUnverifiedAlertsByDefault()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40.0, Longitude = -74.0, RadiusKm = 5.0, IsVerified = false,
                CreatedAt = DateTime.UtcNow, EncryptedEmail = "encrypted",
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
        
        var createResult = await service.CreateAlertAsync(new Alert { Latitude = 40, Longitude = -74, RadiusKm = 5, UserIdentifier = "test-user", UsePush = false }, email);
        var alert = createResult.Value!;
        Assert.False(alert.IsVerified);

        // Manually trigger verification email to create verification record
        await service.SendVerificationEmailAsync(email, HashUtils.ComputeHash(email));

        string? token;
        await using (var context = new ApplicationDbContext(options))
        {
            var verification = await context.EmailVerifications.FirstAsync();
            token = verification.Token;
        }

        // Act
        var result1 = await service.VerifyEmailAsync(token);
        var result2 = await service.VerifyEmailAsync(token);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess); // Should still return true if already verified
        await using (var context = new ApplicationDbContext(options))
        {
            var updatedAlert = await context.Alerts.FindAsync(alert.Id);
            Assert.True(updatedAlert!.IsVerified);
            
            var verification = await context.EmailVerifications.FirstAsync();
            Assert.NotNull(verification.VerifiedAt);
        }
    }

    [Fact]
    public async Task DeleteAlert_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        int alertId;
        await using (var context = new ApplicationDbContext(options))
        {
            var alert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 5.0,
                CreatedAt = DateTime.UtcNow,
                EncryptedEmail = "encrypted",
            };

            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
            alertId = alert.Id;
        }

        // Act
        var result = await service.DeleteAlertAsync(alertId);

        // Assert
        Assert.True(result.IsSuccess);
        await using (var context = new ApplicationDbContext(options))
        {
            var deactivatedAlert = await context.Alerts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == alertId);
            Assert.NotNull(deactivatedAlert!.DeletedAt);
        }
    }

    [Fact]
    public async Task GetMatchingAlerts_ShouldReturnAlertsWithinRadius()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            var alert = new Alert
            {
                Latitude = 40.0,
                Longitude = -74.0,
                RadiusKm = 10.0,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                EncryptedEmail = "encrypted",
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

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40, Longitude = -74, RadiusKm = 5, IsVerified = true,
                UserIdentifier = userId1, CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc",
            });
            context.Alerts.Add(new Alert
            {
                Latitude = 41, Longitude = -75, RadiusKm = 5, IsVerified = true,
                UserIdentifier = userId2, CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc",
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecryptEmail_ShouldReturnNullForNullOrEmptyInput(string? input)
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        // Act
        var result = service.DecryptEmail(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ComputeHash_ShouldBeConsistentAndNormalized()
    {
        // Arrange
        var email1 = "Test@Example.Com ";
        var email2 = "test@example.com";

        // Act
        var hash1 = HashUtils.ComputeHash(email1);
        var hash2 = HashUtils.ComputeHash(email2);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA256 hex string
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldHandleInvalidToken()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        // Act
        var result = await service.VerifyEmailAsync("non-existent-token");

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldMarkAlertsAsVerified()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var email = "verify@example.com";
        var emailHash = HashUtils.ComputeHash(email);
        var token = "secret-token";

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.EmailVerifications.Add(new EmailVerification
            {
                EmailHash = emailHash,
                Token = token,
                CreatedAt = DateTime.UtcNow,
            });
            context.Alerts.Add(new Alert
            {
                EmailHash = emailHash,
                IsVerified = false,
                Latitude = 0, Longitude = 0, RadiusKm = 1,
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.VerifyEmailAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        await using (var context = await factory.CreateDbContextAsync())
        {
            var alert = await context.Alerts.FirstAsync(a => a.EmailHash == emailHash);
            Assert.True(alert.IsVerified);
            var verification = await context.EmailVerifications.FirstAsync(v => v.EmailHash == emailHash);
            Assert.NotNull(verification.VerifiedAt);
        }
    }

    [Fact]
    public async Task GetMatchingAlerts_ShouldNotReturnUnverifiedAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(new Alert
            {
                Latitude = 40.0, Longitude = -74.0, RadiusKm = 10.0,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow, EncryptedEmail = "enc",
            });
            await context.SaveChangesAsync();
        }

        // Act
        var matches = await service.GetMatchingAlertsAsync(40.0, -74.0);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public async Task CreateAlert_ShouldUseExistingVerification_WhenEmailAlreadyVerified()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var email = "already-verified@example.com";
        var emailHash = HashUtils.ComputeHash(email);

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.EmailVerifications.Add(new EmailVerification
            {
                EmailHash = emailHash,
                Token = "token",
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            });
            await context.SaveChangesAsync();
        }

        var alert = new Alert { Latitude = 0, Longitude = 0, RadiusKm = 1, UserIdentifier = "verified-user" };

        // Act
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsVerified);
        
        // Verify background job was NOT enqueued since already verified
        _backgroundJobClientMock.Verify(x => x.Create(
            It.IsAny<Job>(),
            It.IsAny<EnqueuedState>()), Times.Never);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(alert.Id, result.Value!.Id);
        Assert.Equal(alert.ExternalId, result.Value!.ExternalId);
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
            UserIdentifier = "test-user",
        };

        // Act
        var result = await service.CreateAlertAsync(alert, email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(160.9, result.Value!.RadiusKm);
        await using var context = new ApplicationDbContext(options);
        var savedAlert = await context.Alerts.FindAsync(result.Value!.Id);
        Assert.Equal(160.9, savedAlert!.RadiusKm);
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
        for (var i = 0; i < 3; i++)
        {
            await service.CreateAlertAsync(new Alert
            {
                Latitude = 40, Longitude = -74, RadiusKm = 5,
                UserIdentifier = userId,
            }, email);
        }

        // The 4th one should fail
        var alert4 = new Alert
        {
            Latitude = 40, Longitude = -74, RadiusKm = 5,
            UserIdentifier = userId,
        };

        // Act
        var result = await service.CreateAlertAsync(alert4, email);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("You can only create", result.Error);
        
        _localizerMock.Verify(l => l["Alert_Cooldown_Error"], Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteAlert_ShouldNotifyEventService()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var alert = new Alert { Latitude = 0, Longitude = 0, RadiusKm = 1 };
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.DeleteAlertAsync(alert.Id);

        // Assert
        Assert.True(result.IsSuccess);
        _eventServiceMock.Verify(x => x.NotifyEntityChanged(It.IsAny<Alert>(), EntityChangeType.Updated), Times.Once);
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            var deleted = await context.Alerts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == alert.Id);
            Assert.NotNull(deleted);
            Assert.NotNull(deleted.DeletedAt);
        }
    }
}
