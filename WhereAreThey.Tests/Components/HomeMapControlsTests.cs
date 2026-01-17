using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Components.Home;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using Xunit;

namespace WhereAreThey.Tests.Components;

public class HomeMapControlsTests : ComponentTestBase
{
    private readonly Mock<IGeocodingService> _geocodingServiceMock;

    public HomeMapControlsTests()
    {
        _geocodingServiceMock = new Mock<IGeocodingService>();
        Services.AddSingleton(_geocodingServiceMock.Object);
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
        Assert.NotNull(cut.FindComponent<WhereAreThey.Components.AddressSearch>());
        Assert.NotNull(cut.FindComponent<Radzen.Blazor.RadzenToggleButton>());
        Assert.NotNull(cut.FindComponent<Radzen.Blazor.RadzenSelectBar<int>>());
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

        var toggleButton = cut.FindComponent<Radzen.Blazor.RadzenToggleButton>();

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

        var selectBar = cut.FindComponent<Radzen.Blazor.RadzenSelectBar<int>>();

        // Act
        await cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync(2));

        // Assert
        Assert.Equal(2, selectedHours);
    }
}
