using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class AppThemeServiceTests
{
    private readonly Mock<IEventService> _mockEventService = new();

    [Fact]
    public void SetTheme_ShouldUpdateCurrentTheme()
    {
        // Arrange
        var service = new AppThemeService(_mockEventService.Object, NullLogger<AppThemeService>.Instance);

        // Act
        service.SetTheme(AppTheme.Dark);

        // Assert
        Assert.Equal(AppTheme.Dark, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_ShouldTriggerEventWhenThemeChanges()
    {
        // Arrange
        var service = new AppThemeService(_mockEventService.Object, NullLogger<AppThemeService>.Instance);

        // Act
        service.SetTheme(AppTheme.Dark);

        // Assert
        _mockEventService.Verify(x => x.NotifyThemeChanged(), Times.Once);
    }

    [Fact]
    public void SetTheme_ShouldNotTriggerEventWhenThemeIsSame()
    {
        // Arrange
        var service = new AppThemeService(_mockEventService.Object, NullLogger<AppThemeService>.Instance);
        service.SetTheme(AppTheme.Light);
        _mockEventService.Invocations.Clear();

        // Act
        service.SetTheme(AppTheme.Light);

        // Assert
        _mockEventService.Verify(x => x.NotifyThemeChanged(), Times.Never);
    }
}
