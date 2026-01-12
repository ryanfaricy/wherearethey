using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhereAreThey.Services;
using Xunit;

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
        var httpClient = new HttpClient();
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object);

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
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object);

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
              req.Headers.Authorization.Parameter == "mock-token"),
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
        var service = new MicrosoftGraphEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.SendEmailAsync("test@example.com", "Sub", "Body"));
        Assert.Contains("Failed to authenticate", ex.Message);
    }
}
