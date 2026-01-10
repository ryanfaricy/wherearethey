using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AppThemeServiceTests
{
    [Fact]
    public void SetTheme_ShouldUpdateCurrentTheme()
    {
        // Arrange
        var service = new AppThemeService();

        // Act
        service.SetTheme(AppTheme.Dark);

        // Assert
        Assert.Equal(AppTheme.Dark, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_ShouldTriggerEventWhenThemeChanges()
    {
        // Arrange
        var service = new AppThemeService();
        var eventTriggered = false;
        service.OnThemeChanged += () => eventTriggered = true;

        // Act
        service.SetTheme(AppTheme.Dark);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void SetTheme_ShouldNotTriggerEventWhenThemeIsSame()
    {
        // Arrange
        var service = new AppThemeService();
        service.SetTheme(AppTheme.Light);
        var eventTriggered = false;
        service.OnThemeChanged += () => eventTriggered = true;

        // Act
        service.SetTheme(AppTheme.Light);

        // Assert
        Assert.False(eventTriggered);
    }
}
