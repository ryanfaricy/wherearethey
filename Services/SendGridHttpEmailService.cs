using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WhereAreThey.Services;

public class SendGridHttpEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<SendGridHttpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.SendGridApiKey))
        {
            logger.LogWarning("SendGrid API Key not configured. Skipping SendGrid.");
            throw new InvalidOperationException("SendGrid configuration missing");
        }

        try
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = to } },
                        subject = subject
                    }
                },
                from = new { email = _options.FromEmail, name = _options.FromName },
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = body
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SendGridApiKey);
            request.Content = content;

            logger.LogDebug("Sending email to {To} via SendGrid HTTP API", to);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to send email via SendGrid API. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorResponse);
                throw new Exception($"Failed to send email via SendGrid API. Status: {response.StatusCode}");
            }

            logger.LogInformation("Email sent to {To} via SendGrid API with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To} via SendGrid API", to);
            throw;
        }
    }
}
