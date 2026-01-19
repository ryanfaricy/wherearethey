using Microsoft.JSInterop;
using Moq;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class HapticFeedbackServiceTests
{
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly HapticFeedbackService _service;

    public HapticFeedbackServiceTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _service = new HapticFeedbackService(_jsRuntimeMock.Object);
    }

    [Fact]
    public async Task VibrateSuccessAsync_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateSuccessAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrateSuccess", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateErrorAsync_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateErrorAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrateError", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateWarningAsync_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateWarningAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrateWarning", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateEmergencyAsync_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateEmergencyAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrateEmergency", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateUpdateAsync_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateUpdateAsync();

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrateUpdate", 
            It.IsAny<object?[]>()
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateAsync_Duration_InvokesCorrectJS()
    {
        // Act
        await _service.VibrateAsync(100);

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrate", 
            new object[] { 100 }
        ), Times.Once);
    }

    [Fact]
    public async Task VibrateAsync_Pattern_InvokesCorrectJS()
    {
        // Arrange
        var pattern = new[] { 100, 50, 100 };

        // Act
        await _service.VibrateAsync(pattern);

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "haptics.vibrate", 
            new object[] { pattern }
        ), Times.Once);
    }
}
