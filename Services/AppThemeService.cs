using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public enum AppTheme
{
    Light,
    Dark,
    System
}

public class AppThemeService : IAppThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public event Action? OnThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            OnThemeChanged?.Invoke();
        }
    }
}
