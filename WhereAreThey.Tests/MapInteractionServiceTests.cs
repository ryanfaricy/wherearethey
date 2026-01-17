using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using Microsoft.Extensions.Localization;
using Radzen;
using Xunit;
using WhereAreThey.Components;
using WhereAreThey.Components.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace WhereAreThey.Tests;

public class MapInteractionServiceTests : TestContext
{
    private readonly Mock<IMapService> _mapServiceMock;
    private readonly Mock<IMapStateService> _stateServiceMock;
    private readonly DialogService _dialogService;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<IStringLocalizer<App>> _localizerMock;
    private readonly MapInteractionService _service;

    public MapInteractionServiceTests()
    {
        _mapServiceMock = new Mock<IMapService>();
        _stateServiceMock = new Mock<IMapStateService>();
        _adminServiceMock = new Mock<IAdminService>();
        _localizerMock = new Mock<IStringLocalizer<App>>();
        
        Services.AddRadzenComponents();
        _dialogService = Services.GetRequiredService<DialogService>();
        
        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));

        _service = new MapInteractionService(
            _mapServiceMock.Object,
            _stateServiceMock.Object,
            _dialogService,
            _adminServiceMock.Object,
            _localizerMock.Object);
    }

    [Theory]
    [InlineData(10, false, 5.0)]
    [InlineData(15, false, 0.2)]
    [InlineData(16, false, 0.2)]
    [InlineData(10, true, 0.05)]
    public void CalculateSearchRadius_ReturnsCorrectValues(double zoom, bool isMarkerClick, double expected)
    {
        // Act
        var result = _service.CalculateSearchRadius(zoom, isMarkerClick);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleMapClickAsync_ReturnsFalse_WhenNoNearbyData()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new List<LocationReport>());
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new List<Alert>());

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleMapClickAsync_OpensDetails_WhenNearbyDataFound()
    {
        // Arrange
        _mapServiceMock.Setup(m => m.GetZoomLevelAsync()).ReturnsAsync(15);
        var reports = new List<LocationReport> { new() { Id = 1 } };
        _stateServiceMock.Setup(s => s.FindNearbyReports(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(reports);
        _stateServiceMock.Setup(s => s.FindNearbyAlerts(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new List<Alert>());
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);

        bool dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) => {
            if (type == typeof(ReportDetailsDialog))
            {
                dialogOpened = true;
                // Close the dialog immediately to avoid hanging on the 'await'
                _dialogService.Close(null);
            }
        };

        // Act
        var result = await _service.HandleMapClickAsync(0, 0, false);

        // Assert
        Assert.True(result);
        Assert.True(dialogOpened);
    }

    [Fact]
    public async Task HandleMapContextMenuAsync_ShowsGhostPinAndOpensDialog()
    {
        // Arrange
        _adminServiceMock.Setup(a => a.IsAdminAsync()).ReturnsAsync(false);
        
        bool dialogOpened = false;
        _dialogService.OnOpen += (title, type, parameters, options) => {
            if (type == typeof(ReportDialog))
            {
                dialogOpened = true;
                // Close the dialog immediately to avoid hanging on the 'await'
                _dialogService.Close(null);
            }
        };

        // Act
        await _service.HandleMapContextMenuAsync(10, 20, "user-id");

        // Assert
        _mapServiceMock.Verify(m => m.ShowGhostPinAsync(10, 20), Times.Once);
        Assert.True(dialogOpened);
        _mapServiceMock.Verify(m => m.HideGhostPinAsync(), Times.Once);
    }
}
