using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class MapNavigationManagerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly MockNavigationManager _navManager;
    private readonly MapNavigationManager _service;

    public MapNavigationManagerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _alertServiceMock = new Mock<IAlertService>();
        _navManager = new MockNavigationManager();
        _service = new MapNavigationManager(_navManager, _reportServiceMock.Object, _alertServiceMock.Object, NullLogger<MapNavigationManager>.Instance);
    }

    private class MockNavigationManager : NavigationManager
    {
        public MockNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        public void SetUri(string uri)
        {
            Uri = uri;
        }

        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    [Fact]
    public async Task GetNavigationStateAsync_WithHours_ReturnsSelectedHours()
    {
        // Arrange
        _navManager.SetUri("http://localhost/?hours=12");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Equal(12, result.SelectedHours);
    }

    [Fact]
    public async Task GetNavigationStateAsync_WithReportGuid_FetchesAndReturnsReportInfo()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var report = new Report { Id = 123, Latitude = 1.2, Longitude = 3.4 };
        _reportServiceMock.Setup(s => s.GetReportByExternalIdAsync(guid))
            .ReturnsAsync(Result<Report>.Success(report));
        _navManager.SetUri($"http://localhost/?reportId={guid}");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Equal(123, result.FocusReportId);
        Assert.Equal(1.2, result.InitialLat);
        Assert.Equal(3.4, result.InitialLng);
    }

    [Fact]
    public async Task GetNavigationStateAsync_WithReportIntId_ReturnsFocusReportId()
    {
        // Arrange
        _navManager.SetUri("http://localhost/?reportId=456");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Equal(456, result.FocusReportId);
    }

    [Fact]
    public async Task GetNavigationStateAsync_WithAlertGuid_FetchesAndReturnsAlertInfo()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var alert = new Alert { Id = 789, Latitude = 5.6, Longitude = 7.8, RadiusKm = 2.5 };
        _alertServiceMock.Setup(s => s.GetAlertByExternalIdAsync(guid))
            .ReturnsAsync(Result<Alert>.Success(alert));
        _navManager.SetUri($"http://localhost/?alertId={guid}");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Equal(5.6, result.InitialLat);
        Assert.Equal(7.8, result.InitialLng);
        Assert.Equal(2.5, result.InitialRadius);
    }

    [Fact]
    public async Task GetNavigationStateAsync_WithLatLong_ReturnsLatLong()
    {
        // Arrange
        _navManager.SetUri("http://localhost/?lat=10.5&lng=20.5&radius=5.5");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Equal(10.5, result.InitialLat);
        Assert.Equal(20.5, result.InitialLng);
        Assert.Equal(5.5, result.InitialRadius);
    }

    [Fact]
    public async Task GetNavigationStateAsync_EmptyQuery_ReturnsDefaultState()
    {
        // Arrange
        _navManager.SetUri("http://localhost/");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Null(result.SelectedHours);
        Assert.Null(result.FocusReportId);
        Assert.Null(result.InitialLat);
        Assert.Null(result.InitialLng);
        Assert.Null(result.InitialRadius);
    }
    [Fact]
    public async Task GetNavigationStateAsync_WithMissingReportGuid_SetsReportNotFound()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _reportServiceMock.Setup(s => s.GetReportByExternalIdAsync(guid))
            .ReturnsAsync(Result<Report>.Failure("Not found"));
        _navManager.SetUri($"http://localhost/?reportId={guid}");

        // Act
        var result = await _service.GetNavigationStateAsync();

        // Assert
        Assert.Null(result.FocusReportId);
        Assert.True(result.ReportNotFound);
    }
}
