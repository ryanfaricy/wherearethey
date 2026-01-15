using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Services;

namespace WhereAreThey.Tests;

public class FallbackEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_ShouldTryNextProviderOnFailure()
    {
        // Arrange
        var service1 = new Mock<IEmailService>();
        var service2 = new Mock<IEmailService>();
        
        service1.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Service 1 failed"));
            
        var loggerMock = new Mock<ILogger<FallbackEmailService>>();
        var fallbackService = new FallbackEmailService([service1.Object, service2.Object], loggerMock.Object);

        // Act
        await fallbackService.SendEmailAsync("test@example.com", "Sub", "Body");

        // Assert
        service1.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        service2.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldStopOnFirstSuccess()
    {
        // Arrange
        var service1 = new Mock<IEmailService>();
        var service2 = new Mock<IEmailService>();
        
        var loggerMock = new Mock<ILogger<FallbackEmailService>>();
        var fallbackService = new FallbackEmailService([service1.Object, service2.Object], loggerMock.Object);

        // Act
        await fallbackService.SendEmailAsync("test@example.com", "Sub", "Body");

        // Assert
        service1.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        service2.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowAggregateExceptionIfAllFail()
    {
        // Arrange
        var service1 = new Mock<IEmailService>();
        var service2 = new Mock<IEmailService>();
        
        service1.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Fail 1"));
        service2.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Fail 2"));
            
        var loggerMock = new Mock<ILogger<FallbackEmailService>>();
        var fallbackService = new FallbackEmailService([service1.Object, service2.Object], loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AggregateException>(() => fallbackService.SendEmailAsync("test@example.com", "Sub", "Body"));
        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSkipInvalidOperationException()
    {
        // Arrange
        var service1 = new Mock<IEmailService>();
        var service2 = new Mock<IEmailService>();
        
        service1.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Not configured"));
            
        var loggerMock = new Mock<ILogger<FallbackEmailService>>();
        var fallbackService = new FallbackEmailService([service1.Object, service2.Object], loggerMock.Object);

        // Act
        await fallbackService.SendEmailAsync("test@example.com", "Sub", "Body");

        // Assert
        service1.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        service2.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
