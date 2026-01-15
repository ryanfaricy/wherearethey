namespace WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

public class UserConnectionService(IEventService eventService)
{
    private int _connectionCount;

    public int ConnectionCount => Math.Max(0, _connectionCount);

    public void Increment()
    {
        Interlocked.Increment(ref _connectionCount);
        eventService.NotifyConnectionCountChanged();
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref _connectionCount);
        eventService.NotifyConnectionCountChanged();
    }
}
