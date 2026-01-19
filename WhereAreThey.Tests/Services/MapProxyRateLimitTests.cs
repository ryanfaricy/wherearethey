using System.Net;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class MapProxyRateLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MapProxyRateLimitTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SkipMigrations", "true");
            builder.ConfigureTestServices(services =>
            {
                // Replace DB with InMemory to avoid hitting Postgres
                services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();
                services.AddDbContextFactory<ApplicationDbContext>(options => 
                    options.UseInMemoryDatabase("MapProxyTestDB"));

                // Replace Hangfire with MemoryStorage to avoid hitting Postgres
                services.RemoveAll<JobStorage>();
                services.AddHangfire(config => config.UseMemoryStorage());

                // Replace DataProtection to avoid hitting DB
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.GetTempPath()));

                // Mock dependencies needed by the endpoint
                var reportServiceMock = new Mock<IReportService>();
                var settingsServiceMock = new Mock<ISettingsService>();

                reportServiceMock.Setup(s => s.GetReportByExternalIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(Result<LocationReport>.Success(new LocationReport { Latitude = 0, Longitude = 0 }));

                settingsServiceMock.Setup(s => s.GetSettingsAsync())
                    .ReturnsAsync(new SystemSettings { MapboxToken = "test-token" });

                services.AddSingleton(reportServiceMock.Object);
                services.AddSingleton(settingsServiceMock.Object);
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
