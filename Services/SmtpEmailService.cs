using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace WhereAreThey.Services;

public class EmailOptions
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 2525;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "alerts@aretheyhere.com";
    public string FromName { get; set; } = "AreTheyHere Alerts";
    public bool EnableSsl { get; set; } = true;
}

public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
            logger.LogWarning("SMTP Server not configured. Email to {To} not sent. Subject: {Subject}", to, subject);
            logger.LogInformation("EMAIL CONTENT: {Body}", body);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Set a timeout to prevent hanging forever
            // Increased to 60s as some cloud environments (like Railway) can have very slow handshakes
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
            int maxAttempts = 2;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
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

            logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} via {Server}:{Port}", to, _options.SmtpServer, _options.SmtpPort);
            throw;
        }
    }
}
