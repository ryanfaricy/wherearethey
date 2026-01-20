using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
// ReSharper disable MethodHasAsyncOverload

namespace WhereAreThey.Tests.Services;

public class MapStateServiceTests : IDisposable
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<IMapService> _mapServiceMock;
    private readonly MapStateService _service;

    public MapStateServiceTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _alertServiceMock = new Mock<IAlertService>();
        _eventServiceMock = new Mock<IEventService>();
        _mapServiceMock = new Mock<IMapService>();
        var settingsServiceMock = new Mock<ISettingsService>();

        _alertServiceMock.Setup(s => s.GetAllAlertsAsync()).ReturnsAsync([]);
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([]);
        _reportServiceMock.Setup(s => s.GetAllReportsAsync()).ReturnsAsync([]);
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>())).ReturnsAsync([]);
        settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings { ReportExpiryHours = 24 });
        
        _service = new MapStateService(
            _reportServiceMock.Object,
            _alertServiceMock.Object,
            _eventServiceMock.Object,
            _mapServiceMock.Object,
            settingsServiceMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_LoadsInitialAlerts()
    {
        // Arrange
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 10, Longitude = 10, DeletedAt = null, UserIdentifier = "test-user" } };
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(alerts);

        // Act
        await _service.InitializeAsync("test-user");

        // Assert
        Assert.Single(_service.Alerts);
    }

    [Fact]
    public async Task InitializeAsync_AdminMode_LoadsAlerts()
    {
        // Arrange
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 10, Longitude = 10, DeletedAt = null, UserIdentifier = "test-admin" } };
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync("test-admin", It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(alerts);

        // Act
        await _service.InitializeAsync("test-admin", isAdmin: true);

        // Assert
        Assert.Single(_service.Alerts);
    }

    [Fact]
    public async Task LoadReportsAsync_AdminMode_ShowDeleted_ReturnsAllReports()
    {
        // Arrange
        var reports = new List<Report> 
        { 
            new() { Id = 1, DeletedAt = null, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, DeletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
        };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), true)).ReturnsAsync(reports);
        await _service.InitializeAsync("test-admin", isAdmin: true);
        _service.ShowDeleted = true;

        // Act
        await _service.LoadReportsAsync();

        // Assert
        Assert.Equal(2, _service.Reports.Count);
    }

    [Fact]
    public async Task HandleAlertAdded_AdminMode_ShowDeleted_AddedToList()
    {
        // Arrange
        var alert = new Alert { Id = 1, Latitude = 0, Longitude = 0, DeletedAt = DateTime.UtcNow, UserIdentifier = "test-admin" };
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync("test-admin", It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([]);
        await _service.InitializeAsync("test-admin", isAdmin: true);
        _service.ShowDeleted = true;
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, alert, EntityChangeType.Added);

        // Assert
        Assert.Single(_service.Alerts);
        _mapServiceMock.Verify(m => m.UpdateAlertsAsync(It.IsAny<List<Alert>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadReportsAsync_LoadsReportsAndUpdatesMap()
    {
        // Arrange
        var reports = new List<Report> { new() { Id = 1, Latitude = 10, Longitude = 10, CreatedAt = DateTime.UtcNow } };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>())).ReturnsAsync(reports);
        _service.MapInitialized = true;

        // Act
        await _service.LoadReportsAsync();

        // Assert
        Assert.Single(_service.Reports);
        _mapServiceMock.Verify(m => m.UpdateHeatMapAsync(reports, true), Times.Once);
    }

    [Fact]
    public void FindNearbyReports_ReturnsFilteredList()
    {
        // Arrange
        var report1 = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        var report2 = new Report { Id = 2, Latitude = 10, Longitude = 10, CreatedAt = DateTime.UtcNow };
        _service.Reports.Add(report1);
        _service.Reports.Add(report2);

        // Act
        // Distance between (0,0) and (0.0001, 0.0001) is very small, well within 1.0 km
        var result = _service.FindNearbyReports(0.0001, 0.0001, 1.0); 

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void HandleReportAdded_TriggeredByEvent_AddsToListAndNotifiesMap()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Added);

        // Assert
        Assert.Contains(report, _service.Reports);
        _mapServiceMock.Verify(m => m.AddSingleReportAsync(report), Times.Once);
    }

    [Fact]
    public async Task HandleReportDeleted_TriggeredByEvent_AlwaysRemovesFromList()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>()))
            .ReturnsAsync([report]);
        
        await _service.InitializeAsync("test-admin", isAdmin: true);
        _service.ShowDeleted = true; // This will trigger LoadReportsAsync and populate Reports with our report
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Deleted);

        // Assert
        Assert.Empty(_service.Reports);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleReportUpdated_SoftDeleted_ShowDeletedOn_KeptInList()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _service.Reports.Add(report);
        _service.MapInitialized = true;
        await _service.InitializeAsync("test-admin", isAdmin: true);
        _service.ShowDeleted = true;

        // Act - Soft delete sets DeletedAt
        report.DeletedAt = DateTime.UtcNow;
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Updated);

        // Assert - Should be kept in list because ShowDeleted is ON
        Assert.Single(_service.Reports);
        Assert.NotNull(_service.Reports[0].DeletedAt);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Never);
    }

    [Fact]
    public async Task HandleReportUpdated_SoftDeleted_ShowDeletedOff_RemovedFromList()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _service.Reports.Add(report);
        _service.MapInitialized = true;
        await _service.InitializeAsync("test-user", isAdmin: false);
        _service.ShowDeleted = false;

        // Act - Soft delete sets DeletedAt
        report.DeletedAt = DateTime.UtcNow;
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Updated);

        // Assert - Should be removed because ShowDeleted is OFF
        Assert.Empty(_service.Reports);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleAlertAdded_AdminMode_AddedToList()
    {
        // Arrange
        var alert = new Alert { Id = 1, Latitude = 0, Longitude = 0, DeletedAt = null, UserIdentifier = "test-admin" };
        await _service.InitializeAsync("test-admin", isAdmin: true);
        _service.MapInitialized = true;

        // Act
        // ReSharper disable once MethodHasAsyncOverload
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, alert, EntityChangeType.Added);

        // Assert
        Assert.Single(_service.Alerts);
        _mapServiceMock.Verify(m => m.UpdateAlertsAsync(It.IsAny<List<Alert>>()), Times.Once);
    }

    [Fact]
    public async Task LoadReportsAsync_RespectsIsAdminFlag()
    {
        // Setup mocks for alerts which are called during InitializeAsync
        _alertServiceMock.Setup(s => s.GetAllAlertsAsync()).ReturnsAsync([]);
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync([]);
        
        // Setup report mocks
        _reportServiceMock.Setup(s => s.GetAllReportsAsync()).ReturnsAsync([]);
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>())).ReturnsAsync([]);
        
        // 1. Test Admin Mode
        await _service.InitializeAsync("test-admin", isAdmin: true);
        await _service.LoadAllReportsAsync();
        // For admin using LoadAllReportsAsync, it calls GetAllReportsAsync
        _reportServiceMock.Verify(s => s.GetAllReportsAsync(), Times.Once);

        // 2. Switch to User Mode
        await _service.InitializeAsync("test-user", isAdmin: false);
        await _service.LoadReportsAsync(6);
        _reportServiceMock.Verify(s => s.GetRecentReportsAsync(6, false), Times.Once);
    }

    [Fact]
    public async Task LoadReportsAsync_AdminMode_FiltersDeletedReports()
    {
        // Arrange
        var reports = new List<Report> 
        { 
            new() { Id = 1, DeletedAt = null, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, DeletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
        };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), false)).ReturnsAsync(reports);
        await _service.InitializeAsync("test-admin", isAdmin: true);

        // Act
        await _service.LoadReportsAsync();

        // Assert
        Assert.Single(_service.Reports);
        Assert.Equal(1, _service.Reports[0].Id);
    }

    [Fact]
    public async Task HandleReportUpdated_ReportBecomesExpired_RemovedFromList()
    {
        // Arrange
        // Current report is recent
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>()))
            .ReturnsAsync([report]);
            
        await _service.LoadReportsAsync(24); // Set window to 24h
        _service.MapInitialized = true;

        // Act - update report to be 48h old
        report.CreatedAt = DateTime.UtcNow.AddHours(-48);
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Updated);

        // Assert
        Assert.Empty(_service.Reports);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleReportAdded_ExpiredReport_Ignored()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow.AddHours(-48) };
        await _service.LoadReportsAsync(24); // Set window to 24h
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Added);

        // Assert
        Assert.Empty(_service.Reports);
        _mapServiceMock.Verify(m => m.AddSingleReportAsync(It.IsAny<Report>()), Times.Never);
    }

    [Fact]
    public async Task HandleReportUpdated_AdminMode_AllLoaded_DoesNotExpire()
    {
        // Arrange
        var report = new Report { Id = 1, Latitude = 0, Longitude = 0, CreatedAt = DateTime.UtcNow };
        _service.Reports.Add(report);
        await _service.InitializeAsync("test-admin", isAdmin: true);
        await _service.LoadAllReportsAsync(); // Load ALL
        _service.MapInitialized = true;

        // Act - update report to be 48h old
        report.CreatedAt = DateTime.UtcNow.AddHours(-48);
        _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Updated);

        // Assert - still there because admin is in "All" mode
        Assert.Single(_service.Reports);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_ResetShowDeleted_WhenNotAdmin()
    {
        // Arrange
        _service.ShowDeleted = true;
        await _service.InitializeAsync("test-admin", isAdmin: true);
        Assert.True(_service.ShowDeleted);

        // Act
        await _service.InitializeAsync("test-user", isAdmin: false);

        // Assert
        Assert.False(_service.ShowDeleted);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _service.Dispose();
    }
    [Fact]
    public void AddReportToState_AddsReport_EvenIfOld()
    {
        // Arrange
        var report = new Report { Id = 1, CreatedAt = DateTime.UtcNow.AddHours(-48), Latitude = 0, Longitude = 0 };
        _service.Reports.Clear();

        // Act
        _service.AddReportToState(report);

        // Assert
        Assert.Single(_service.Reports);
        Assert.Equal(1, _service.Reports[0].Id);
    }

    [Fact]
    public async Task MapStateService_ShouldBeThreadSafe_WhenHandlingManyEvents()
    {
        // Arrange
        await _service.InitializeAsync("test-user", false);

        var numberOfReports = 100; // Reduced for performance in regular suite
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < numberOfReports; i++)
        {
            var report = new Report { Id = i, CreatedAt = DateTime.UtcNow, Latitude = 0, Longitude = 0 };
            tasks.Add(Task.Run(() => _eventServiceMock.Raise(e => e.OnEntityChanged += null, report, EntityChangeType.Added)));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numberOfReports, _service.Reports.Count);
    }
}
