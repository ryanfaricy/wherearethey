using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using Radzen.Blazor;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class HelpDialogTests : ComponentTestBase
{
    private readonly Mock<ISettingsService> _settingsServiceMock;

    public HelpDialogTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        Services.AddSingleton(_settingsServiceMock.Object);
        
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = true });
    }

    [Fact]
    public void HelpDialog_Renders_Sections()
    {
        // Arrange
        // Act
        var cut = Render<HelpDialog>();

        // Assert
        Assert.Contains("What_Is_This_Title", cut.Markup);
        Assert.Contains("Quick_Help_Guide", cut.Markup);
        Assert.Contains("Map_Interactions", cut.Markup);
        Assert.Contains("Map_Controls_Title", cut.Markup);
        Assert.Contains("Toolbar_Actions", cut.Markup);
        Assert.Contains("AntiSpam_Limits_Title", cut.Markup);
    }

    [Fact]
    public void HelpDialog_Donate_Visibility_BasedOnSettings()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = false });

        // Act
        var cut = Render<HelpDialog>();

        // Assert
        Assert.DoesNotContain("Donate_Desc", cut.Markup);
        
        // Change settings and re-render
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { DonationsEnabled = true });
            
        var cut2 = Render<HelpDialog>();
        Assert.Contains("Donate_Desc", cut2.Markup);
    }

    [Fact]
    public void HelpDialog_ClickingPrivacy_OpensDialog()
    {
        // Arrange
        var dialogService = Services.GetRequiredService<DialogService>();
        var dialogOpened = false;
        dialogService.OnOpen += (title, type, parameters, options) => {
            if (title == "PRIVACY_POLICY")
            {
                dialogOpened = true;
            }
        };

        var cut = Render<HelpDialog>();
        var buttons = cut.FindComponents<RadzenButton>();
        var privacyButton = buttons.First(b => b.Instance.Text == "PRIVACY");

        // Act
        cut.InvokeAsync(() => privacyButton.Instance.Click.InvokeAsync(null));

        // Assert
        Assert.True(dialogOpened);
    }

    [Fact]
    public void HelpDialog_ClickingFeedback_OpensDialog()
    {
        // Arrange
        var dialogService = Services.GetRequiredService<DialogService>();
        var dialogOpened = false;
        dialogService.OnOpen += (title, type, parameters, options) => {
            if (title == "FEEDBACK")
            {
                dialogOpened = true;
            }
        };

        var cut = Render<HelpDialog>();
        var buttons = cut.FindComponents<RadzenButton>();
        var feedbackButton = buttons.First(b => b.Instance.Text == "FEEDBACK");

        // Act
        cut.InvokeAsync(() => feedbackButton.Instance.Click.InvokeAsync(null));

        // Assert
        Assert.True(dialogOpened);
    }
}
