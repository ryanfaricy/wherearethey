using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for broadcasting and subscribing to application-wide events.
/// </summary>
public interface IEventService
{
    /// <summary>Raised when an entity is changed.</summary>
    event Action<object, EntityChangeType> OnEntityChanged;
    /// <summary>Raised when a batch of entities of a certain type has changed (e.g. mass delete).</summary>
    event Action<Type> OnEntityBatchChanged;

    /// <summary>Raised when system settings are changed.</summary>
    event Action<SystemSettings> OnSettingsChanged;
    /// <summary>Raised when an administrative login attempt occurs.</summary>
    event Action<AdminLoginAttempt> OnAdminLoginAttempt;
    /// <summary>Raised when an email address is verified.</summary>
    event Action<string> OnEmailVerified;
    /// <summary>Raised when the number of active connections changes.</summary>
    event Action OnConnectionCountChanged;
    /// <summary>Raised when the application theme is changed.</summary>
    event Action OnThemeChanged;

    /// <summary>Notifies subscribers that an entity has changed.</summary>
    void NotifyEntityChanged<T>(T entity, EntityChangeType type) where T : IAuditable;
    /// <summary>Notifies subscribers that a batch of entities of a certain type has changed.</summary>
    void NotifyEntityBatchChanged(Type entityType);
    /// <summary>Notifies subscribers that settings have changed.</summary>
    void NotifySettingsChanged(SystemSettings settings);
    /// <summary>Notifies subscribers of an admin login attempt.</summary>
    void NotifyAdminLoginAttempt(AdminLoginAttempt attempt);
    /// <summary>Notifies subscribers that an email has been verified.</summary>
    void NotifyEmailVerified(string emailHash);
    /// <summary>Notifies subscribers that the connection count has changed.</summary>
    void NotifyConnectionCountChanged();
    /// <summary>Notifies subscribers that the theme has changed.</summary>
    void NotifyThemeChanged();
}
