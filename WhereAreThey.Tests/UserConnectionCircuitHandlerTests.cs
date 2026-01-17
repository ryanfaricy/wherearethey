using Moq;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Xunit;

namespace WhereAreThey.Tests;

public class UserConnectionCircuitHandlerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly UserConnectionService _connectionService;
    private readonly UserConnectionCircuitHandler _handler;

    public UserConnectionCircuitHandlerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _connectionService = new UserConnectionService(_eventServiceMock.Object);
        _handler = new UserConnectionCircuitHandler(_connectionService);
    }

    [Fact]
    public async Task OnCircuitOpenedAsync_IncrementsConnectionCount()
    {
        // Act
        await _handler.OnCircuitOpenedAsync(null!, default);

        // Assert
        Assert.Equal(1, _connectionService.ConnectionCount);
        _eventServiceMock.Verify(e => e.NotifyConnectionCountChanged(), Times.Once);
    }

    [Fact]
    public async Task OnCircuitClosedAsync_DecrementsConnectionCount()
    {
        // Arrange
        await _handler.OnCircuitOpenedAsync(null!, default);
        _eventServiceMock.Invocations.Clear();

        // Act
        await _handler.OnCircuitClosedAsync(null!, default);

        // Assert
        Assert.Equal(0, _connectionService.ConnectionCount);
        _eventServiceMock.Verify(e => e.NotifyConnectionCountChanged(), Times.Once);
    }
}
