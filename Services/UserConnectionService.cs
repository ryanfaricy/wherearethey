namespace WhereAreThey.Services;

public class UserConnectionService
{
    private int _connectionCount = 0;

    public int ConnectionCount => Math.Max(0, _connectionCount);

    public event Action? OnConnectionCountChanged;

    public void Increment()
    {
        Interlocked.Increment(ref _connectionCount);
        OnConnectionCountChanged?.Invoke();
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref _connectionCount);
        OnConnectionCountChanged?.Invoke();
    }
}
