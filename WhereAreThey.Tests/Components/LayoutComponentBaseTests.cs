using Bunit;
using Radzen;
using WhereAreThey.Components.Admin;
using Xunit;

namespace WhereAreThey.Tests.Components;

public class LayoutComponentBaseTests : ComponentTestBase
{
    private class TestLayoutComponent : LayoutComponentBase
    {
        public Orientation GetStackOrientation() => StackOrientation;
        public AlignItems GetStackAlign() => StackAlign;
    }

    [Fact]
    public void Properties_ShouldReflectMobileState()
    {
        // Arrange
        var cut = Render<TestLayoutComponent>(parameters => parameters
            .Add(p => p.IsMobile, true));
        var instance = cut.Instance;

        // Assert - Mobile
        Assert.True(instance.IsMobile);
        Assert.Equal(Orientation.Vertical, instance.GetStackOrientation());
        Assert.Equal(AlignItems.Stretch, instance.GetStackAlign());

        // Act - Desktop
        var cut2 = Render<TestLayoutComponent>(parameters => parameters
            .Add(p => p.IsMobile, false));
        var instance2 = cut2.Instance;

        // Assert - Desktop
        Assert.False(instance2.IsMobile);
        Assert.Equal(Orientation.Horizontal, instance2.GetStackOrientation());
        Assert.Equal(AlignItems.Center, instance2.GetStackAlign());
    }
}
