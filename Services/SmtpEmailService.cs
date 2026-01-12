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
    public string FromEmail { get; set; } = "alerts@aretheyhere.com";
    public string FromName { get; set; } = "AreTheyHere Alerts";
    public bool EnableSsl { get; set; } = true;
}

public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.SmtpServer) || _options.SmtpServer.Contains("YOUR_"))
        {
            logger.LogWarning("SMTP Server not configured. Email to {To} not sent. Subject: {Subject}", to, subject);
            logger.LogInformation("EMAIL CONTENT (STUB): {Body}", body);
            return;
        }

        if (string.IsNullOrEmpty(_options.SmtpUser) || _options.SmtpUser.Contains("YOUR_SMTP_USER") ||
            string.IsNullOrEmpty(_options.SmtpPass) || _options.SmtpPass.Contains("YOUR_SMTP_PASSWORD"))
        {
            logger.LogError("SMTP Authentication not configured correctly (Placeholders detected). Set Email:SmtpUser and Email:SmtpPass in secrets. Email to {To} not sent.", to);
            return;
        }

        try
        {
            using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPass);
            client.EnableSsl = _options.EnableSsl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            logger.LogInformation("Attempting to send email to {To} via {Server}:{Port} (User: {User}, SSL: {SSL})", 
                to, _options.SmtpServer, _options.SmtpPort, _options.SmtpUser, _options.EnableSsl);
            
            await client.SendMailAsync(mailMessage);
            logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
