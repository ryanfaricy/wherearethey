using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

namespace WhereAreThey.Tests;

public class ReportServiceTests
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<ILogger<ReportService>> _loggerMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IAdminService> _adminServiceMock = new();

    public ReportServiceTests()
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
                "Cooldown_Error" => "You can only make one report every {0} minutes.",
                "Distance_Error" => "You can only make a report within {0} miles of your location.",
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
        var validator = new LocationReportValidator(factory, settingsService, _adminServiceMock.Object, localizer);
        return new ReportService(factory, _backgroundJobClientMock.Object, settingsService, _eventServiceMock.Object, validator, _loggerMock.Object);
    }

    [Fact]
    public async Task AddReport_ShouldAddReport()
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
            IsEmergency = false,
        };

        // Act
        var result = await service.AddReportAsync(report);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value!.Id);
        Assert.Equal(40.7128, result.Value!.Latitude);
        Assert.True(result.Value!.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddReport_ShouldTriggerAdminNotification()
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
            IsEmergency = false,
        };

        // Act
        await service.AddReportAsync(report);

        // Assert
        _eventServiceMock.Verify(x => x.NotifyEntityChanged(It.Is<LocationReport>(r => r.Message == report.Message), EntityChangeType.Added), Times.Once);
    }

    [Fact]
    public async Task GetRecentReports_ShouldReturnReportsWithinTimeRange()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = new ApplicationDbContext(options))
        {
            var oldReport = new LocationReport
            {
                Latitude = 40.0,
                Longitude = -74.0,
                Timestamp = DateTime.UtcNow.AddHours(-48),
            };
            
            var recentReport = new LocationReport
            {
                Latitude = 41.0,
                Longitude = -75.0,
                Timestamp = DateTime.UtcNow.AddHours(-2),
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

        await using (var context = new ApplicationDbContext(options))
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
    public async Task AddReport_ShouldTriggerAlerts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act
        await service.AddReportAsync(report);

        // Assert
        _backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == nameof(IReportProcessingService.ProcessReportAsync) && job.Args[0] == report),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task AddReport_Integration_ShouldSendEmailToAlertSubscribers()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var emailServiceMock = new Mock<IEmailService>();
        var emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        emailTemplateServiceMock.Setup(t => t.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string _, object model) => 
            {
                var reportMsg = model.GetType().GetProperty("ReportMessage")?.GetValue(model)?.ToString() ?? "";
                return $"Rendered body: {reportMsg}";
            });
        var settingsService = CreateSettingsService(factory);
        var alertValidator = new AlertValidator(factory, settingsService, CreateLocalizer());
        var reportValidator = new LocationReportValidator(factory, settingsService, _adminServiceMock.Object, CreateLocalizer());
        var appOptions = Options.Create(new AppOptions());
        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        
        var services = new ServiceCollection();
        services.AddSingleton(emailServiceMock.Object);
        services.AddSingleton(emailTemplateServiceMock.Object);
        services.AddSingleton(appOptions);
        services.AddSingleton(settingsService);
        services.AddSingleton(CreateLocalizer());
        services.AddSingleton(backgroundJobClientMock.Object);
        services.AddLogging();
        services.AddSingleton<IGeocodingService>(new GeocodingService(new HttpClient(), settingsService, new Mock<ILogger<GeocodingService>>().Object));
        services.AddSingleton<ILocationService>(new LocationService(factory, settingsService, new Mock<ILogger<LocationService>>().Object));
        services.AddScoped<IReportProcessingService, ReportProcessingService>();
        
        // Circular dependency handling: AlertService needs IBackgroundJobClient
        services.AddSingleton<IAlertService>(sp => new AlertService(
            factory, 
            dataProtectionProvider, 
            sp.GetRequiredService<IEmailService>(), 
            sp.GetRequiredService<IBackgroundJobClient>(), 
            _eventServiceMock.Object, 
            sp.GetRequiredService<IOptions<AppOptions>>(), 
            sp.GetRequiredService<IEmailTemplateService>(),
            new Mock<ILogger<AlertService>>().Object, 
            alertValidator));
        
        var serviceProvider = services.BuildServiceProvider();

        // Setup background job client to execute synchronously for the test
        backgroundJobClientMock.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) =>
            {
                var instance = serviceProvider.GetRequiredService(job.Type);
                var task = (Task)job.Method.Invoke(instance, job.Args.ToArray())!;
                task.GetAwaiter().GetResult();
            });

        var service = new ReportService(factory, backgroundJobClientMock.Object, settingsService, _eventServiceMock.Object, reportValidator, new Mock<ILogger<ReportService>>().Object);
        var alertService = serviceProvider.GetRequiredService<IAlertService>();
        
        // User B sets up an alert
        var userBEmail = "userB@example.com";
        var alert = new Alert 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            RadiusKm = 10.0, 
            UserIdentifier = "UserB",
            Message = "UserB's Area",
        };
        await alertService.CreateAlertAsync(alert, userBEmail);

        // Manually verify the alert for the test
        await using (var context = new ApplicationDbContext(options))
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
            IsEmergency = true,
        };

        // Act
        await service.AddReportAsync(report);

        // Wait for background task
        await Task.Delay(1000);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailsAsync(
            It.Is<IEnumerable<Email>>(emails => 
                emails.Count() == 1 &&
                emails.First().To == userBEmail &&
                emails.First().Subject.Contains("EMERGENCY") &&
                emails.First().Body.Contains("Alert trigger message"))), Times.Once);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(report.Id, result.Value!.Id);
        Assert.Equal(report.ExternalId, result.Value!.ExternalId);
    }

    [Fact]
    public async Task DeleteReport_ShouldSoftDeleteReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new LocationReport { Latitude = 40.0, Longitude = -74.0, Timestamp = DateTime.UtcNow };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.LocationReports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.DeleteReportAsync(report.Id);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            var deletedReport = await context.LocationReports.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == report.Id);
            Assert.NotNull(deletedReport);
            Assert.NotNull(deletedReport.DeletedAt);
        }
        
        var recent = await service.GetRecentReportsAsync();
        Assert.Empty(recent);
    }
}
