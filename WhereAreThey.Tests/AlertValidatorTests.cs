using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests;

public class AlertValidatorTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IStringLocalizer<App>> _localizerMock;

    public AlertValidatorTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _localizerMock = new Mock<IStringLocalizer<App>>();
        
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));
        
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { AlertLimitCount = 3, ReportCooldownMinutes = 60 });
    }

    private static async Task<(ApplicationDbContext, IDbContextFactory<ApplicationDbContext>)> CreateContextAndFactoryAsync()
    {
        await Task.CompletedTask;
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None)).ReturnsAsync(() => new ApplicationDbContext(options));

        return (context, factoryMock.Object);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenIdentifierIsEmpty()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        var validator = new AlertValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var alert = new Alert { UserIdentifier = "" };

        // Act
        var result = await validator.ValidateAsync(alert);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Alert.UserIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        var validator = new AlertValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var alert = new Alert { UserIdentifier = "valid-id", Message = "Check out https://spam.com" };

        // Act
        var result = await validator.ValidateAsync(alert);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Alert.Message));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenAtLimit()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var userId = "test-user";
        
        // Add 3 recent alerts
        context.Alerts.AddRange(
            new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
            new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-30) }
        );
        await context.SaveChangesAsync();

        var validator = new AlertValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var alert = new Alert { UserIdentifier = userId };

        // Act
        var result = await validator.ValidateAsync(alert);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Alert.UserIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenBelowLimit()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var userId = "test-user";
        
        context.Alerts.AddRange(
            new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-20) }
        );
        await context.SaveChangesAsync();

        var validator = new AlertValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var alert = new Alert { UserIdentifier = userId };

        // Act
        var result = await validator.ValidateAsync(alert);

        // Assert
        Assert.True(result.IsValid);
    }
}
