namespace WhereAreThey.Models;

/// <summary>
/// Represents user feedback or a bug report.
/// </summary>
public class Feedback : IAuditable
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <summary>
    /// The type of feedback (e.g., "Bug", "Feature").
    /// </summary>
    public string Type { get; set; } = "Bug";

    /// <summary>
    /// The feedback message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Anonymous identifier of the user who submitted the feedback.
    /// </summary>
    public string? UserIdentifier { get; set; }

    /// <summary>
    /// The timestamp when the feedback was submitted.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
