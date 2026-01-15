using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IAdminService
{
    Task<bool> LoginAsync(string password, string? ipAddress);
    Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50);
}
