using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WhereAreThey.Services;

public class MicrosoftGraphEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<MicrosoftGraphEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_options.GraphTenantId) || 
            string.IsNullOrEmpty(_options.GraphClientId) || 
            string.IsNullOrEmpty(_options.GraphClientSecret) || 
            string.IsNullOrEmpty(_options.GraphSenderUserId))
        {
            logger.LogWarning("Microsoft Graph configuration is incomplete. Skipping Microsoft Graph.");
            throw new InvalidOperationException("Microsoft Graph configuration missing");
        }

        try
        {
            var token = await GetAccessTokenAsync();
            
            var emailPayload = new
            {
                message = new
                {
                    subject = subject,
                    body = new
                    {
                        contentType = "HTML",
                        content = body
                    },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { address = to } }
                    }
                },
                saveToSentItems = false
            };

            var json = JsonSerializer.Serialize(emailPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.microsoft.com/v1.0/users/{_options.GraphSenderUserId}/sendMail");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            logger.LogDebug("Sending email to {To} via Microsoft Graph API", to);
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to send email via Microsoft Graph. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorResponse);
                throw new Exception($"Failed to send email via Microsoft Graph. Status: {response.StatusCode}");
            }

            logger.LogInformation("Email sent to {To} via Microsoft Graph with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To} via Microsoft Graph", to);
            throw;
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokenUrl = $"https://login.microsoftonline.com/{_options.GraphTenantId}/oauth2/v2.0/token";
        var dict = new Dictionary<string, string>
        {
            { "client_id", _options.GraphClientId },
            { "scope", "https://graph.microsoft.com/.default" },
            { "client_secret", _options.GraphClientSecret },
            { "grant_type", "client_credentials" }
        };

        var content = new FormUrlEncodedContent(dict);
        var response = await httpClient.PostAsync(tokenUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to get Microsoft Graph access token. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorResponse);
            throw new Exception("Failed to authenticate with Microsoft Graph");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? throw new Exception("Access token missing in response");
    }
}
