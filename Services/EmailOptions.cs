namespace WhereAreThey.Services;

public class EmailOptions
{
    // Common
    public string FromEmail { get; set; } = "alerts@aretheyhere.com";
    public string FromName { get; set; } = "AreTheyHere Alerts";

    // Brevo (Primary)
    public string ApiKey { get; set; } = "";

    // Mailjet
    public string MailjetApiKey { get; set; } = "";
    public string MailjetApiSecret { get; set; } = "";

    // SendGrid
    public string SendGridApiKey { get; set; } = "";

    // Microsoft Graph
    public string GraphTenantId { get; set; } = "";
    public string GraphClientId { get; set; } = "";
    public string GraphClientSecret { get; set; } = "";
    public string GraphSenderUserId { get; set; } = "";

    // SMTP (Fallback)
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 2525;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}
