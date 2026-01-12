using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class BrevoHttpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldLogWarningAndReturnIfNoApiKey()
    {
        // Arrange
        var options = new EmailOptions { ApiKey = "" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<BrevoHttpEmailService>>();
        var httpClient = new HttpClient();
        var service = new BrevoHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Brevo API Key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPostToBrevoApi()
    {
        // Arrange
        var options = new EmailOptions { ApiKey = "test-api-key", FromEmail = "sender@example.com", FromName = "Sender" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<BrevoHttpEmailService>>();
        
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"messageId\": \"123\"}"),
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new BrevoHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("recipient@example.com", "Hello", "<b>World</b>");

        // Assert
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Once(),
           ItExpr.Is<HttpRequestMessage>(req =>
              req.Method == HttpMethod.Post &&
              req.RequestUri == new Uri("https://api.brevo.com/v3/smtp/email") &&
              req.Headers.Contains("api-key") &&
              req.Headers.GetValues("api-key").First() == "test-api-key"),
           ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowOnFailureStatus()
    {
        // Arrange
        var options = new EmailOptions { ApiKey = "test-api-key" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<BrevoHttpEmailService>>();
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
              StatusCode = HttpStatusCode.BadRequest,
              Content = new StringContent("{\"code\": \"bad_request\", \"message\": \"invalid\"}"),
           });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new BrevoHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.SendEmailAsync("test@example.com", "Sub", "Body"));
    }
}
