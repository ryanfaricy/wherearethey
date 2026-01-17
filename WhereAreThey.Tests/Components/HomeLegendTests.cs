using Bunit;
using WhereAreThey.Components.Home;
using Xunit;

namespace WhereAreThey.Tests.Components;

public class HomeLegendTests : ComponentTestBase
{
    [Fact]
    public void HomeLegend_Renders_Correctly()
    {
        // Arrange
        // Act
        var cut = Render<HomeLegend>();

        // Assert
        // Check if the legend card is rendered
        Assert.NotNull(cut.Find(".legend-card"));
        
        // Check if all three legend items are rendered (Emergency Report, Report, Alert Zone)
        var icons = cut.FindComponents<Radzen.Blazor.RadzenIcon>();
        Assert.Equal(3, icons.Count);
        
        // Verify localized text is present (using the keys since our mock returns keys)
        Assert.Contains("EMERGENCY_REPORT", cut.Markup);
        Assert.Contains("Report", cut.Markup);
        Assert.Contains("Alert_Zone", cut.Markup);
    }
}
