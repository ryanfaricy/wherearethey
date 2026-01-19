using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldLogWarningAndReturnIfNoSmtpServer()
    {
        // Arrange
        var options = new EmailOptions { SmtpServer = "" };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<SmtpEmailService>>();
        var service = new SmtpEmailService(optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMTP Server not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
