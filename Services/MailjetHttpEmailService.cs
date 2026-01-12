using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WhereAreThey.Services;

public class MailjetHttpEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<MailjetHttpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.MailjetApiKey) || string.IsNullOrEmpty(_options.MailjetApiSecret))
        {
            logger.LogWarning("Mailjet API Key or Secret not configured. Skipping Mailjet.");
            throw new InvalidOperationException("Mailjet configuration missing");
        }

        try
        {
            var payload = new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new { Email = _options.FromEmail, Name = _options.FromName },
                        To = new[] { new { Email = to, Name = "" } },
                        Subject = subject,
                        HTMLPart = body
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailjet.com/v3.1/send");
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.MailjetApiKey}:{_options.MailjetApiSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
            request.Content = content;

            logger.LogDebug("Sending email to {To} via Mailjet HTTP API", to);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to send email via Mailjet API. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorResponse);
                throw new Exception($"Failed to send email via Mailjet API. Status: {response.StatusCode}");
            }

            logger.LogInformation("Email sent to {To} via Mailjet API with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To} via Mailjet API", to);
            throw;
        }
    }
}
