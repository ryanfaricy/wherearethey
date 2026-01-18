using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for administrative operations and authentication.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Attempts to log in as an administrator.
    /// </summary>
    /// <param name="password">The administrative password.</param>
    /// <param name="ipAddress">The IP address of the login attempt.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> LoginAsync(string password, string? ipAddress);

    /// <summary>
    /// Gets a list of recent administrative login attempts.
    /// </summary>
    /// <param name="count">The number of attempts to retrieve.</param>
    /// <returns>A list of recent login attempts.</returns>
    Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50);

    /// <summary>
    /// Raised when an administrator successfully logs in during the current session.
    /// </summary>
    event Action OnAdminLogin;

    /// <summary>
    /// Raised when an administrator logs out during the current session.
    /// </summary>
    event Action OnAdminLogout;

    /// <summary>
    /// Notifies subscribers that a successful admin login has occurred.
    /// </summary>
    void NotifyAdminLogin();

    /// <summary>
    /// Notifies subscribers that an admin logout has occurred.
    /// </summary>
    void NotifyAdminLogout();

    /// <summary>
    /// Checks if the current user is an administrator based on local storage state.
    /// </summary>
    /// <returns>True if the user is considered an admin; otherwise, false.</returns>
    Task<bool> IsAdminAsync();
}
