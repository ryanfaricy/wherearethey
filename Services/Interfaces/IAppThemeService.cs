namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing the application's visual theme.
/// </summary>
public interface IAppThemeService
{
    /// <summary>
    /// Gets the current application theme.
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    void SetTheme(AppTheme theme);
}
