using Moq;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using Xunit;

namespace WhereAreThey.Tests;

public class UserConnectionServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly UserConnectionService _service;

    public UserConnectionServiceTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _service = new UserConnectionService(_eventServiceMock.Object);
    }

    [Fact]
    public void Increment_IncreasesCount_AndNotifies()
    {
        // Act
        _service.Increment();

        // Assert
        Assert.Equal(1, _service.ConnectionCount);
        _eventServiceMock.Verify(e => e.NotifyConnectionCountChanged(), Times.Once);
    }

    [Fact]
    public void Decrement_DecreasesCount_AndNotifies()
    {
        // Arrange
        _service.Increment();
        _eventServiceMock.Invocations.Clear();

        // Act
        _service.Decrement();

        // Assert
        Assert.Equal(0, _service.ConnectionCount);
        _eventServiceMock.Verify(e => e.NotifyConnectionCountChanged(), Times.Once);
    }

    [Fact]
    public void ConnectionCount_NeverBelowZero()
    {
        // Act
        _service.Decrement();

        // Assert
        Assert.Equal(0, _service.ConnectionCount);
    }
}
