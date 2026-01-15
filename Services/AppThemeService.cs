using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public enum AppTheme
{
    Light,
    Dark,
    System
}

public class AppThemeService(IEventService eventService) : IAppThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            eventService.NotifyThemeChanged();
        }
    }
}
