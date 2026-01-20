using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <summary>
/// Service for tracking active user connections.
/// </summary>
public class UserConnectionService(IEventService eventService, ILogger<UserConnectionService> logger)
{
    private int _connectionCount;

    /// <summary>
    /// Gets the current number of active connections.
    /// </summary>
    public int ConnectionCount => Math.Max(0, _connectionCount);

    /// <summary>
    /// Increments the connection count.
    /// </summary>
    public void Increment()
    {
        var count = Interlocked.Increment(ref _connectionCount);
        logger.LogDebug("User connected. Active count: {Count}", count);
        eventService.NotifyConnectionCountChanged();
    }

    /// <summary>
    /// Decrements the connection count.
    /// </summary>
    public void Decrement()
    {
        var count = Interlocked.Decrement(ref _connectionCount);
        logger.LogDebug("User disconnected. Active count: {Count}", count);
        eventService.NotifyConnectionCountChanged();
    }
}
