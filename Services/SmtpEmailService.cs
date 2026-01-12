using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace WhereAreThey.Services;

public class EmailOptions
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
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
            client.Timeout = 15000; // 15 seconds

            var secureSocketOptions = SecureSocketOptions.None;
            if (_options.EnableSsl)
            {
                secureSocketOptions = _options.SmtpPort == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : SecureSocketOptions.StartTls;
            }

            await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, secureSocketOptions);

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
