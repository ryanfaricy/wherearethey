using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AntiSpamTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    private IStringLocalizer<App> CreateLocalizer()
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
                _ => key
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
                _ => key
            };
            return new LocalizedString(key, string.Format(val, args));
        });
        return mock.Object;
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

    private ILocationService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var localizer = CreateLocalizer();
        var settingsService = CreateSettingsService(factory);
        var validator = new SubmissionValidator(factory, localizer);
        var reportProcessingMock = new Mock<IReportProcessingService>();
        return new LocationService(factory, reportProcessingMock.Object, settingsService, validator, localizer);
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenTooFarFromReporter()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 41.0, // ~111km away
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report));
        Assert.Contains("5.0 miles", ex.Message);
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenReportingTooFrequently()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report1 = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        var report2 = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA"
        };

        // Act
        await service.AddLocationReportAsync(report1);
        
        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report2));
        Assert.Contains("5 minutes", ex.Message);
    }

    [Fact]
    public async Task AddLocationReport_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new LocationReport
        {
            Latitude = 40.0,
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA",
            Message = "Check out my site: https://spam.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report));
        Assert.Contains("Links are not allowed", ex.Message);
    }

    [Fact]
    public async Task GetRecentReports_ShouldBeLimitedBySettings()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            // Report from 12 hours ago
            context.LocationReports.Add(new LocationReport 
            { 
                Latitude = 40.0, Longitude = -74.0, 
                Timestamp = DateTime.UtcNow.AddHours(-12) 
            });
            // Report from 2 hours ago
            context.LocationReports.Add(new LocationReport 
            { 
                Latitude = 41.0, Longitude = -75.0, 
                Timestamp = DateTime.UtcNow.AddHours(-2) 
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
            DonationsEnabled = false
        };
        await settingsService.UpdateSettingsAsync(customSettings);

        // Act
        var retrieved = await settingsService.GetSettingsAsync();

        // Assert
        Assert.False(retrieved.DonationsEnabled);
        
        using (var context = new ApplicationDbContext(options))
        {
            // Report from 12 hours ago - should now be included
            context.LocationReports.Add(new LocationReport 
            { 
                Latitude = 40.0, Longitude = -74.0, 
                Timestamp = DateTime.UtcNow.AddHours(-12) 
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync();

        // Assert
        Assert.Single(results);
        
        // Check cooldown
        var report = new LocationReport { ReporterIdentifier = "UserB", Latitude = 40, Longitude = -74, ReporterLatitude = 40, ReporterLongitude = -74 };
        await service.AddLocationReportAsync(report);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(report));
        Assert.Contains("10 minutes", ex.Message);

        // Check distance
        var farReport = new LocationReport { 
            ReporterIdentifier = "UserC", 
            Latitude = 40, Longitude = -74, 
            ReporterLatitude = 41.0, ReporterLongitude = -74, // ~111km away
            Timestamp = DateTime.UtcNow 
        };
        var exDist = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLocationReportAsync(farReport));
        Assert.Contains("50.0 miles", exDist.Message);
    }
}
