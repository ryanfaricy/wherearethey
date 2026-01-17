using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Radzen;
using WhereAreThey.Components.Home;
using WhereAreThey.Components;
using Xunit;

namespace WhereAreThey.Tests.ComponentTests;

public class HomeToolbarTests : TestContext
{
    public HomeToolbarTests()
    {
        var mockLocalizer = new Mock<IStringLocalizer<App>>();
        mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));
        Services.AddSingleton(mockLocalizer.Object);
        Services.AddRadzenComponents();
        
        // Mock JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void HomeToolbar_RendersCorrectly()
    {
        // Act
        var cut = Render<HomeToolbar>(parameters => parameters
            .Add(p => p.IsMobile, false)
            .Add(p => p.DonationsEnabled, true)
        );

        // Assert
        var buttons = cut.FindAll("button");
        Assert.NotEmpty(buttons);
        
        var markup = cut.Markup;
        Assert.Contains("REPORT", markup);
        Assert.Contains("DONATE", markup);
    }

    [Fact]
    public void HomeToolbar_DoesNotShowDonate_WhenDisabled()
    {
        // Act
        var cut = Render<HomeToolbar>(parameters => parameters
            .Add(p => p.IsMobile, false)
            .Add(p => p.DonationsEnabled, false)
        );

        // Assert
        var markup = cut.Markup;
        Assert.Contains("REPORT", markup);
        Assert.DoesNotContain("DONATE", markup);
    }
}
