namespace WhereAreThey.Services.Interfaces;

public interface IAppThemeService
{
    AppTheme CurrentTheme { get; }
    event Action? OnThemeChanged;
    void SetTheme(AppTheme theme);
}
