using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WhereAreThey.Services;

public class EmailOptions
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
    public string FromEmail { get; set; } = "alerts@wherearethey.com";
    public string FromName { get; set; } = "WhereAreThey Alerts";
    public bool EnableSsl { get; set; } = true;
}

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
            _logger.LogWarning("SMTP Server not configured. Email to {To} not sent. Subject: {Subject}", to, subject);
            _logger.LogInformation("EMAIL CONTENT: {Body}", body);
            return;
        }

        try
        {
            using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort)
            {
                Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPass),
                EnableSsl = _options.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
