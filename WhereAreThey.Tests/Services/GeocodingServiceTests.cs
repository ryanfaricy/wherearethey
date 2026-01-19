using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class GeocodingServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<ILogger<GeocodingService>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();

    public GeocodingServiceTests()
    {
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { MapboxToken = "test-token" });
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldReturnAddress_WhenSuccessful()
    {
        // Arrange
        var responseContent = new
        {
            features = new[]
            {
                new { place_name = "123 Test St, City, Country" },
            },
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseContent)),
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var service = new GeocodingService(httpClient, _settingsServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7128, -74.0060);

        // Assert
        Assert.Equal("123 Test St, City, Country", result);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnResults_WhenSuccessful()
    {
        // Arrange
        var responseContent = new
        {
            features = new[]
            {
                new { place_name = "Result 1", center = new[] { -74.0, 40.0 } },
                new { place_name = "Result 2", center = new[] { -73.0, 41.0 } },
            },
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseContent)),
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var service = new GeocodingService(httpClient, _settingsServiceMock.Object, _loggerMock.Object);

        // Act
        var results = await service.SearchAsync("test query");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Result 1", results[0].Address);
        Assert.Equal(40.0, results[0].Latitude);
        Assert.Equal(-74.0, results[0].Longitude);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldReturnNull_WhenNoToken()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { MapboxToken = "" });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var service = new GeocodingService(httpClient, _settingsServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await service.ReverseGeocodeAsync(0, 0);

        // Assert
        Assert.Null(result);
    }
}
