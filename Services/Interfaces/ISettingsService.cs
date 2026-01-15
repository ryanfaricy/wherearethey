using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface ISettingsService
{
    Task<SystemSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(SystemSettings settings);
}
