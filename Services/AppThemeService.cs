using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public enum AppTheme
{
    Light,
    Dark,
    System,
}

/// <inheritdoc />
public class AppThemeService(IEventService eventService, ILogger<AppThemeService> logger) : IAppThemeService
{
    /// <inheritdoc />
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    /// <inheritdoc />
    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme)
        {
            return;
        }
        
        logger.LogInformation("Changing application theme to {Theme}", theme);
        CurrentTheme = theme;
        eventService.NotifyThemeChanged();
    }
}
