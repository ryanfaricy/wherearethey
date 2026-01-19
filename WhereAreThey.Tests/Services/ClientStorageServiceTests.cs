using Microsoft.JSInterop;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class ClientStorageServiceTests
{
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly ClientStorageService _service;

    public ClientStorageServiceTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _service = new ClientStorageService(_jsRuntimeMock.Object);
    }

    [Fact]
    public async Task GetUserIdentifierAsync_InvokesCorrectJS()
    {
        // Arrange
        _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>(
                "getUserIdentifier", 
                It.IsAny<object?[]>()
            ))
            .ReturnsAsync("test-id");

        // Act
        var result = await _service.GetUserIdentifierAsync();

        // Assert
        Assert.Equal("test-id", result);
        _jsRuntimeMock.Verify(js => js.InvokeAsync<string?>(
            "getUserIdentifier", 
            It.Is<object?[]>(args => args.Length == 0)
        ), Times.Once);
    }

    [Fact]
    public async Task IsNewUserAsync_InvokesCorrectJS()
    {
        // Arrange
        _jsRuntimeMock.Setup(js => js.InvokeAsync<bool>(
                "isNewUser", 
                It.IsAny<object?[]>()
            ))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsNewUserAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ClearNewUserFlagAsync_InvokesCorrectJS()
    {
        // Act
        await _service.ClearNewUserFlagAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "clearNewUserFlag", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_InvokesCorrectJS()
    {
        // Arrange
        _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>(
                "localStorage.getItem", 
                new object[] { "key1" }
            ))
            .ReturnsAsync("val1");

        // Act
        var result = await _service.GetItemAsync("key1");

        // Assert
        Assert.Equal("val1", result);
    }

    [Fact]
    public async Task SetItemAsync_InvokesCorrectJS()
    {
        // Act
        await _service.SetItemAsync("key1", "val1");

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem", 
            new object[] { "key1", "val1" }
        ), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_InvokesCorrectJS()
    {
        // Act
        await _service.RemoveItemAsync("key1");

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem", 
            new object[] { "key1" }
        ), Times.Once);
    }

    [Fact]
    public async Task GetLocationAsync_ReturnsPosition_OnSuccess()
    {
        // Arrange
        var position = new GeolocationPosition { Coords = new GeolocationCoordinates { Latitude = 1.0, Longitude = 2.0 } };
        _jsRuntimeMock.Setup(js => js.InvokeAsync<GeolocationPosition>(
                "getLocation", 
                It.IsAny<object?[]>()
            ))
            .ReturnsAsync(position);

        // Act
        var result = await _service.GetLocationAsync();

        // Assert
        Assert.Equal(position, result);
    }

    [Fact]
    public async Task GetLocationAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        _jsRuntimeMock.Setup(js => js.InvokeAsync<GeolocationPosition>(
                "getLocation", 
                It.IsAny<object?[]>()
            ))
            .ThrowsAsync(new Exception("JS error"));

        // Act
        var result = await _service.GetLocationAsync();

        // Assert
        Assert.Null(result);
    }
}
