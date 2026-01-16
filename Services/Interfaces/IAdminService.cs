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
    /// <returns>True if login was successful; otherwise, false.</returns>
    Task<bool> LoginAsync(string password, string? ipAddress);

    /// <summary>
    /// Gets a list of recent administrative login attempts.
    /// </summary>
    /// <param name="count">The number of attempts to retrieve.</param>
    /// <returns>A list of recent login attempts.</returns>
    Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50);
}
