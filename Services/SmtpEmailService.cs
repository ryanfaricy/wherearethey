using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendMailInternalAsync([to], null, subject, body);
    }

    /// <inheritdoc />
    public async Task SendEmailsAsync(IEnumerable<Email> emails)
    {
        var emailList = emails.ToList();
        if (emailList.Count == 0) return;

        // Group by subject and body to allow BCC optimization
        var groups = emailList.GroupBy(e => new { e.Subject, e.Body });

        foreach (var group in groups)
        {
            var recipients = group.Select(e => e.To).Distinct().ToList();

            if (recipients.Count == 1)
            {
                await SendEmailAsync(recipients[0], group.Key.Subject, group.Key.Body);
            }
            else
            {
                // Send in batches to multiple recipients via BCC to keep addresses private
                // SMTP servers often have limits on the number of recipients per message (e.g. 100)
                const int batchSize = 100;
                for (int i = 0; i < recipients.Count; i += batchSize)
                {
                    var batch = recipients.Skip(i).Take(batchSize).ToList();
                    // Use FromEmail as the 'To' recipient and put the actual recipients in BCC
                    await SendMailInternalAsync([_options.FromEmail], batch, group.Key.Subject, group.Key.Body);
                }
            }
        }
    }

    private async Task SendMailInternalAsync(
        IEnumerable<string>? toRecipients,
        IEnumerable<string>? bccRecipients,
        string subject,
        string body)
    {
        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
            logger.LogWarning("SMTP Server not configured. Email NOT sent. Subject: {Subject}", subject);
            logger.LogInformation("EMAIL CONTENT: {Body}", body);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            
            if (toRecipients != null)
            {
                foreach (var to in toRecipients)
                {
                    message.To.Add(new MailboxAddress("", to));
                }
            }

            if (bccRecipients != null)
            {
                foreach (var bcc in bccRecipients)
                {
                    message.Bcc.Add(new MailboxAddress("", bcc));
                }
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Set a timeout to prevent hanging forever
            client.Timeout = 60000; 
            
            // Disable CRL checks to avoid timeouts in restricted network environments
            client.CheckCertificateRevocation = false;

            // Set LocalDomain for EHLO - derives from FromEmail if possible
            if (!string.IsNullOrEmpty(_options.FromEmail) && _options.FromEmail.Contains('@'))
            {
                client.LocalDomain = _options.FromEmail.Split('@')[1];
            }

            var secureSocketOptions = SecureSocketOptions.None;
            if (_options.EnableSsl)
            {
                secureSocketOptions = _options.SmtpPort == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : SecureSocketOptions.StartTls;
            }

            // Log DNS resolution for debugging cloud networking issues
            try
            {
                var ips = await Dns.GetHostAddressesAsync(_options.SmtpServer);
                logger.LogDebug("Resolved {Server} to: {IPs}", _options.SmtpServer, string.Join(", ", ips.Select(i => i.ToString())));
            }
            catch (Exception dnsEx)
            {
                logger.LogWarning(dnsEx, "Failed to resolve SMTP server {Server}", _options.SmtpServer);
            }

            // Attempt connection with a single retry to handle transient network blips
            var maxAttempts = 2;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    logger.LogDebug("Attempt {Attempt}/{Max}: Connecting to {Server}:{Port} (Options: {Options})", 
                        attempt, maxAttempts, _options.SmtpServer, _options.SmtpPort, secureSocketOptions);
                    
                    await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, secureSocketOptions);
                    break; // Success
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(ex, "Connection attempt {Attempt} failed. Retrying in 2 seconds...", attempt);
                    await Task.Delay(2000);
                }
            }

            if (!string.IsNullOrEmpty(_options.SmtpUser))
            {
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email(s) sent successfully via SMTP with subject {Subject}", subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email via {Server}:{Port}", _options.SmtpServer, _options.SmtpPort);
            throw;
        }
    }
}
