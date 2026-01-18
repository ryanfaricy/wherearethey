using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using Radzen.Blazor;
using WhereAreThey.Components;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class AddressSearchTests : ComponentTestBase
{
    private readonly Mock<IGeocodingService> _geocodingServiceMock;

    public AddressSearchTests()
    {
        _geocodingServiceMock = new Mock<IGeocodingService>();
        Services.AddSingleton(_geocodingServiceMock.Object);
    }

    [Fact]
    public void AddressSearch_Renders_Correctly()
    {
        // Arrange
        // Act
        var cut = Render<AddressSearch>();

        // Assert
        Assert.NotNull(cut.FindComponent<RadzenAutoComplete>());
    }

    [Fact]
    public async Task AddressSearch_Typing_TriggersSearch()
    {
        // Arrange
        var results = new List<GeocodingResult>
        {
            new() { Address = "123 Main St", Latitude = 10, Longitude = 20 },
        };
        _geocodingServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>()))
            .ReturnsAsync(results);

        var cut = Render<AddressSearch>();
        var autocomplete = cut.FindComponent<RadzenAutoComplete>();

        // Act
        // We simulate the LoadData event which is triggered when typing
        await cut.InvokeAsync(() => autocomplete.Instance.LoadData.InvokeAsync(new LoadDataArgs { Filter = "Main" }));

        // Assert
        _geocodingServiceMock.Verify(s => s.SearchAsync("Main"), Times.Once);
    }

    [Fact]
    public async Task AddressSearch_SelectingResult_TriggersCallback()
    {
        // Arrange
        var results = new List<GeocodingResult>
        {
            new() { Address = "123 Main St", Latitude = 10, Longitude = 20 },
        };
        _geocodingServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>()))
            .ReturnsAsync(results);

        GeocodingResult? selectedResult = null;
        var cut = Render<AddressSearch>(parameters => parameters
            .Add(p => p.OnResultSelected, r => selectedResult = r)
        );
        var autocomplete = cut.FindComponent<RadzenAutoComplete>();

        // Trigger search to populate internal results
        await cut.InvokeAsync(() => autocomplete.Instance.LoadData.InvokeAsync(new LoadDataArgs { Filter = "Main" }));

        // Act
        // Simulate selecting the address
        await cut.InvokeAsync(() => autocomplete.Instance.Change.InvokeAsync("123 Main St"));

        // Assert
        Assert.NotNull(selectedResult);
        Assert.Equal("123 Main St", selectedResult.Address);
    }

    [Fact]
    public async Task AddressSearch_Clearing_TriggersCallbackWithEmptyString()
    {
        // Arrange
        var searchVal = "Initial";
        var cut = Render<AddressSearch>(parameters => parameters
            .Add(p => p.SearchValue, searchVal)
            .Add(p => p.SearchValueChanged, s => searchVal = s)
        );
        var autocomplete = cut.FindComponent<RadzenAutoComplete>();

        // Act
        // Simulate clearing the input (Radzen sends null or empty string to Change)
        await cut.InvokeAsync(() => autocomplete.Instance.Change.InvokeAsync(null));

        // Assert
        Assert.Equal("", searchVal);
    }
}
