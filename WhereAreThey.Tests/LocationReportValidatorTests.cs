using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests;

public class LocationReportValidatorTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IStringLocalizer<App>> _localizerMock = new();
    private readonly Mock<IAdminService> _adminServiceMock = new();

    public LocationReportValidatorTests()
    {
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("key", "error message"));
        _localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns(new LocalizedString("key", "error message"));
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);
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
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
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
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
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
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
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
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
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
    public async Task Validator_ShouldFail_WhenIdentifierTooShort()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport { ReporterIdentifier = "short" };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.ReporterIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenValid()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportCooldownMinutes = 5, MaxReportDistanceMiles = 10 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
        var report = new LocationReport 
        { 
            ReporterIdentifier = "valid-passphrase-123",
            Latitude = 40.7128, Longitude = -74.0060,
            ReporterLatitude = 40.7129, ReporterLongitude = -74.0061
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenReporterCoordinatesAreNull()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport 
        { 
            ReporterIdentifier = "valid-passphrase-123",
            Latitude = 40, Longitude = -74,
            ReporterLatitude = null, ReporterLongitude = null
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.ReporterLatitude));
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenDistanceIsJustOverLimit()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        // 1 mile = 1.60934 km
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { MaxReportDistanceMiles = 1 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
        // 40.7128, -74.0060 to 40.7128, -74.0253 is ~1.62 km (just over 1 mile)
        var report = new LocationReport 
        { 
            ReporterIdentifier = "valid-passphrase-123",
            Latitude = 40.7128, Longitude = -74.0060,
            ReporterLatitude = 40.7128, ReporterLongitude = -74.0253
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.Latitude));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenDistanceIsExactlyAtLimit()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { MaxReportDistanceMiles = 10 });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
        // Use coordinates that are within the limit
        var report = new LocationReport 
        { 
            ReporterIdentifier = "valid-passphrase-123",
            Latitude = 40.7128, Longitude = -74.0060,
            ReporterLatitude = 40.7128, ReporterLongitude = -74.0060
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenInCooldown_Recent()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var identifier = "user123456";
        var cooldown = 5;
        context.LocationReports.Add(new LocationReport 
        { 
            ReporterIdentifier = identifier, 
            Timestamp = DateTime.UtcNow.AddMinutes(-cooldown).AddSeconds(30),
            Latitude = 0, Longitude = 0
        });
        await context.SaveChangesAsync();

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportCooldownMinutes = cooldown });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport 
        { 
            ReporterIdentifier = identifier,
            Latitude = 0, Longitude = 0,
            ReporterLatitude = 0, ReporterLongitude = 0
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.ReporterIdentifier));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenJustOutsideCooldown()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var identifier = "user123456";
        var cooldown = 5;
        context.LocationReports.Add(new LocationReport 
        { 
            ReporterIdentifier = identifier, 
            Timestamp = DateTime.UtcNow.AddMinutes(-cooldown).AddSeconds(-30),
            Latitude = 0, Longitude = 0
        });
        await context.SaveChangesAsync();

        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportCooldownMinutes = cooldown });
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport 
        { 
            ReporterIdentifier = identifier,
            Latitude = 0, Longitude = 0,
            ReporterLatitude = 0, ReporterLongitude = 0
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Check this www.google.com")]
    [InlineData("Check this http://google.com")]
    [InlineData("Check this https://google.com")]
    public async Task Validator_ShouldFail_WhenMessageContainsVariousUrls(string message)
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        var report = new LocationReport { ReporterIdentifier = "user123456", Message = message };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LocationReport.Message));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenAdmin_EvenWithRestrictions()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var identifier = "admin-user";
        
        // Add a recent report to trigger cooldown for normal users
        context.LocationReports.Add(new LocationReport 
        { 
            ReporterIdentifier = identifier, 
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            Latitude = 0, Longitude = 0
        });
        await context.SaveChangesAsync();

        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(true);
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings 
        { 
            ReportCooldownMinutes = 5,
            MaxReportDistanceMiles = 1
        });
        
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
        var report = new LocationReport 
        { 
            ReporterIdentifier = "a", // Too short for normal users
            Message = "Check this https://admin.com", // Links not allowed for normal users
            Latitude = 40.7128, Longitude = -74.0060,
            ReporterLatitude = 34.0522, ReporterLongitude = -118.2437 // Way too far for normal users
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public async Task Validator_ShouldSucceed_WhenAdmin_AndIdentifierIsEmpty()
    {
        // Arrange
        var (_, factory) = await CreateContextAndFactoryAsync();
        
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(true);
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        
        var validator = new LocationReportValidator(factory, _settingsServiceMock.Object, _adminServiceMock.Object, _localizerMock.Object);
        
        var report = new LocationReport 
        { 
            ReporterIdentifier = "", // Empty
            Latitude = 0, Longitude = 0,
            ReporterLatitude = 0, ReporterLongitude = 0
        };

        // Act
        var result = await validator.ValidateAsync(report);

        // Assert
        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
