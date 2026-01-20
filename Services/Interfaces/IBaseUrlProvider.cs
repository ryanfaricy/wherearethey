namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Provides the base URL of the application.
/// </summary>
public interface IBaseUrlProvider
{
    /// <summary>
    /// Gets the base URL of the application.
    /// </summary>
    /// <returns>The base URL string.</returns>
    string GetBaseUrl();
}
