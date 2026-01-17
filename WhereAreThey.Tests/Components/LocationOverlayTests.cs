using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Components.Home;
using WhereAreThey.Services.Interfaces;
using Xunit;

namespace WhereAreThey.Tests.Components;

public class LocationOverlayTests : ComponentTestBase
{
    private readonly Mock<IClientLocationService> _locationServiceMock;

    public LocationOverlayTests()
    {
        _locationServiceMock = new Mock<IClientLocationService>();
        Services.AddSingleton(_locationServiceMock.Object);
    }

    [Fact]
    public void LocationOverlay_RendersNothing_WhenNotLocating()
    {
        // Arrange
        _locationServiceMock.Setup(s => s.IsLocating).Returns(false);

        // Act
        var cut = Render<LocationOverlay>();

        // Assert
        Assert.Empty(cut.Nodes);
    }

    [Fact]
    public void LocationOverlay_RendersLoading_WhenLocating()
    {
        // Arrange
        _locationServiceMock.Setup(s => s.IsLocating).Returns(true);
        _locationServiceMock.Setup(s => s.ShowManualPick).Returns(false);

        // Act
        var cut = Render<LocationOverlay>();

        // Assert
        Assert.NotEmpty(cut.Nodes);
        Assert.Contains("Acquiring_Location", cut.Markup);
        Assert.NotNull(cut.FindComponent<Radzen.Blazor.RadzenProgressBarCircular>());
    }

    [Fact]
    public void LocationOverlay_RendersManualPickButton_WhenShowManualPickIsTrue()
    {
        // Arrange
        _locationServiceMock.Setup(s => s.IsLocating).Returns(true);
        _locationServiceMock.Setup(s => s.ShowManualPick).Returns(true);

        // Act
        var cut = Render<LocationOverlay>();

        // Assert
        var button = cut.FindComponent<Radzen.Blazor.RadzenButton>();
        Assert.Equal("Manual_Pick", button.Instance.Text);
    }

    [Fact]
    public async Task LocationOverlay_ClickingManualPick_TriggersCallback()
    {
        // Arrange
        _locationServiceMock.Setup(s => s.IsLocating).Returns(true);
        _locationServiceMock.Setup(s => s.ShowManualPick).Returns(true);

        bool manualPickTriggered = false;
        var cut = Render<LocationOverlay>(parameters => parameters
            .Add(p => p.OnManualPick, () => manualPickTriggered = true)
        );

        // Act
        var button = cut.FindComponent<Radzen.Blazor.RadzenButton>();
        await cut.InvokeAsync(() => button.Instance.Click.InvokeAsync());

        // Assert
        Assert.True(manualPickTriggered);
    }
}
