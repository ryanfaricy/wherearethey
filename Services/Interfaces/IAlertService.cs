using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing user alerts and notifications.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Creates a new alert for a specific location.
    /// </summary>
    /// <param name="alert">The alert configuration.</param>
    /// <param name="email">The email address to notify.</param>
    /// <returns>The created alert.</returns>
    Task<Alert> CreateAlertAsync(Alert alert, string email);

    /// <summary>
    /// Sends a verification email to the user.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="emailHash">The hash of the email address.</param>
    Task SendVerificationEmailAsync(string email, string emailHash);

    /// <summary>
    /// Verifies an email address using a token.
    /// </summary>
    /// <param name="token">The verification token.</param>
    /// <returns>True if verification was successful; otherwise, false.</returns>
    Task<bool> VerifyEmailAsync(string token);

    /// <summary>
    /// Decrypts an encrypted email address.
    /// </summary>
    /// <param name="encryptedEmail">The encrypted email string.</param>
    /// <returns>The decrypted email address, or null if decryption fails.</returns>
    string? DecryptEmail(string? encryptedEmail);

    /// <summary>
    /// Gets an alert by its external identifier.
    /// </summary>
    /// <param name="externalId">The external GUID of the alert.</param>
    /// <returns>The alert if found; otherwise, null.</returns>
    Task<Alert?> GetAlertByExternalIdAsync(Guid externalId);

    /// <summary>
    /// Gets active alerts, optionally filtered by user and verification status.
    /// </summary>
    /// <param name="userIdentifier">The unique identifier for the user.</param>
    /// <param name="onlyVerified">Whether to only return verified alerts.</param>
    /// <returns>A list of active alerts.</returns>
    Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true);

    /// <summary>
    /// Deactivates an alert.
    /// </summary>
    /// <param name="id">The internal ID of the alert.</param>
    /// <returns>True if the alert was successfully deactivated; otherwise, false.</returns>
    Task<bool> DeactivateAlertAsync(int id);

    /// <summary>
    /// Updates an existing alert.
    /// </summary>
    /// <param name="alert">The alert with updated values.</param>
    /// <param name="email">The email address to notify (optional, if changed).</param>
    Task UpdateAlertAsync(Alert alert, string? email = null);

    /// <summary>
    /// Gets all alerts that match a given location.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <returns>A list of matching alerts.</returns>
    Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude);

    /// <summary>
    /// Gets all alerts for administrative purposes.
    /// </summary>
    /// <returns>A list of all alerts.</returns>
    Task<List<Alert>> GetAllAlertsAdminAsync();

    /// <summary>
    /// Permanently deletes an alert.
    /// </summary>
    /// <param name="id">The internal ID of the alert.</param>
    Task DeleteAlertAsync(int id);
}
