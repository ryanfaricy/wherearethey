using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests;

public class MapProxyRateLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MapProxyRateLimitTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock dependencies needed by the endpoint to avoid hitting DB/External APIs
                var reportServiceMock = new Mock<IReportService>();
                var settingsServiceMock = new Mock<ISettingsService>();

                reportServiceMock.Setup(s => s.GetReportByExternalIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(Result<LocationReport>.Success(new LocationReport { Latitude = 0, Longitude = 0 }));

                settingsServiceMock.Setup(s => s.GetSettingsAsync())
                    .ReturnsAsync(new SystemSettings { MapboxToken = "test-token" });

                services.AddSingleton(reportServiceMock.Object);
                services.AddSingleton(settingsServiceMock.Object);
                
                // We don't necessarily need to mock HttpClient for the rate limit test 
                // because the rate limiter should trigger before the HttpClient is used.
            });
        });
    }

    [Fact]
    public async Task MapProxy_ShouldBeRateLimited()
    {
        // Arrange
        var client = _factory.CreateClient();
        var reportId = Guid.NewGuid().ToString();
        var url = $"/api/map/proxy?reportId={reportId}";

        // Act & Assert
        // We set the limit to 30 per minute in Program.cs
        for (var i = 0; i < 30; i++)
        {
            var response = await client.GetAsync(url);
            // We don't care about the success of the proxy itself, just that it's not rate limited yet.
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // 31st request should be rate limited
        var rateLimitedResponse = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
    }
}
