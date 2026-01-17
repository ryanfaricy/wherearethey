using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen.Blazor;
using WhereAreThey.Components.Pages;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class SettingsDialogTests : ComponentTestBase
{
    private readonly Mock<IAppThemeService> _themeServiceMock;

    public SettingsDialogTests()
    {
        _themeServiceMock = new Mock<IAppThemeService>();
        Services.AddSingleton(_themeServiceMock.Object);
        
        // NavigationManager is already registered in BunitContext
    }

    [Fact]
    public void SettingsDialog_Renders_Correctly()
    {
        // Arrange
        _themeServiceMock.Setup(s => s.CurrentTheme).Returns(AppTheme.Light);
        
        // Mock JS calls in OnAfterRenderAsync
        JSInterop.Setup<string?>("getUserIdentifier").SetResult("test-id");
        JSInterop.Setup<string>("themeManager.getStoredTheme").SetResult("Light");
        JSInterop.SetupVoid("pwaFunctions.registerHelper", _ => true);
        JSInterop.SetupVoid("registerSettingsHelper", _ => true);
        JSInterop.Setup<bool>("pwaFunctions.isPwa").SetResult(false);
        JSInterop.Setup<bool>("pwaFunctions.isIOS").SetResult(false);

        // Act
        var cut = Render<SettingsDialog>();

        // Assert
        Assert.Contains("SETTINGS", cut.Markup);
        Assert.Contains("Language", cut.Markup);
        Assert.Contains("Theme", cut.Markup);
        
        // Check for theme selector
        Assert.NotNull(cut.FindComponent<RadzenSelectBar<AppTheme>>());
    }

    [Fact]
    public async Task SettingsDialog_ThemeChange_CallsServiceAndJs()
    {
        // Arrange
        _themeServiceMock.Setup(s => s.CurrentTheme).Returns(AppTheme.Light);
        
        JSInterop.Setup<string?>("getUserIdentifier").SetResult("test-id");
        JSInterop.Setup<string>("themeManager.getStoredTheme").SetResult("Light");
        JSInterop.SetupVoid("pwaFunctions.registerHelper", _ => true);
        JSInterop.SetupVoid("registerSettingsHelper", _ => true);
        JSInterop.Setup<bool>("pwaFunctions.isPwa").SetResult(false);
        JSInterop.Setup<bool>("pwaFunctions.isIOS").SetResult(false);
        
        var themeJsSetup = JSInterop.SetupVoid("themeManager.setTheme", "Dark");
        themeJsSetup.SetVoidResult();

        var cut = Render<SettingsDialog>();
        var selectBar = cut.FindComponent<RadzenSelectBar<AppTheme>>();

        // Act
        await cut.InvokeAsync(async () => {
            await selectBar.Instance.ValueChanged.InvokeAsync(AppTheme.Dark);
            await selectBar.Instance.Change.InvokeAsync(AppTheme.Dark);
        });

        // Assert
        _themeServiceMock.Verify(s => s.SetTheme(AppTheme.Dark), Times.Once);
        themeJsSetup.VerifyInvoke("themeManager.setTheme");
    }
    
    [Fact]
    public void SettingsDialog_LanguageChange_Navigates()
    {
        // Arrange
        _themeServiceMock.Setup(s => s.CurrentTheme).Returns(AppTheme.Light);
        
        JSInterop.Setup<string?>("getUserIdentifier").SetResult("test-id");
        JSInterop.Setup<string>("themeManager.getStoredTheme").SetResult("Light");
        JSInterop.SetupVoid("pwaFunctions.registerHelper", _ => true);
        JSInterop.SetupVoid("registerSettingsHelper", _ => true);
        JSInterop.Setup<bool>("pwaFunctions.isPwa").SetResult(false);
        JSInterop.Setup<bool>("pwaFunctions.isIOS").SetResult(false);

        var nav = Services.GetRequiredService<NavigationManager>();
        
        var cut = Render<SettingsDialog>();
        var dropdown = cut.FindComponent<RadzenDropDown<string>>();

        // Act
        // Simulate changing culture to 'es'
        // RadzenDropDown uses Change event
        cut.InvokeAsync(() => dropdown.Instance.Change.InvokeAsync("es"));

        // Assert
        Assert.Contains("/Culture/Set", nav.Uri);
        Assert.Contains("culture=es", nav.Uri);
    }
}
