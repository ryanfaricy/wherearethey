using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
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

namespace WhereAreThey.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<ILogger<ReportService>> _loggerMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock = new();
    private readonly Mock<IAdminService> _adminServiceMock = new();
    private SqliteConnection? _connection;

    public ReportServiceTests()
    {
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);
        _baseUrlProviderMock.Setup(x => x.GetBaseUrl()).Returns("https://test.com");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _connection?.Dispose();
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

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        using (var context = new ApplicationDbContext(options))
        {
            context.Database.EnsureCreated();
        }

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
        return new ReportService(factory, _backgroundJobClientMock.Object, settingsService, _eventServiceMock.Object, _baseUrlProviderMock.Object, validator, _loggerMock.Object);
    }

    [Fact]
    public async Task AddReport_ShouldAddReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report
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
        var result = await service.CreateReportAsync(report);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value!.Id);
        Assert.Equal(40.7128, result.Value!.Latitude);
        Assert.True(result.Value!.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddReport_ShouldTriggerAdminNotification()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report
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
        await service.CreateReportAsync(report);

        // Assert
        _eventServiceMock.Verify(x => x.NotifyEntityChanged(It.Is<Report>(r => r.Message == report.Message), EntityChangeType.Added), Times.Once);
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
            var oldReport = new Report
            {
                Latitude = 40.0,
                Longitude = -74.0,
                CreatedAt = DateTime.UtcNow.AddHours(-48),
            };
            
            var recentReport = new Report
            {
                Latitude = 41.0,
                Longitude = -75.0,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            };

            context.Reports.Add(oldReport);
            context.Reports.Add(recentReport);
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
            context.Reports.Add(new Report { CreatedAt = DateTime.UtcNow });
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
        var report = new Report { Latitude = 40.0, Longitude = -74.0, ReporterLatitude = 40.0, ReporterLongitude = -74.0, ReporterIdentifier = "test-user" };

        // Act
        await service.CreateReportAsync(report);

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
        var reportValidator = new ReportValidator(factory, settingsService, _adminServiceMock.Object, CreateLocalizer());
        var appOptions = Options.Create(new AppOptions());
        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        
        var services = new ServiceCollection();
        services.AddSingleton(emailServiceMock.Object);
        services.AddSingleton(emailTemplateServiceMock.Object);
        services.AddSingleton(appOptions);
        services.AddSingleton(_baseUrlProviderMock.Object);
        services.AddSingleton(settingsService);
        services.AddSingleton(CreateLocalizer());
        services.AddSingleton(backgroundJobClientMock.Object);
        services.AddLogging();
        services.AddSingleton<IGeocodingService>(new GeocodingService(new HttpClient(), settingsService, new Mock<ILogger<GeocodingService>>().Object));
        services.AddSingleton<ILocationService>(new LocationService(factory, settingsService, new Mock<ILogger<LocationService>>().Object));
        services.AddSingleton(new Mock<IWebPushService>().Object);
        services.AddScoped<IReportProcessingService, ReportProcessingService>();
        
        // Circular dependency handling: AlertService needs IBackgroundJobClient
        var alertServiceInstance = new AlertService(
            factory, 
            dataProtectionProvider, 
            emailServiceMock.Object, 
            backgroundJobClientMock.Object, 
            _eventServiceMock.Object, 
            _baseUrlProviderMock.Object,
            emailTemplateServiceMock.Object,
            new Mock<ILogger<AlertService>>().Object, 
            alertValidator);

        services.AddSingleton<IAlertService>(alertServiceInstance);
        
        var serviceProvider = services.BuildServiceProvider();

        // Setup background job client to execute synchronously for the test
        backgroundJobClientMock.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) =>
            {
                var instance = serviceProvider.GetRequiredService(job.Type);
                var task = (Task)job.Method.Invoke(instance, job.Args.ToArray())!;
                task.GetAwaiter().GetResult();
            });

        var service = new ReportService(factory, backgroundJobClientMock.Object, settingsService, _eventServiceMock.Object, _baseUrlProviderMock.Object, reportValidator, new Mock<ILogger<ReportService>>().Object);
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
            UsePush = false,
            UseEmail = true,
        };
        await alertService.CreateAlertAsync(alert, userBEmail);

        // Manually verify the email for User B
        await using (var context = new ApplicationDbContext(options))
        {
            var emailHash = HashUtils.ComputeHash(userBEmail);
            var verification = await context.EmailVerifications.FirstOrDefaultAsync(v => v.EmailHash == emailHash);
            if (verification != null)
            {
                verification.VerifiedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            
            // Also need to mark existing alerts as verified
            var alerts = await context.Alerts.Where(a => a.EmailHash == emailHash).ToListAsync();
            foreach (var a in alerts) a.IsVerified = true;
            await context.SaveChangesAsync();
        }

        // User A reports something nearby
        var report = new Report 
        { 
            Latitude = 40.0, 
            Longitude = -74.0,
            ReporterLatitude = 40.0,
            ReporterLongitude = -74.0,
            ReporterIdentifier = "test-user-long-id",
            Message = "Alert trigger message",
            IsEmergency = true,
        };

        // Act
        await service.CreateReportAsync(report);

        // Assert
        emailServiceMock.Verify(x => x.SendEmailsAsync(
            It.Is<IEnumerable<Email>>(emails => 
                emails.Count() == 1 &&
                emails.First().To == userBEmail &&
                emails.First().Subject.Contains("EMERGENCY") &&
                emails.First().Body.Contains("Alert trigger message"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetTopRecentReports_ShouldReturnRequestedNumber()
    {
        // Arrange
        var factory = CreateFactory(CreateOptions());
        var service = CreateService(factory);
        await using var context = await factory.CreateDbContextAsync();

        for (var i = 1; i <= 30; i++)
        {
            context.Reports.Add(new Report
            {
                Latitude = 10, Longitude = 10, CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                ReporterIdentifier = "user", ExternalId = Guid.NewGuid(),
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTopRecentReportsAsync(20);

        // Assert
        Assert.Equal(20, result.Count);
        Assert.Equal(result.OrderByDescending(r => r.CreatedAt), result);
    }

    [Fact]
    public async Task GetReportById_ShouldReturnCorrectReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report { Latitude = 40.0, Longitude = -74.0, CreatedAt = DateTime.UtcNow, ExternalId = Guid.NewGuid() };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetReportByIdAsync(report.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(report.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetReportByExternalId_ShouldReturnCorrectReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report { Latitude = 40.0, Longitude = -74.0, CreatedAt = DateTime.UtcNow, ExternalId = Guid.NewGuid() };
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
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
        var report = new Report { Latitude = 40.0, Longitude = -74.0, CreatedAt = DateTime.UtcNow };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.DeleteReportAsync(report.Id);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            var deletedReport = await context.Reports.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == report.Id);
            Assert.NotNull(deletedReport);
            Assert.NotNull(deletedReport.DeletedAt);
        }
        
        // Verify only Updated event was sent, NOT Deleted
        _eventServiceMock.Verify(e => e.NotifyEntityChanged(It.IsAny<Report>(), EntityChangeType.Updated), Times.AtLeastOnce);
        _eventServiceMock.Verify(e => e.NotifyEntityChanged(It.IsAny<Report>(), EntityChangeType.Deleted), Times.Never);
        
        var recent = await service.GetRecentReportsAsync();
        Assert.Empty(recent);
    }

    [Fact]
    public async Task DeleteReport_ShouldHardDeleteReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report { Latitude = 40.0, Longitude = -74.0, CreatedAt = DateTime.UtcNow };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.DeleteReportAsync(report.Id, hardDelete: true);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            var deletedReport = await context.Reports.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == report.Id);
            Assert.Null(deletedReport); // Should be gone
        }
    }

    [Fact]
    public async Task DeleteReport_WhenAlreadySoftDeleted_ShouldHardDelete()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report { Latitude = 40.0, Longitude = -74.0, CreatedAt = DateTime.UtcNow, DeletedAt = DateTime.UtcNow };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act - even without hardDelete: true, it should hard delete because it's already soft-deleted
        var result = await service.DeleteReportAsync(report.Id);

        // Assert
        Assert.True(result.IsSuccess);
        
        await using (var context = await factory.CreateDbContextAsync())
        {
            var deletedReport = await context.Reports.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == report.Id);
            Assert.Null(deletedReport); // Should be gone
        }
    }
    [Fact]
    public async Task GetRecentReports_WithIncludeDeletedTrue_ShouldReturnDeletedReports()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);

        await using (var context = await factory.CreateDbContextAsync())
        {
            var deletedReport = new Report
            {
                Latitude = 40.0,
                Longitude = -74.0,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                DeletedAt = DateTime.UtcNow,
                ReporterIdentifier = "test",
            };
            context.Reports.Add(deletedReport);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(hours: 24, includeDeleted: true);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task GetReportById_ShouldReturnReport_EvenIfSoftDeleted()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            CreatedAt = DateTime.UtcNow, 
            ExternalId = Guid.NewGuid(),
            DeletedAt = DateTime.UtcNow 
        };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetReportByIdAsync(report.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.DeletedAt);
    }

    [Fact]
    public async Task GetReportByExternalId_ShouldReturnReport_EvenIfSoftDeleted()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var report = new Report 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            CreatedAt = DateTime.UtcNow, 
            ExternalId = Guid.NewGuid(),
            DeletedAt = DateTime.UtcNow 
        };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(report);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetReportByExternalIdAsync(report.ExternalId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.DeletedAt);
    }

    [Fact]
    public async Task GetRecentReportsAsync_ShouldIncludeSpecificallyRequestedSoftDeletedReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var externalId = Guid.NewGuid();
        var deletedReport = new Report 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            CreatedAt = DateTime.UtcNow, 
            ExternalId = externalId,
            DeletedAt = DateTime.UtcNow 
        };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(deletedReport);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(hours: 24, includeDeleted: false, includeExternalId: externalId);

        // Assert
        Assert.Contains(results, r => r.ExternalId == externalId);
    }

    [Fact]
    public async Task GetRecentReportsAsync_ShouldIncludeSpecificallyRequestedExpiredReport()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var externalId = Guid.NewGuid();
        var expiredReport = new Report 
        { 
            Latitude = 40.0, 
            Longitude = -74.0, 
            CreatedAt = DateTime.UtcNow.AddDays(-10), 
            ExternalId = externalId
        };

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Reports.Add(expiredReport);
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.GetRecentReportsAsync(hours: 24, includeDeleted: false, includeExternalId: externalId);

        // Assert
        Assert.Contains(results, r => r.ExternalId == externalId);
    }
}
