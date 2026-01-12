using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class MailjetHttpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldThrowIfNoConfig()
    {
        // Arrange
        var options = new EmailOptions { MailjetApiKey = "" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MailjetHttpEmailService>>();
        var httpClient = new HttpClient();
        var service = new MailjetHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendEmailAsync("test@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPostToMailjetApi()
    {
        // Arrange
        var options = new EmailOptions { MailjetApiKey = "key", MailjetApiSecret = "secret", FromEmail = "sender@example.com" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<MailjetHttpEmailService>>();
        
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
              Content = new StringContent("{}"),
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new MailjetHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("recipient@example.com", "Hello", "World");

        // Assert
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Once(),
           ItExpr.Is<HttpRequestMessage>(req =>
              req.Method == HttpMethod.Post &&
              req.RequestUri == new Uri("https://api.mailjet.com/v3.1/send") &&
              req.Headers.Authorization != null &&
              req.Headers.Authorization.Scheme == "Basic"),
           ItExpr.IsAny<CancellationToken>()
        );
    }
}
