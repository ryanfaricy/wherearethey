namespace WhereAreThey.Models;

/// <summary>
/// Represents an alert zone configured by a user to receive notifications.
/// </summary>
public class Alert : IAuditable, ILocatable
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for public URLs.
    /// </summary>
    public Guid ExternalId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Latitude of the alert zone center.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the alert zone center.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Radius of the alert zone in kilometers.
    /// </summary>
    public double RadiusKm { get; set; }

    /// <summary>
    /// Optional message or name for the alert zone.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Encrypted email address for notifications.
    /// </summary>
    public string? EncryptedEmail { get; set; }

    /// <summary>
    /// Hash of the email address for lookups and verification.
    /// </summary>
    public string? EmailHash { get; set; }

    /// <summary>
    /// Whether the email address has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Whether to send email notifications for this alert.
    /// </summary>
    public bool UseEmail { get; set; } = true;

    /// <summary>
    /// Whether to send web push notifications for this alert.
    /// </summary>
    public bool UsePush { get; set; } = true;

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Unique identifier for the user who created the alert.
    /// </summary>
    public string? UserIdentifier { get; set; }
}
