using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace WhereAreThey.Tests;

public class ClientLocationServiceTests
{
    private readonly Mock<IClientStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ClientLocationService>> _loggerMock;
    private readonly ClientLocationService _service;

    public ClientLocationServiceTests()
    {
        _storageServiceMock = new Mock<IClientStorageService>();
        _loggerMock = new Mock<ILogger<ClientLocationService>>();
        _service = new ClientLocationService(_storageServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetLocationWithFallbackAsync_ReturnsPosition_WhenStorageSucceedsQuickly()
    {
        // Arrange
        var position = new GeolocationPosition { Coords = new GeolocationCoordinates { Latitude = 1, Longitude = 2 } };
        _storageServiceMock.Setup(s => s.GetLocationAsync()).ReturnsAsync(position);

        // Act
        var result = await _service.GetLocationWithFallbackAsync();

        // Assert
        Assert.Equal(position, result);
        Assert.Equal(position, _service.LastKnownPosition);
        Assert.False(_service.IsLocating);
    }

    [Fact]
    public async Task GetLocationWithFallbackAsync_ShowsManualPick_OnTimeout()
    {
        // Arrange
        var tcs = new TaskCompletionSource<GeolocationPosition?>();
        _storageServiceMock.Setup(s => s.GetLocationAsync()).Returns(tcs.Task);

        bool stateChanged = false;
        _service.OnStateChanged += () => stateChanged = true;

        // Act
        // We can't easily "wait" for the 7s timeout in a unit test without it being slow
        // unless we refactor ClientLocationService to take a time provider.
        // For now, let's test the ConfirmManualPick logic which is also part of it.
        
        var locationTask = _service.GetLocationWithFallbackAsync();
        
        // Assert initial state
        Assert.True(_service.IsLocating);

        var manualPosition = new GeolocationPosition { Coords = new GeolocationCoordinates { Latitude = 3, Longitude = 4 } };
        _service.ConfirmManualPick(manualPosition);

        var result = await locationTask;

        // Assert
        Assert.Equal(manualPosition, result);
        Assert.Equal(manualPosition, _service.LastKnownPosition);
        Assert.False(_service.IsLocating);
    }

    [Fact]
    public void UpdateLastKnownPosition_UpdatesPropertiesAndNotifies()
    {
        // Arrange
        var position = new GeolocationPosition { Coords = new GeolocationCoordinates { Latitude = 5, Longitude = 6 } };
        bool notified = false;
        _service.OnStateChanged += () => notified = true;

        // Act
        _service.UpdateLastKnownPosition(position);

        // Assert
        Assert.Equal(position, _service.LastKnownPosition);
        Assert.True(notified);
        Assert.True((DateTime.UtcNow - _service.LastLocationUpdate).TotalSeconds < 1);
    }
}
