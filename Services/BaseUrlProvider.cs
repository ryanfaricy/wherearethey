using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class BaseUrlProvider(
    NavigationManager? navigationManager = null,
    IHttpContextAccessor? httpContextAccessor = null,
    IOptions<AppOptions>? options = null,
    ILogger<BaseUrlProvider>? logger = null) : IBaseUrlProvider
{
    /// <inheritdoc />
    public string GetBaseUrl()
    {
        // 1. Try NavigationManager (Blazor)
        if (navigationManager != null)
        {
            try
            {
                var url = navigationManager.BaseUri.TrimEnd('/');
                logger?.LogDebug("Resolved base URL from NavigationManager: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                // NavigationManager might throw if not in a circuit
                logger?.LogTrace(ex, "NavigationManager not available for base URL resolution");
            }
        }

        // 2. Try HttpContext (API/MVC)
        var request = httpContextAccessor?.HttpContext?.Request;
        if (request != null)
        {
            var url = $"{request.Scheme}://{request.Host}".TrimEnd('/');
            logger?.LogDebug("Resolved base URL from HttpContext: {Url}", url);
            return url;
        }

        // 3. Fallback to configuration
        var fallbackUrl = options?.Value.BaseUrl.TrimEnd('/') ?? "https://www.aretheyhere.com";
        logger?.LogDebug("Resolved base URL from configuration fallback: {Url}", fallbackUrl);
        return fallbackUrl;
    }
}
