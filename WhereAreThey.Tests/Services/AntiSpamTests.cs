using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests.Services;

public class AntiSpamTests
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<ILogger<ReportService>> _loggerMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IAdminService> _adminServiceMock = new();

    public AntiSpamTests()
    {
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);
    }

    private static IStringLocalizer<App> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<App>>();
        mock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => 
        {
            var val = key switch
            {
                "Links_Error" => "Links are not allowed in reports to prevent spam.",
                "Location_Verify_Error" => "Unable to verify your current location. Please ensure GPS is enabled.",
                "Feedback_Links_Error" => "Links are not allowed in feedback to prevent spam.",
                "Cooldown_Error" => "You can only make one report every {0} minutes.",
                "Distance_Error" => "You can only make a report within {0} miles of your location.",
                "Feedback_Cooldown_Error" => "You can only submit one feedback every {0} minutes.",
                _ => key,
            };
            return new LocalizedString(key, val);
        });
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => 
        {
            var val = key switch
            {
                "Cooldown_Error" => "You can only make one report every {0} minutes.",
                "Distance_Error" => "You can only make a report within {0} miles of your location.",
                "Feedback_Cooldown_Error" => "You can only submit one feedback every {0} minutes.",
                _ => key,
            };
            return new LocalizedString(key, string.Format(val, args));
        });
        return mock.Object;
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

    private IReportService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var localizer = CreateLocalizer();
        var settingsService = CreateSettingsService(factory);
        var validator = new ReportValidator(factory, settingsService, _adminServiceMock.Object, localizer);
        return new ReportService(factory, _backgroundJobClientMock.Object, settingsService, _eventServiceMock.Object, validator, _loggerMock.Object);
    }

    [Fact]
    public async Task AddReport_ShouldFail_WhenTooFarFromReporter()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 41.0, // ~111km away
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA",
        };

        // Act
        var result = await service.CreateReportAsync(report);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("5.0 miles", result.Error);
    }

    [Fact]
    public async Task AddReport_ShouldFail_WhenReportingTooFrequently()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report1 = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA-Passphrase",
        };

        var report2 = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA-Passphrase",
        };

        // Act
        await service.CreateReportAsync(report1);
        var result = await service.CreateReportAsync(report2);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("5 minutes", result.Error);
    }

    [Fact]
    public async Task AddReport_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA-Passphrase",
            Message = "Check out my site: https://spam.com",
        };

        // Act
        var result = await service.CreateReportAsync(report);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Links are not allowed", result.Error);
    }

    [Fact]
    public async Task GetRecentReports_ShouldBeLimitedBySettings()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            // Report from 12 hours ago
            context.Reports.Add(new Report 
            { 
                Latitude = 40.0, Longitude = -74.0, 
                CreatedAt = DateTime.UtcNow.AddHours(-12),
            });
            // Report from 2 hours ago
            context.Reports.Add(new Report 
            { 
                Latitude = 41.0, Longitude = -75.0, 
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(41.0, results[0].Latitude);
    }

    [Fact]
    public async Task Settings_ShouldBeConfigurable()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var settingsService = CreateSettingsService(factory);

        var customSettings = new SystemSettings 
        { 
            ReportExpiryHours = 24, 
            ReportCooldownMinutes = 10,
            MaxReportDistanceMiles = 50.0m,
            DonationsEnabled = false,
        };
        await settingsService.UpdateSettingsAsync(customSettings);

        // Act
        var retrieved = await settingsService.GetSettingsAsync();

        // Assert
        Assert.False(retrieved.DonationsEnabled);

        await using (var context = new ApplicationDbContext(options))
        {
            // Report from 12 hours ago - should now be included
            context.Reports.Add(new Report 
            { 
                Latitude = 40.0, Longitude = -74.0, 
                CreatedAt = DateTime.UtcNow.AddHours(-12),
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync();

        // Assert
        Assert.Single(results);
        
        // Check cooldown
        var report = new Report { ReporterIdentifier = "UserB-Passphrase", Latitude = 40, Longitude = -74, ReporterLatitude = 40, ReporterLongitude = -74 };
        await service.CreateReportAsync(report);
        
        var resultCooldown = await service.CreateReportAsync(report);
        Assert.True(resultCooldown.IsFailure);
        Assert.Contains("10 minutes", resultCooldown.Error);

        // Check distance
        var farReport = new Report { 
            ReporterIdentifier = "UserC-Passphrase", 
            Latitude = 40, Longitude = -74, 
            ReporterLatitude = 41.0, ReporterLongitude = -74, // ~111km away
            CreatedAt = DateTime.UtcNow,
        };
        var resultDist = await service.CreateReportAsync(farReport);
        Assert.True(resultDist.IsFailure);
        Assert.Contains("50.0 miles", resultDist.Error);
    }
}
