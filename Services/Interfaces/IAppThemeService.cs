namespace WhereAreThey.Services.Interfaces;

public interface IAppThemeService
{
    AppTheme CurrentTheme { get; }
    void SetTheme(AppTheme theme);
}
