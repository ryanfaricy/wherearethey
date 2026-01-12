using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WhereAreThey.Services;

public class BrevoHttpEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<BrevoHttpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            logger.LogWarning("Brevo API Key not configured. Email to {To} not sent. Subject: {Subject}", to, subject);
            logger.LogInformation("EMAIL CONTENT: {Body}", body);
            return;
        }

        try
        {
            var payload = new
            {
                sender = new { name = _options.FromName, email = _options.FromEmail },
                to = new[] { new { email = to } },
                subject = subject,
                htmlContent = body
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _options.ApiKey);
            request.Content = content;

            logger.LogDebug("Sending email to {To} via Brevo HTTP API", to);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to send email via Brevo API. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorResponse);
                throw new Exception($"Failed to send email via Brevo API. Status: {response.StatusCode}");
            }

            logger.LogInformation("Email sent to {To} via Brevo API with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To} via Brevo API", to);
            throw;
        }
    }
}
