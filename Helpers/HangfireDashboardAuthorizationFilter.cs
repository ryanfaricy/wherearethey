using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace WhereAreThey.Helpers;

/// <summary>
/// Authorization filter for the Hangfire Dashboard that uses Basic Authentication.
/// </summary>
/// <param name="adminPassword">The password required to access the dashboard.</param>
public class HangfireDashboardAuthorizationFilter(string adminPassword) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return Authorize(context.GetHttpContext());
    }

    public bool Authorize(HttpContext httpContext)
    {
        string? authHeader = httpContext.Request.Headers.Authorization;
        
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encodedCredential = authHeader[6..];
                var decodedCredential = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredential));
                var parts = decodedCredential.Split(':');
                if (parts.Length == 2)
                {
                    var user = parts[0];
                    var password = parts[1];

                    // Using FixedTimeEquals to prevent timing attacks
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var adminPasswordBytes = Encoding.UTF8.GetBytes(adminPassword);

                    if (user == "admin" && 
                        passwordBytes.Length == adminPasswordBytes.Length && 
                        CryptographicOperations.FixedTimeEquals(passwordBytes, adminPasswordBytes))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore decoding errors and proceed to challenge
            }
        }

        SetChallengeResponse(httpContext);
        return false;
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
