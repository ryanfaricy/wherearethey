using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen.Blazor;
using WhereAreThey.Components;
using WhereAreThey.Components.Home;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class HomeMapControlsTests : ComponentTestBase
{
    public HomeMapControlsTests()
    {
        var geocodingServiceMock = new Mock<IGeocodingService>();
        Services.AddSingleton(geocodingServiceMock.Object);
    }

    [Fact]
    public void HomeMapControls_Renders_Correctly()
    {
        // Arrange
        // Act
        var cut = Render<HomeMapControls>(parameters => parameters
            .Add(p => p.ReportExpiryHours, 24)
            .Add(p => p.SelectedHours, 24)
        );

        // Assert
        Assert.NotNull(cut.FindComponent<AddressSearch>());
        Assert.NotNull(cut.FindComponent<RadzenToggleButton>());
        Assert.NotNull(cut.FindComponent<RadzenSelectBar<int>>());
    }

    [Fact]
    public async Task HomeMapControls_FollowMeToggle_TriggersCallback()
    {
        // Arrange
        bool? followMeValue = null;
        var cut = Render<HomeMapControls>(parameters => parameters
            .Add(p => p.FollowMe, false)
            .Add(p => p.FollowMeChanged, EventCallback.Factory.Create<bool>(this, v => followMeValue = v))
        );

        var toggleButton = cut.FindComponent<RadzenToggleButton>();

        // Act
        await cut.InvokeAsync(() => toggleButton.Instance.ValueChanged.InvokeAsync(true));

        // Assert
        Assert.True(followMeValue);
    }

    [Fact]
    public async Task HomeMapControls_HoursChange_TriggersCallback()
    {
        // Arrange
        int? selectedHours = null;
        var cut = Render<HomeMapControls>(parameters => parameters
            .Add(p => p.SelectedHours, 1)
            .Add(p => p.ReportExpiryHours, 24)
            .Add(p => p.OnHoursChange, EventCallback.Factory.Create<int>(this, v => selectedHours = v))
        );

        var selectBar = cut.FindComponent<RadzenSelectBar<int>>();

        // Act
        await cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync(2));

        // Assert
        Assert.Equal(2, selectedHours);
    }
}
