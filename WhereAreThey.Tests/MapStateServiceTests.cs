using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests;

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
        
        _service = new MapStateService(
            _reportServiceMock.Object,
            _alertServiceMock.Object,
            _eventServiceMock.Object,
            _mapServiceMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_LoadsInitialAlerts()
    {
        // Arrange
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 10, Longitude = 10, IsActive = true } };
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(alerts);

        // Act
        await _service.InitializeAsync("test-user");

        // Assert
        Assert.Single(_service.Alerts);
    }

    [Fact]
    public async Task InitializeAsync_AdminMode_DoesNotLoadAlerts()
    {
        // Arrange
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 10, Longitude = 10, IsActive = true } };
        // Even if GetAllAlertsAdminAsync would return alerts
        _alertServiceMock.Setup(s => s.GetAllAlertsAdminAsync()).ReturnsAsync(alerts);

        // Act
        await _service.InitializeAsync("test-admin", isAdmin: true);

        // Assert
        Assert.Empty(_service.Alerts);
        _mapServiceMock.Verify(m => m.UpdateAlertsAsync(It.IsAny<List<Alert>>()), Times.Never);
    }

    [Fact]
    public async Task LoadReportsAsync_LoadsReportsAndUpdatesMap()
    {
        // Arrange
        var reports = new List<LocationReport> { new() { Id = 1, Latitude = 10, Longitude = 10 } };
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>())).ReturnsAsync(reports);
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
        var report1 = new LocationReport { Id = 1, Latitude = 0, Longitude = 0 };
        var report2 = new LocationReport { Id = 2, Latitude = 10, Longitude = 10 };
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
        var report = new LocationReport { Id = 1, Latitude = 0, Longitude = 0 };
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnReportAdded += null, report);

        // Assert
        Assert.Contains(report, _service.Reports);
        _mapServiceMock.Verify(m => m.AddSingleReportAsync(report), Times.Once);
    }

    [Fact]
    public void HandleReportDeleted_TriggeredByEvent_RemovesFromListAndNotifiesMap()
    {
        // Arrange
        var report = new LocationReport { Id = 1, Latitude = 0, Longitude = 0 };
        _service.Reports.Add(report);
        _service.MapInitialized = true;

        // Act
        _eventServiceMock.Raise(e => e.OnReportDeleted += null, 1);

        // Assert
        Assert.Empty(_service.Reports);
        _mapServiceMock.Verify(m => m.RemoveSingleReportAsync(1), Times.Once);
    }

    [Fact]
    public async Task HandleAlertAdded_AdminMode_Ignored()
    {
        // Arrange
        await _service.InitializeAsync("test-admin", isAdmin: true);
        var alert = new Alert { Id = 1, Latitude = 0, Longitude = 0, IsActive = true };
        _service.MapInitialized = true;

        // Act
        // ReSharper disable once MethodHasAsyncOverload
        _eventServiceMock.Raise(e => e.OnAlertAdded += null, alert);

        // Assert
        Assert.Empty(_service.Alerts);
        _mapServiceMock.Verify(m => m.UpdateAlertsAsync(It.IsAny<List<Alert>>()), Times.Never);
    }

    [Fact]
    public async Task LoadReportsAsync_RespectsIsAdminFlag()
    {
        // Setup mocks for alerts which are called during InitializeAsync
        _alertServiceMock.Setup(s => s.GetAllAlertsAdminAsync()).ReturnsAsync([]);
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync([]);

        // 1. Test Admin Mode
        await _service.InitializeAsync("test-admin", isAdmin: true);
        await _service.LoadReportsAsync();
        _reportServiceMock.Verify(s => s.GetAllReportsAsync(), Times.Once);

        // 2. Switch to User Mode
        await _service.InitializeAsync("test-user", isAdmin: false);
        await _service.LoadReportsAsync(6);
        _reportServiceMock.Verify(s => s.GetRecentReportsAsync(6), Times.Once);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _service.Dispose();
    }
}
