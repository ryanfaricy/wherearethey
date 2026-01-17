using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Components.Home;
using Xunit;

namespace WhereAreThey.Tests.Components;

public class HomeToolbarTests : ComponentTestBase
{
    [Fact]
    public void HomeToolbar_Renders_AllButtons()
    {
        // Arrange
        // (Base class handles services)

        // Act
        var cut = Render<HomeToolbar>();

        // Assert
        // There should be buttons for: REPORT, ALERTS, SETTINGS, HELP, REFRESH
        // and DONATE if enabled.
        var buttons = cut.FindComponents<Radzen.Blazor.RadzenButton>();
        Assert.Equal(5, buttons.Count); // REPORT, ALERTS, SETTINGS, HELP, REFRESH (DonationsEnabled is false by default)
    }

    [Fact]
    public void HomeToolbar_DonationsEnabled_RendersDonateButton()
    {
        // Arrange
        // Act
        var cut = Render<HomeToolbar>(parameters => parameters
            .Add(p => p.DonationsEnabled, true)
        );

        // Assert
        var buttons = cut.FindComponents<Radzen.Blazor.RadzenButton>();
        Assert.Equal(6, buttons.Count);
    }

    [Fact]
    public void HomeToolbar_ClickReport_TriggersEvent()
    {
        // Arrange
        var clicked = false;
        var cut = Render<HomeToolbar>(parameters => parameters
            .Add(p => p.OnReportClick, () => clicked = true)
        );

        // Act
        var reportButton = cut.FindAll("button").First(b => b.TextContent.Contains("REPORT"));
        reportButton.Click();

        // Assert
        Assert.True(clicked);
    }
}
