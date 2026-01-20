using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Radzen;
using WhereAreThey.Components;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class MapInteractionServiceTests : BunitContext
{
    private readonly Mock<IMapService> _mapServiceMock;
    private readonly Mock<IMapStateService> _stateServiceMock;
    private readonly DialogService _dialogService;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly MapInteractionService _service;

    public MapInteractionServiceTests()
    {
        _mapServiceMock = new Mock<IMapService>();
        _mapServiceMock.Setup(m => m.GetMapStateAsync()).ReturnsAsync((MapState?)null);
        _stateServiceMock = new Mock<IMapStateService>();
        _adminServiceMock = new Mock<IAdminService>();
        var localizerMock = new Mock<IStringLocalizer<App>>();
        
        Services.AddRadzenComponents();
        _dialogService = Services.GetRequiredService<DialogService>();
        
        localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));

        _service = new MapInteractionService(
            _mapServiceMock.Object,
            _stateServiceMock.Object,
            _dialogService,
            _adminServiceMock.Object,
            NullLogger<MapInteractionService>.Instance);
    }

    [Theory]
    [InlineData(10, false, null, 10.0)]
    [InlineData(15, false, null, 1.0)]
    [InlineData(16, false, null, 1.0)]
    [InlineData(10, true, null, 0.1)]
    [InlineData(10, false, 50.0, 50.0)]
    [InlineData(10, false, 200.0, 160.0)]
    [InlineData(15, false, 2.0, 2.0)]
    [InlineData(15, true, 2.0, 0.1)] // Marker click should always be 0.1
    public void CalculateSearchRadius_ReturnsCorrectValues(double zoom, bool isMarkerClick, double? viewportRadius, double expected)
    {
        // Act
        var result = _service.CalculateSearchRadius(zoom, isMarkerClick, viewportRadius);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleMapClickAsync_ReturnsFalse_WhenNoNearbyData()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        _stateServiceMock.Setup(s => s.Alerts).Returns([]);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensReportDetails_WhenNearbyReportsFound_OnMapClick()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        _stateServiceMock.Setup(s => s.Alerts).Returns([]);
        var reports = new List<Report> { new() { Id = 1 } };
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
                Assert.Single(((List<Report>)parameters["Reports"]));
            }
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensReportDetails_WhenNearbyAlertFound()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 0, Longitude = 0, RadiusKm = 1.0 } };
        _stateServiceMock.Setup(s => s.Alerts).Returns(alerts);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(alerts);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
                Assert.Equal(1, parameters["SelectedAlertId"]);
            }
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensDetails_WhenReportMarkerClicked()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        var reports = new List<Report> { new() { Id = 1 } };
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type != typeof(ReportDetailsDialog))
            {
                return;
            }

            dialogOpened = true;
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, true, reportId: 1);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensReportDetails_WhenBothNearby()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        var alerts = new List<Alert> { new() { Id = 1, Latitude = 0, Longitude = 0, RadiusKm = 1.0 } };
        var reports = new List<Report> { new() { Id = 1, Latitude = 0, Longitude = 0 } };
        _stateServiceMock.Setup(s => s.Alerts).Returns(alerts);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(alerts);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
            }
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_HighlightsClosestReport_WhenMultipleFound()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        _stateServiceMock.Setup(s => s.Alerts).Returns([]);
        
        // Closest should be report 2 (at 0,0)
        var reports = new List<Report> 
        { 
            new() { Id = 1, Latitude = 1, Longitude = 1 },
            new() { Id = 2, Latitude = 0, Longitude = 0 },
        };
        
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        int? highlightedReportId = null;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                highlightedReportId = (int?)parameters["SelectedReportId"];
            }
            _dialogService.Close();
        };

        // Act - Click at 0,0
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.Equal(2, highlightedReportId);
    }

    [Fact]
    public async Task HandleMapContextMenuAsync_ShowsGhostPinAndOpensDialog()
    {
        // Arrange
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);
        
        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type != typeof(ReportDialog))
            {
                return;
            }

            dialogOpened = true;
            // Close the dialog immediately to avoid hanging on the 'await'
            _dialogService.Close();
        };

        // Act
        await _service.HandleMapContextMenuAsync(10, 20, "user-id");

        // Assert
        _mapServiceMock.Verify(m => m.ShowGhostPinAsync(10, 20), Times.Once);
        Assert.True(dialogOpened);
        _mapServiceMock.Verify(m => m.HideGhostPinAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleMapClickAsync_ReturnsFalse_WhenMapClickOutsideAlertRadius()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(13); // searchRadiusKm = 5.0
        var alert = new Alert { Id = 1, Latitude = 0, Longitude = 0, RadiusKm = 1.0 };
        _stateServiceMock.Setup(s => s.Alerts).Returns([alert]);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        
        // Click is at ~2.2km away from center (Latitude 0.02 is approx 2.2km at equator)
        var lat = 0.02; 
        double lng = 0;

        // Act
        var result = await _service.HandleMapClickAsync(lat, lng, false);

        // Assert
        Assert.False(result); // Should be false because it's outside 1km radius and > 100m from center
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensReportDetails_WhenMapClickInsideAlertRadiusButNoReports()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        var alert = new Alert { Id = 1, Latitude = 0, Longitude = 0, RadiusKm = 1.0 };
        _stateServiceMock.Setup(s => s.Alerts).Returns([alert]);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]);
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
                Assert.Equal(1, parameters["SelectedAlertId"]);
            }
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_DoesNothing_WhenAlertCreationModeActive()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        
        var reports = new List<Report> { new() { Id = 1 } };
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        
        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            dialogOpened = true;
            _dialogService.Close();
        };

        // Act - Call with alertCreationMode = true
        var result = await _service.HandleMapClickAsync(0, 0, false, alertCreationMode: true);

        // Assert
        Assert.True(result); // Handled (blocked)
        Assert.False(dialogOpened); // No dialog should open
    }

    [Fact]
    public async Task HandleMapContextMenuAsync_DoesNothing_WhenAlertCreationModeActive()
    {
        // Arrange
        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            dialogOpened = true;
            _dialogService.Close();
        };

        // Act
        await _service.HandleMapContextMenuAsync(0, 0, alertCreationMode: true);

        // Assert
        Assert.False(dialogOpened);
    }

    [Fact]
    public async Task HandleMapClickAsync_IncludesExplicitReportId_EvenIfOutsideRadius()
    {
        // Arrange
        var lat = 10.0;
        var lng = 10.0;
        var explicitReportId = 999;
        var explicitReport = new Report { Id = explicitReportId, Latitude = 20.0, Longitude = 20.0 }; // Far away

        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15.0);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]); // None nearby
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns([]); // None nearby
        _stateServiceMock.Setup(s => s.Alerts).Returns([]);
        _stateServiceMock.Setup(s => s.Reports)
            .Returns([explicitReport]);
        
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        var dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) =>
        {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
                var reports = (List<Report>)parameters["Reports"];
                Assert.Contains(reports, r => r.Id == explicitReportId);
            }
            _dialogService.Close();
        };

        // Act
        var result = await _service.HandleMapClickAsync(lat, lng, isMarkerClick: false, reportId: explicitReportId);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }
}
