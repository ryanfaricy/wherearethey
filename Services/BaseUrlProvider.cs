using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class BaseUrlProvider(
    NavigationManager? navigationManager = null,
    IHttpContextAccessor? httpContextAccessor = null,
    IOptions<AppOptions>? options = null) : IBaseUrlProvider
{
    public string GetBaseUrl()
    {
        // 1. Try NavigationManager (Blazor)
        if (navigationManager != null)
        {
            try
            {
                return navigationManager.BaseUri.TrimEnd('/');
            }
            catch
            {
                // NavigationManager might throw if not in a circuit
            }
        }

        // 2. Try HttpContext (API/MVC)
        var request = httpContextAccessor?.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}".TrimEnd('/');
        }

        // 3. Fallback to configuration
        return options?.Value.BaseUrl.TrimEnd('/') ?? "https://www.aretheyhere.com";
    }
}
