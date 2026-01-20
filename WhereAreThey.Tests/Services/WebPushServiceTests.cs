using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class WebPushServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IBaseUrlProvider> _baseUrlProviderMock = new();
    private readonly Mock<IOptions<EmailOptions>> _emailOptionsMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<WebPushService>> _loggerMock = new();

    public WebPushServiceTests()
    {
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings 
            { 
                VapidPublicKey = "test-public-key", 
                VapidPrivateKey = "test-private-key",
            });

        _emailOptionsMock.Setup(o => o.Value)
            .Returns(new EmailOptions { FromEmail = "test@example.com" });

        _baseUrlProviderMock.Setup(b => b.GetBaseUrl())
            .Returns("https://example.com");

        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
    }

    [Fact]
    public async Task SendNotificationsAsync_ShouldUseMailtoSubject_WhenEmailIsAvailable()
    {
        // Arrange
        var service = new WebPushService(
            _settingsServiceMock.Object,
            _contextFactoryMock.Object,
            _baseUrlProviderMock.Object,
            _emailOptionsMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);

        var subscriptions = new List<WebPushSubscription>
        {
            new() { Id = 1, Endpoint = "https://endpoint.com", P256DH = "p256dh", Auth = "auth" },
        };

        // Act & Assert
        // We can't easily verify the internal VapidDetails without refactoring more,
        // but we can verify it doesn't throw before calling SendNotificationAsync (which will fail because of invalid keys/endpoint)
        // Given that SendNotificationAsync will throw WebPushException or similar due to invalid keys/endpoint,
        // we can at least check if it reaches that point.
        
        await service.SendNotificationsAsync(subscriptions, "Title", "Message");
        
        // If it reaches here, it means it didn't crash during setup.
        // The logger should have recorded an error because of the fake endpoint/keys.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("mailto:test@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationsAsync_ShouldUseBaseUrlSubject_WhenEmailIsMissing()
    {
        // Arrange
        _emailOptionsMock.Setup(o => o.Value)
            .Returns(new EmailOptions { FromEmail = "" });

        var service = new WebPushService(
            _settingsServiceMock.Object,
            _contextFactoryMock.Object,
            _baseUrlProviderMock.Object,
            _emailOptionsMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);

        var subscriptions = new List<WebPushSubscription>
        {
            new() { Id = 1, Endpoint = "https://endpoint.com", P256DH = "p256dh", Auth = "auth" },
        };

        // Act
        await service.SendNotificationsAsync(subscriptions, "Title", "Message");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("https://example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
