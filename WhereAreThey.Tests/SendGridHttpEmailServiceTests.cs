using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class SendGridHttpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldThrowIfNoConfig()
    {
        // Arrange
        var options = new EmailOptions { SendGridApiKey = "" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<SendGridHttpEmailService>>();
        var httpClient = new HttpClient();
        var service = new SendGridHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendEmailAsync("test@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPostToSendGridApi()
    {
        // Arrange
        var options = new EmailOptions { SendGridApiKey = "sg-key", FromEmail = "sender@example.com" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<SendGridHttpEmailService>>();
        
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
              StatusCode = HttpStatusCode.Accepted, // SendGrid returns 202
              Content = new StringContent("{}"),
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new SendGridHttpEmailService(httpClient, optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("recipient@example.com", "Hello", "World");

        // Assert
        handlerMock.Protected().Verify(
           "SendAsync",
           Times.Once(),
           ItExpr.Is<HttpRequestMessage>(req =>
              req.Method == HttpMethod.Post &&
              req.RequestUri == new Uri("https://api.sendgrid.com/v3/mail/send") &&
              req.Headers.Authorization != null &&
              req.Headers.Authorization.Scheme == "Bearer" &&
              req.Headers.Authorization.Parameter == "sg-key"),
           ItExpr.IsAny<CancellationToken>()
        );
    }
}
