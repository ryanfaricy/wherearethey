using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;
using WhereAreThey.Components;
using Xunit;

namespace WhereAreThey.Tests.Services;

public class AlertServiceUpdateTests
{
    private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
    private readonly Mock<IDataProtector> _dataProtectorMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock;
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly Mock<IStringLocalizer<App>> _localizerMock;

    public AlertServiceUpdateTests()
    {
        _dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        _dataProtectorMock = new Mock<IDataProtector>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _eventServiceMock = new Mock<IEventService>();
        _baseUrlProviderMock = new Mock<IBaseUrlProvider>();
        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AlertService>>();
        _localizerMock = new Mock<IStringLocalizer<App>>();

        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        _localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _dataProtectionProviderMock
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_dataProtectorMock.Object);
        
        _dataProtectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(b => b);
        _dataProtectorMock
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns<byte[]>(b => b);

        _baseUrlProviderMock.Setup(p => p.GetBaseUrl()).Returns("https://test.com");
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None)).ReturnsAsync(() => new ApplicationDbContext(options));
        return factoryMock.Object;
    }

    private IAlertService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());

        var validator = new AlertValidator(factory, settingsServiceMock.Object, _localizerMock.Object);
        return new AlertService(
            factory,
            _dataProtectionProviderMock.Object,
            _emailServiceMock.Object,
            _backgroundJobClientMock.Object,
            _eventServiceMock.Object,
            _baseUrlProviderMock.Object,
            _emailTemplateServiceMock.Object,
            _loggerMock.Object,
            validator);
    }

    [Fact]
    public async Task UpdateAlert_ShouldReVerify_WhenEmailChanges()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        var alert = new Alert
        {
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = true,
            UseEmail = true,
            UsePush = false,
            EncryptedEmail = "old-email-protected",
            EmailHash = HashUtils.ComputeHash("old@test.com")
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        var updatedAlert = new Alert
        {
            Id = alert.Id,
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = true, // Still true in the object being passed
            UseEmail = true,
            UsePush = false
        };

        // Act
        var result = await service.UpdateAlertAsync(updatedAlert, "new@test.com");

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FirstAsync(a => a.Id == alert.Id);
            Assert.False(savedAlert.IsVerified);
            Assert.Equal(HashUtils.ComputeHash("new@test.com"), savedAlert.EmailHash);
        }

        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(IAlertService.SendVerificationEmailAsync) && (string)job.Args[0] == "new@test.com"),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAlert_ShouldNotReVerify_WhenEmailAlreadyVerified()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var verifiedEmail = "verified@test.com";
        var verifiedEmailHash = HashUtils.ComputeHash(verifiedEmail);

        await using (var context = new ApplicationDbContext(options))
        {
            context.EmailVerifications.Add(new EmailVerification
            {
                EmailHash = verifiedEmailHash,
                VerifiedAt = DateTime.UtcNow,
                Token = "token"
            });
            await context.SaveChangesAsync();
        }

        var alert = new Alert
        {
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = true,
            UseEmail = true
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        var updatedAlert = new Alert
        {
            Id = alert.Id,
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = true,
            UseEmail = true
        };

        // Act
        var result = await service.UpdateAlertAsync(updatedAlert, verifiedEmail);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FirstAsync(a => a.Id == alert.Id);
            Assert.True(savedAlert.IsVerified);
        }

        _backgroundJobClientMock.Verify(x => x.Create(
            It.IsAny<Job>(),
            It.IsAny<EnqueuedState>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAlert_ShouldMarkAsVerified_WhenUsePushIsTrue()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        var alert = new Alert
        {
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = false,
            UseEmail = true,
            UsePush = false
        };

        await using (var context = new ApplicationDbContext(options))
        {
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        var updatedAlert = new Alert
        {
            Id = alert.Id,
            Latitude = 10,
            Longitude = 10,
            RadiusKm = 5,
            UserIdentifier = "user1",
            IsVerified = false,
            UseEmail = true,
            UsePush = true // Changed to true
        };

        // Act
        var result = await service.UpdateAlertAsync(updatedAlert);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FirstAsync(a => a.Id == alert.Id);
            Assert.True(savedAlert.IsVerified);
        }
    }
}
