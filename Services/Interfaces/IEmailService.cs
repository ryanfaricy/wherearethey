using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body (HTML supported).</param>
    Task SendEmailAsync(string to, string subject, string body);

    /// <summary>
    /// Sends multiple emails asynchronously, potentially in batches if they share subject and body.
    /// </summary>
    /// <param name="emails">The collection of emails to send.</param>
    Task SendEmailsAsync(IEnumerable<Email> emails);
}
