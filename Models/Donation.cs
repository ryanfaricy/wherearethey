namespace WhereAreThey.Models;

/// <summary>
/// Represents a donation made to the platform.
/// </summary>
public class Donation : IAuditable
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <summary>
    /// The donation amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency of the donation (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// The email address of the donor.
    /// </summary>
    public string? DonorEmail { get; set; }

    /// <summary>
    /// The name of the donor.
    /// </summary>
    public string? DonorName { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The identifier for the payment in the external system (e.g., Square).
    /// </summary>
    public string? ExternalPaymentId { get; set; }

    /// <summary>
    /// The current status of the donation (e.g., "pending", "succeeded", "failed").
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Checks if the donation was successful.
    /// </summary>
    public bool IsSuccess() => Status is "succeeded" or "completed";
}
