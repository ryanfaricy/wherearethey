using Moq;
using WhereAreThey.Events;
using WhereAreThey.Events.Handlers;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class EventHandlerTests
{
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<IReportProcessingService> _reportProcessingServiceMock = new();

    [Fact]
    public async Task AlertCreatedHandler_ShouldSendVerificationEmail_WhenNotVerified()
    {
        // Arrange
        var handler = new AlertCreatedHandler(_alertServiceMock.Object);
        var email = "test@example.com";
        var alert = new Alert { IsVerified = false };
        var notification = new AlertCreatedEvent(alert, email);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _alertServiceMock.Verify(s => s.SendVerificationEmailAsync(email, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AlertCreatedHandler_ShouldNotSendVerificationEmail_WhenAlreadyVerified()
    {
        // Arrange
        var handler = new AlertCreatedHandler(_alertServiceMock.Object);
        var email = "test@example.com";
        var alert = new Alert { IsVerified = true };
        var notification = new AlertCreatedEvent(alert, email);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        _alertServiceMock.Verify(s => s.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReportAddedHandler_ShouldCallProcessReportAsync()
    {
        // Arrange
        var handler = new ReportAddedHandler(_reportProcessingServiceMock.Object);
        var report = new LocationReport { Id = 1 };
        var notification = new ReportAddedEvent(report);

        // Act
        await handler.Handle(notification, CancellationToken.None);
        
        // Wait a bit because ReportAddedHandler uses Task.Run
        await Task.Delay(100);

        // Assert
        _reportProcessingServiceMock.Verify(s => s.ProcessReportAsync(report), Times.Once);
    }
}
