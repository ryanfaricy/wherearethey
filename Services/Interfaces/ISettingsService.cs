using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface ISettingsService
{
    Task<SystemSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(SystemSettings settings);
}
