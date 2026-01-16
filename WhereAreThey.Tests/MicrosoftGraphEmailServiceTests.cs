using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhereAreThey.Services;

namespace WhereAreThey.Tests;

public class MicrosoftGraphEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationExceptionIfNoConfig()
    {
        // Arrange
        var options = new EmailOptions { GraphTenantId = "" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MicrosoftGraphEmailService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient();
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object, cache);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendEmailAsync("test@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPostToGraphApi()
    {
        // Arrange
        var options = new EmailOptions 
        { 
            GraphTenantId = "tenant", 
            GraphClientId = "client", 
            GraphClientSecret = "secret", 
            GraphSenderUserId = "user-id",
            FromEmail = "sender@example.com", 
            FromName = "Sender" 
        };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MicrosoftGraphEmailService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        
        // Mock token response
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oauth2/v2.0/token")),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"access_token\": \"mock-token\"}"),
           });

        // Mock sendMail response
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("sendMail")),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.Accepted
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object, cache);

        // Act
        await service.SendEmailAsync("recipient@example.com", "Hello", "<b>World</b>");

        // Assert
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Once(),
           ItExpr.Is<HttpRequestMessage>(req =>
              req.Method == HttpMethod.Post &&
              req.RequestUri!.ToString().Contains("sendMail") &&
              req.Headers.Authorization!.Scheme == "Bearer" &&
              req.Headers.Authorization.Parameter == "mock-token" &&
              req.Content!.ReadAsStringAsync().Result.Contains("sender@example.com") &&
              req.Content.ReadAsStringAsync().Result.Contains("Sender")),
           ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowOnTokenFailure()
    {
        // Arrange
        var options = new EmailOptions 
        { 
            GraphTenantId = "tenant", 
            GraphClientId = "client", 
            GraphClientSecret = "secret", 
            GraphSenderUserId = "user-id" 
        };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MicrosoftGraphEmailService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oauth2/v2.0/token")),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.Unauthorized,
              Content = new StringContent("{\"error\": \"invalid_client\"}"),
           });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object, cache);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.SendEmailAsync("test@example.com", "Sub", "Body"));
        Assert.Contains("Failed to authenticate", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldCacheToken()
    {
        // Arrange
        var options = new EmailOptions 
        { 
            GraphTenantId = "tenant", 
            GraphClientId = "client", 
            GraphClientSecret = "secret", 
            GraphSenderUserId = "user-id",
            FromEmail = "sender@example.com", 
            FromName = "Sender" 
        };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MicrosoftGraphEmailService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        
        // Mock token response - should only be called once
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oauth2/v2.0/token")),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"access_token\": \"mock-token\", \"expires_in\": 3600}"),
           });

        // Mock sendMail response
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("sendMail")),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.Accepted
           });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object, cache);

        // Act
        await service.SendEmailAsync("recipient1@example.com", "Hello 1", "Body 1");
        await service.SendEmailAsync("recipient2@example.com", "Hello 2", "Body 2");

        // Assert
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Once(), // Token should be requested only once
           ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oauth2/v2.0/token")),
           ItExpr.IsAny<CancellationToken>()
        );
        
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Exactly(2), // sendMail should be called twice
           ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("sendMail")),
           ItExpr.IsAny<CancellationToken>()
        );
    }
}
