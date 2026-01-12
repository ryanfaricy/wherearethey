using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

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

    [Fact]
    public async Task SendEmailAsync_ShouldLogErrorAndReturnIfPlaceholdersUsed()
    {
        // Arrange
        var options = new EmailOptions 
        { 
            SmtpServer = "smtp.example.com",
            SmtpUser = "YOUR_SMTP_USER",
            SmtpPass = "YOUR_SMTP_PASSWORD"
        };
        var optionsMock = new Mock<IOptions<EmailOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var loggerMock = new Mock<ILogger<SmtpEmailService>>();
        var service = new SmtpEmailService(optionsMock.Object, loggerMock.Object);

        // Act
        await service.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Placeholders detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
