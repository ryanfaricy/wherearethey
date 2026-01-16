using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public enum AppTheme
{
    Light,
    Dark,
    System
}

/// <inheritdoc />
public class AppThemeService(IEventService eventService) : IAppThemeService
{
    /// <inheritdoc />
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    /// <inheritdoc />
    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            eventService.NotifyThemeChanged();
        }
    }
}
