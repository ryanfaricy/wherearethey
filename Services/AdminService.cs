using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class AdminService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IAdminNotificationService adminNotificationService,
    IConfiguration configuration) : IAdminService
{
    public async Task<bool> LoginAsync(string password, string? ipAddress)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Check for brute force: more than 5 failed attempts from this IP in the last 15 minutes
        var lockoutThreshold = DateTime.UtcNow.AddMinutes(-15);
        var failedAttempts = await context.AdminLoginAttempts
            .AsNoTracking()
            .CountAsync(a => a.IpAddress == ipAddress && a.Timestamp >= lockoutThreshold && !a.IsSuccessful);

        if (failedAttempts >= 5)
        {
            await RecordAttempt(ipAddress, false);
            throw new InvalidOperationException("Too many failed login attempts from this IP. Please try again in 15 minutes.");
        }

        var adminPassword = configuration["AdminPassword"];
        var isSuccessful = false;
        if (!string.IsNullOrEmpty(adminPassword))
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var adminPasswordBytes = Encoding.UTF8.GetBytes(adminPassword);
            
            if (passwordBytes.Length == adminPasswordBytes.Length)
            {
                isSuccessful = CryptographicOperations.FixedTimeEquals(passwordBytes, adminPasswordBytes);
            }
            else
            {
                // To prevent timing leaks of password length, always perform a comparison
                CryptographicOperations.FixedTimeEquals(adminPasswordBytes, adminPasswordBytes);
                isSuccessful = false;
            }
        }

        await RecordAttempt(ipAddress, isSuccessful);

        return isSuccessful;
    }

    private async Task RecordAttempt(string? ipAddress, bool isSuccessful)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var attempt = new AdminLoginAttempt
        {
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful
        };
        context.AdminLoginAttempts.Add(attempt);
        await context.SaveChangesAsync();
        adminNotificationService.NotifyAdminLoginAttempt(attempt);
    }

    public async Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AdminLoginAttempts
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
