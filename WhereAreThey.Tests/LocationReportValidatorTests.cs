using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Validators;
using Xunit;

namespace WhereAreThey.Tests;

public class LocationReportValidatorTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IStringLocalizer<App>> _localizerMock = new();

    public LocationReportValidatorTests()
    {
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("key", "error message"));
        _localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns(new LocalizedString("key", "error message"));
    }

    private async Task<(ApplicationDbContext, IDbContextFactory<ApplicationDbContext>)> CreateContextAndFactoryAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new ApplicationDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(default)).ReturnsAsync(() => new ApplicationDbContext(options));

        return (context, factoryMock.Object);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenIdentifierIsEmpty()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport { ReporterIdentifier = "" };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.ReporterIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport { ReporterIdentifier = "user", Message = "Check this https://spam.com" };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.Message));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenInCooldown()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var identifier = "user123";
        context.LocationReports.Add(new LocationReport 
        { 
            ReporterIdentifier = identifier, 
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            Latitude = 0,
            Longitude = 0
        });
        await context.SaveChangesAsync();

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportCooldownMinutes = 5 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport 
        { 
            ReporterIdentifier = identifier,
            Latitude = 0,
            Longitude = 0,
            ReporterLatitude = 0,
            ReporterLongitude = 0
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.ReporterIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenDistanceTooGreat()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { MaxReportDistanceMiles = 1 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        
        var report = new LocationReport 
        { 
            ReporterIdentifier = "user",
            Latitude = 40.7128, Longitude = -74.0060, // NYC
            ReporterLatitude = 34.0522, ReporterLongitude = -118.2437 // LA
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.Latitude));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenValid()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportCooldownMinutes = 5, MaxReportDistanceMiles = 10 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _localizerMock.Object);
        
        var report = new LocationReport 
        { 
            ReporterIdentifier = "user",
            Latitude = 40.7128, Longitude = -74.0060,
            ReporterLatitude = 40.7129, ReporterLongitude = -74.0061
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid);
    }
}
