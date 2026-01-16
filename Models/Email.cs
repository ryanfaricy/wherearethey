namespace WhereAreThey.Models;

/// <summary>
/// Represents an email to be sent.
/// </summary>
public class Email
{
    /// <summary>
    /// Gets or sets the recipient email address.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email body (HTML supported).
    /// </summary>
    public string Body { get; set; } = string.Empty;
}
