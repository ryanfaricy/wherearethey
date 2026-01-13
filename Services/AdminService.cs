using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AdminService(IDbContextFactory<ApplicationDbContext> contextFactory, IConfiguration configuration)
{
    public async Task<bool> LoginAsync(string password, string? ipAddress)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Check for brute force: more than 5 failed attempts from this IP in the last 15 minutes
        var lockoutThreshold = DateTime.UtcNow.AddMinutes(-15);
        var failedAttempts = await context.AdminLoginAttempts
            .CountAsync(a => a.IpAddress == ipAddress && a.Timestamp >= lockoutThreshold && !a.IsSuccessful);

        if (failedAttempts >= 5)
        {
            await RecordAttempt(ipAddress, false);
            throw new InvalidOperationException("Too many failed login attempts from this IP. Please try again in 15 minutes.");
        }

        var adminPassword = configuration["AdminPassword"];
        var isSuccessful = !string.IsNullOrEmpty(adminPassword) && password == adminPassword;

        await RecordAttempt(ipAddress, isSuccessful);

        return isSuccessful;
    }

    private async Task RecordAttempt(string? ipAddress, bool isSuccessful)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.AdminLoginAttempts.Add(new AdminLoginAttempt
        {
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AdminLoginAttempts
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
