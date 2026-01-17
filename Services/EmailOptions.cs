using System.ComponentModel.DataAnnotations;

namespace WhereAreThey.Services;

public class EmailOptions
{
    // Common
    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = "alert@aretheyhere.com";
    
    [Required]
    public string FromName { get; set; } = "AreTheyHere Alerts";

    // Microsoft Graph
    public string GraphTenantId { get; set; } = "";
    public string GraphClientId { get; set; } = "";
    public string GraphClientSecret { get; set; } = "";
    public string GraphSenderUserId { get; set; } = "";

    // SMTP (Fallback)
    public string SmtpServer { get; set; } = "";
    
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 2525;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}
