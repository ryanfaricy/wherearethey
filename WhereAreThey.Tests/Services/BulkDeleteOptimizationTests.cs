using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire;
using FluentValidation;
using Microsoft.Data.Sqlite;

namespace WhereAreThey.Tests.Services;

public class BulkDeleteOptimizationTests : IDisposable
{
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock = new();
    private readonly Mock<IValidator<Report>> _validatorMock = new();
    private readonly Mock<ILogger<ReportService>> _loggerMock = new();
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public BulkDeleteOptimizationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = new ApplicationDbContext(_options))
        {
            context.Database.EnsureCreated();
        }

        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(_options));
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SoftDeleteRangeAsync_TriggersBatchEvent_AndNotIndividualEvents()
    {
        // Arrange
        var service = new ReportService(
            _contextFactoryMock.Object,
            _backgroundJobClientMock.Object,
            _settingsServiceMock.Object,
            _eventServiceMock.Object,
            _baseUrlProviderMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Seed some data
        await using (var context = await _contextFactoryMock.Object.CreateDbContextAsync())
        {
            context.Reports.Add(new Report { Id = 1, CreatedAt = DateTime.UtcNow });
            context.Reports.Add(new Report { Id = 2, CreatedAt = DateTime.UtcNow });
            context.Reports.Add(new Report { Id = 3, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var idsToDelete = new List<int> { 1, 2, 3 };

        // Act
        await service.DeleteReportsAsync(idsToDelete, hardDelete: false);

        // Assert
        _eventServiceMock.Verify(e => e.NotifyEntityBatchChanged(typeof(Report)), Times.Once);
        _eventServiceMock.Verify(e => e.NotifyEntityChanged(It.IsAny<Report>(), It.IsAny<EntityChangeType>()), Times.Never);
    }

    [Fact]
    public async Task HardDeleteRangeAsync_TriggersBatchEvent_AndNotIndividualEvents()
    {
        // Arrange
        var service = new ReportService(
            _contextFactoryMock.Object,
            _backgroundJobClientMock.Object,
            _settingsServiceMock.Object,
            _eventServiceMock.Object,
            _baseUrlProviderMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Seed some data
        await using (var context = await _contextFactoryMock.Object.CreateDbContextAsync())
        {
            context.Reports.Add(new Report { Id = 4, CreatedAt = DateTime.UtcNow });
            context.Reports.Add(new Report { Id = 5, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var idsToDelete = new List<int> { 4, 5 };

        // Act
        var result = await service.DeleteReportsAsync(idsToDelete, hardDelete: true);

        // Assert
        Assert.True(result.IsSuccess, result.Error);
        _eventServiceMock.Verify(e => e.NotifyEntityBatchChanged(typeof(Report)), Times.Once);
        _eventServiceMock.Verify(e => e.NotifyEntityChanged(It.IsAny<Report>(), It.IsAny<EntityChangeType>()), Times.Never);
    }

    [Fact]
    public async Task MapStateService_Reloads_OnBatchEvent()
    {
        // Arrange
        var reportServiceMock = new Mock<IReportService>();
        var alertServiceMock = new Mock<IAlertService>();
        var eventService = new EventService(); // Use real event service
        var mapServiceMock = new Mock<IMapService>();
        var settingsServiceMock = new Mock<ISettingsService>();

        settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportExpiryHours = 24 });
        reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Report>());

        using var mapStateService = new MapStateService(
            reportServiceMock.Object,
            alertServiceMock.Object,
            eventService,
            mapServiceMock.Object,
            settingsServiceMock.Object);

        await mapStateService.InitializeAsync("test-user");

        // Clear previous calls
        reportServiceMock.Invocations.Clear();

        // Act
        eventService.NotifyEntityBatchChanged(typeof(Report));
        
        // Wait a bit as it's fire-and-forget in MapStateService (_ = LoadReportsAsync())
        await Task.Delay(100);

        // Assert
        reportServiceMock.Verify(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<Guid?>()), Times.Once);
    }
}
