using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class EventService : IEventService
{
    /// <inheritdoc />
    public event Action<object, EntityChangeType>? OnEntityChanged;
    /// <inheritdoc />
    public event Action<Type>? OnEntityBatchChanged;
    /// <inheritdoc />
    public event Action<SystemSettings>? OnSettingsChanged;
    /// <inheritdoc />
    public event Action<AdminLoginAttempt>? OnAdminLoginAttempt;
    /// <inheritdoc />
    public event Action<string>? OnEmailVerified;
    /// <inheritdoc />
    public event Action? OnConnectionCountChanged;
    /// <inheritdoc />
    public event Action? OnThemeChanged;

    /// <inheritdoc />
    public void NotifyEntityChanged<T>(T entity, EntityChangeType type) where T : IAuditable
    {
        OnEntityChanged?.Invoke(entity, type);
    }

    /// <inheritdoc />
    public void NotifyEntityBatchChanged(Type entityType)
    {
        OnEntityBatchChanged?.Invoke(entityType);
    }

    /// <inheritdoc />
    public void NotifySettingsChanged(SystemSettings settings) => OnSettingsChanged?.Invoke(settings);
    /// <inheritdoc />
    public void NotifyAdminLoginAttempt(AdminLoginAttempt attempt) => OnAdminLoginAttempt?.Invoke(attempt);
    /// <inheritdoc />
    public void NotifyEmailVerified(string emailHash) => OnEmailVerified?.Invoke(emailHash);
    /// <inheritdoc />
    public void NotifyConnectionCountChanged() => OnConnectionCountChanged?.Invoke();
    /// <inheritdoc />
    public void NotifyThemeChanged() => OnThemeChanged?.Invoke();
}
