using Bunit;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen.Blazor;
using WhereAreThey.Components.Layout;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class NavMenuTests : ComponentTestBase
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IAdminService> _adminServiceMock;

    public NavMenuTests()
    {
        var mapServiceMock = new Mock<IMapService>();
        var storageServiceMock = new Mock<IClientStorageService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _adminServiceMock = new Mock<IAdminService>();
        
        // Use real ProtectedLocalStorage with mocked dependencies
        var localStorage = new ProtectedLocalStorage(
            JSInterop.JSRuntime, 
            Mock.Of<IDataProtectionProvider>());

        Services.AddSingleton(mapServiceMock.Object);
        Services.AddSingleton(storageServiceMock.Object);
        Services.AddSingleton(_settingsServiceMock.Object);
        Services.AddSingleton(_adminServiceMock.Object);
        Services.AddSingleton(localStorage);
        
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = true });
    }

    [Fact]
    public void NavMenu_Renders_StandardItems()
    {
        // Arrange
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var items = cut.FindComponents<RadzenPanelMenuItem>();
        Assert.Contains(items, i => i.Instance.Text == "ARE THEY HERE?");
        Assert.Contains(items, i => i.Instance.Text == "THEY ARE HERE!");
        Assert.Contains(items, i => i.Instance.Text == "Alerts");
    }

    [Fact]
    public void NavMenu_Donate_Visibility_BasedOnSettings()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = false });

        // Act
        var cut = Render<NavMenu>();

        // Assert
        var items = cut.FindComponents<RadzenPanelMenuItem>();
        Assert.DoesNotContain(items, i => i.Instance.Text == "Donate");
        
        // Change settings and re-render
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = true });
            
        var cut2 = Render<NavMenu>();
        var items2 = cut2.FindComponents<RadzenPanelMenuItem>();
        Assert.Contains(items2, i => i.Instance.Text == "Donate");
    }

    [Fact]
    public void NavMenu_ControlPanel_Visibility_InitiallyHidden()
    {
        // Arrange
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var items = cut.FindComponents<RadzenPanelMenuItem>();
        Assert.DoesNotContain(items, i => i.Instance.Text == "CONTROL_PANEL");
    }

    [Fact]
    public void NavMenu_ControlPanel_Shows_OnAdminLogin()
    {
        // Arrange
        var cut = Render<NavMenu>();

        // Act
        _adminServiceMock.Raise(a => a.OnAdminLogin += null);

        // Assert
        var items = cut.FindComponents<RadzenPanelMenuItem>();
        Assert.Contains(items, i => i.Instance.Text == "CONTROL_PANEL");
    }

    [Fact]
    public void NavMenu_ClickingItem_TriggersCallback()
    {
        // Arrange
        var clicked = false;
        var cut = Render<NavMenu>(parameters => parameters
            .Add(p => p.OnMenuItemClick, () => clicked = true)
        );
        
        // Find the "ARE THEY HERE?" item (it's the first one)
        var item = cut.FindComponent<RadzenPanelMenuItem>();

        // Act
        // Use cut.InvokeAsync to simulate the click if necessary, but RadzenPanelMenuItem click 
        // usually triggers the Click event callback
        cut.InvokeAsync(() => item.Instance.Click.InvokeAsync(null));

        // Assert
        Assert.True(clicked);
    }
}
