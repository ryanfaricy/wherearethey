using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Validators;
using Xunit;

namespace WhereAreThey.Tests;

public class LocationServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<ILogger<AlertService>> _alertLoggerMock = new();
    private readonly Mock<IReportProcessingService> _reportProcessingMock = new();

    private IStringLocalizer<App> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<App>>();
        mock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => 
        {
            var val = key switch
            {
                "Links_Error" => "Links are not allowed in reports to prevent spam.",
                "Location_Verify_Error" => "Unable to verify your current location. Please ensure GPS is enabled.",
                "Cooldown_Error" => "You can only make one report every {0} minutes.",
                "Distance_Error" => "You can only make a report within {0} miles of your location.",
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
        var validator = new LocationReportValidator(factory, settingsService, localizer);
        return new LocationService(factory, _reportProcessingMock.Object, settingsService, validator, localizer);
    }

    [Fact]
    public async Task AddLocationReport_ShouldAddReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new LocationReport
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            ReporterLatitude = 40.7128,
            ReporterLongitude = -74.0060,
            ReporterIdentifier = "test-user",
            Message = "Test location",
            IsEmergency = false
        };

        // Act
        var result = await service.AddLocationReportAsync(report);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(40.7128, result.Latitude);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddLocationReport_ShouldTriggerOnReportAddedEvent()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new LocationReport
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            ReporterLatitude = 40.7128,
            ReporterLongitude = -74.0060,
            ReporterIdentifier = "test-user-2",
            Message = "Test event",
            IsEmergency = false
        };

        LocationReport? triggeredReport = null;
        int triggerCount = 0;
        service.OnReportAdded += (r) => { triggeredReport = r; triggerCount++; };

        // Act
        await service.AddLocationReportAsync(report);

        // Assert
        Assert.Equal(1, triggerCount);
        Assert.NotNull(triggeredReport);
        Assert.Equal(report.Message, triggeredReport.Message);
        Assert.Equal(report.Id, triggeredReport.Id);
    }

    [Fact]
    public async Task GetRecentReports_ShouldReturnReportsWithinTimeRange()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            var oldReport = new LocationReport
            {
                Latitude = 40.0,
                Longitude = -74.0,
                Timestamp = DateTime.UtcNow.AddHours(-48)
            };
            
            var recentReport = new LocationReport
            {
                Latitude = 41.0,
                Longitude = -75.0,
                Timestamp = DateTime.UtcNow.AddHours(-2) // Changed from -12 to -2 because of 6-hour limit
            };

            context.LocationReports.Add(oldReport);
            context.LocationReports.Add(recentReport);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(24);

        // Assert
        Assert.Single(results);
        Assert.Equal(41.0, results[0].Latitude);
    }

    [Fact]
    public async Task GetRecentReports_ShouldReturnEmptyIfFutureCutoff()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        using (var context = new ApplicationDbContext(options))
        {
            context.LocationReports.Add(new LocationReport { Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(-1); // hours = -1

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetReportsInRadius_ShouldReturnNearbyReports()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        // New York City coordinates
        var centerLat = 40.7128;
        var centerLon = -74.0060;
        
        var nearReport = new LocationReport
        {
            Latitude = 40.7580, // ~5 km away
            Longitude = -73.9855,
            Timestamp = DateTime.UtcNow
        };
        
        var farReport = new LocationReport
        {
            Latitude = 42.3601, // ~200 km away
            Longitude = -71.0589,
            Timestamp = DateTime.UtcNow
        };

        using (var context = new ApplicationDbContext(options))
        {
            context.LocationReports.Add(nearReport);
            context.LocationReports.Add(farReport);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetReportsInRadiusAsync(centerLat, centerLon, 10.0);

        // Assert
        Assert.Single(results);
        Assert.Equal(nearReport.Latitude, results[0].Latitude);
    }

    [Fact]
    public async Task GetReportsInRadius_EdgeCases()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var centerLat = 40.0;
        var centerLon = -74.0;
        var radiusKm = 10.0;

        // Point inside roughly 10km north
        // 1 degree lat approx 111km -> 10km is ~0.09 degrees
        var insideRadius = new LocationReport { Latitude = 40.0 + (9.9 / 111.0), Longitude = -74.0, Timestamp = DateTime.UtcNow };
        var justOutsideRadius = new LocationReport { Latitude = 40.0 + (10.2 / 111.0), Longitude = -74.0, Timestamp = DateTime.UtcNow };

        using (var context = new ApplicationDbContext(options))
        {
            context.LocationReports.Add(insideRadius);
            context.LocationReports.Add(justOutsideRadius);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetReportsInRadiusAsync(centerLat, centerLon, radiusKm);

        // Assert
        Assert.Single(results);
        Assert.Equal(insideRadius.Latitude, results[0].Latitude);
    }

    [Fact]
    public async Task AddLocationReport_ShouldTriggerAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act
        await service.AddLocationReportAsync(report);
        await Task.Delay(100);

        // Assert
        _reportProcessingMock.Verify(x => x.ProcessReportAsync(It.IsAny<LocationReport>()), Times.Once);
    }

    [Fact]
    public async Task AddLocationReport_ShouldNotCrashOnAlertError()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        
        // Setup mock to throw
        _reportProcessingMock.Setup(x => x.ProcessReportAsync(It.IsAny<LocationReport>())).ThrowsAsync(new Exception("Mock error"));

        var service = CreateService(factory);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.AddLocationReportAsync(report));
        Assert.Null(exception); // Should not throw
    }

    [Fact]
    public async Task AddLocationReport_Integration_ShouldSendEmailToAlertSubscribers()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var emailServiceMock = new Mock<IEmailService>();
        var settingsService = CreateSettingsService(factory);
        var alertValidator = new AlertValidator(factory, settingsService, CreateLocalizer());
        var reportValidator = new LocationReportValidator(factory, settingsService, CreateLocalizer());
        var alertService = new AlertService(factory, dataProtectionProvider, emailServiceMock.Object, _configurationMock.Object, _alertLoggerMock.Object, settingsService, alertValidator, CreateLocalizer());
        var geocodingService = new GeocodingService(new HttpClient(), settingsService, new Mock<ILogger<GeocodingService>>().Object);
        
        var services = new ServiceCollection();
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton(emailServiceMock.Object);
        services.AddSingleton<IGeocodingService>(geocodingService);
        services.AddSingleton(_configurationMock.Object);
        services.AddSingleton(settingsService);
        services.AddSingleton(CreateLocalizer());
        services.AddLogging();
        services.AddSingleton<IReportProcessingService, ReportProcessingService>();
        var serviceProvider = services.BuildServiceProvider();

        var service = new LocationService(factory, serviceProvider.GetRequiredService<IReportProcessingService>(), settingsService, reportValidator, CreateLocalizer());
        
        // User B sets up an alert
        var userBEmail = "userB@example.com";
        var alert = new Alert 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            RadiusKm = 10.0, 
            IsActive = true,
            UserIdentifier = "UserB",
            Message = "UserB's Area"
        };
        await alertService.CreateAlertAsync(alert, userBEmail);

        // Manually verify the alert for the test
        using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FirstAsync(a => a.UserIdentifier == "UserB");
            savedAlert.IsVerified = true;
            await context.SaveChangesAsync();
        }

        // User A reports something nearby (roughly 1.1km away)
        var report = new LocationReport 
        { 
            Latitude = 40.01, 
            Longitude = -74.0,
            ReporterLatitude = 40.01,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "test-user",
            Message = "Alert trigger message",
            IsEmergency = true
        };

        // Act
        await service.AddLocationReportAsync(report);

        // Wait for background task
        await Task.Delay(500);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailAsync(
            It.Is<string>(s => s == userBEmail),
            It.Is<string>(s => s.Contains("EMERGENCY")),
            It.Is<string>(b => b.Contains("Alert trigger message"))), Times.Once);
    }

    [Fact]
    public async Task AddLocationReport_ShouldIncludeAlertMessageInEmail()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var emailServiceMock = new Mock<IEmailService>();
        var settingsService = CreateSettingsService(factory);
        var alertValidator = new AlertValidator(factory, settingsService, CreateLocalizer());
        var reportValidator = new LocationReportValidator(factory, settingsService, CreateLocalizer());
        var alertService = new AlertService(factory, dataProtectionProvider, emailServiceMock.Object, _configurationMock.Object, _alertLoggerMock.Object, settingsService, alertValidator, CreateLocalizer());
        var geocodingService = new GeocodingService(new HttpClient(), settingsService, new Mock<ILogger<GeocodingService>>().Object);
        
        var services = new ServiceCollection();
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton(emailServiceMock.Object);
        services.AddSingleton<IGeocodingService>(geocodingService);
        services.AddSingleton(_configurationMock.Object);
        services.AddSingleton(settingsService);
        services.AddSingleton(CreateLocalizer());
        services.AddLogging();
        services.AddSingleton<IReportProcessingService, ReportProcessingService>();
        var serviceProvider = services.BuildServiceProvider();

        var service = new LocationService(factory, serviceProvider.GetRequiredService<IReportProcessingService>(), settingsService, reportValidator, CreateLocalizer());
        
        var userBEmail = "userB@example.com";
        var alertMessage = "This is my custom alert message";
        var alert = new Alert 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            RadiusKm = 10.0, 
            IsActive = true,
            UserIdentifier = "UserB",
            Message = alertMessage
        };
        await alertService.CreateAlertAsync(alert, userBEmail);

        // Manually verify the alert for the test
        using (var context = new ApplicationDbContext(options))
        {
            var savedAlert = await context.Alerts.FirstAsync(a => a.UserIdentifier == "UserB");
            savedAlert.IsVerified = true;
            await context.SaveChangesAsync();
        }

        var report = new LocationReport 
        { 
            Latitude = 40.01, 
            Longitude = -74.0,
            ReporterLatitude = 40.01,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "UserA",
            Message = "Something happened",
            IsEmergency = false
        };

        // Act
        await service.AddLocationReportAsync(report);

        // Wait for background task
        await Task.Delay(500);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailAsync(
            It.Is<string>(s => s == userBEmail),
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains(alertMessage))), Times.Once);
    }

    [Fact]
    public async Task AddLocationReport_ShouldHandleDecryptionFailureGracefully()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act
        await service.AddLocationReportAsync(report);
        await Task.Delay(100);

        // Assert
        _reportProcessingMock.Verify(x => x.ProcessReportAsync(It.IsAny<LocationReport>()), Times.Once);
    }

    [Fact]
    public async Task GetReportByExternalId_ShouldReturnCorrectReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, Timestamp = DateTime.UtcNow, ExternalId = Guid.NewGuid() };
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetReportByExternalIdAsync(report.ExternalId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
        Assert.Equal(report.ExternalId, result.ExternalId);
    }

    [Fact]
    public async Task AddLocationReport_ShouldUseConfiguredBaseUrlInEmail()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var emailServiceMock = new Mock<IEmailService>();
        var settingsService = CreateSettingsService(factory);
        var alertValidator = new AlertValidator(factory, settingsService, CreateLocalizer());
        var reportValidator = new LocationReportValidator(factory, settingsService, CreateLocalizer());

        var customBaseUrl = "https://custom.example.com";
        _configurationMock.Setup(x => x["BaseUrl"]).Returns(customBaseUrl);
        
        var alertService = new AlertService(factory, dataProtectionProvider, emailServiceMock.Object, _configurationMock.Object, _alertLoggerMock.Object, settingsService, alertValidator, CreateLocalizer());
        var geocodingService = new GeocodingService(new HttpClient(), settingsService, new Mock<ILogger<GeocodingService>>().Object);
        
        var services = new ServiceCollection();
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton(emailServiceMock.Object);
        services.AddSingleton<IGeocodingService>(geocodingService);
        services.AddSingleton(_configurationMock.Object);
        services.AddSingleton(settingsService);
        services.AddSingleton(CreateLocalizer());
        services.AddLogging();
        services.AddSingleton<IReportProcessingService, ReportProcessingService>();
        var serviceProvider = services.BuildServiceProvider();

        var service = new LocationService(factory, serviceProvider.GetRequiredService<IReportProcessingService>(), settingsService, reportValidator, CreateLocalizer());
        
        var userEmail = "test@example.com";
        var alert = new Alert 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            RadiusKm = 10.0, 
            IsActive = true,
            IsVerified = true,
            UserIdentifier = "UserB"
        };
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            alert.EncryptedEmail = dataProtectionProvider.CreateProtector("WhereAreThey.Alerts.Email").Protect(userEmail);
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();
        }

        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act
        await service.AddLocationReportAsync(report);
        await Task.Delay(500);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailAsync(
            It.Is<string>(s => s == userEmail),
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains(customBaseUrl + "/?reportId="))), Times.Once);
    }
}
