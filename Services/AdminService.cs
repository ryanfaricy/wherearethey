using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class AdminService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IEventService eventService,
    ProtectedLocalStorage localStorage,
    IOptions<AppOptions> appOptions,
    ILogger<AdminService> logger) : IAdminService
{
    /// <inheritdoc />
    public event Action? OnAdminLogin;

    /// <inheritdoc />
    public event Action? OnAdminLogout;

    /// <inheritdoc />
    public void NotifyAdminLogin()
    {
        logger.LogInformation("Admin login notified");
        _isAdminCached = true;
        OnAdminLogin?.Invoke();
    }

    /// <inheritdoc />
    public void NotifyAdminLogout()
    {
        logger.LogInformation("Admin logout notified");
        _isAdminCached = false;
        OnAdminLogout?.Invoke();
    }

    private bool? _isAdminCached;

    /// <inheritdoc />
    public async Task<bool> IsAdminAsync()
    {
        if (_isAdminCached.HasValue)
        {
            return _isAdminCached.Value;
        }

        try
        {
            var result = await localStorage.GetAsync<DateTime>("lastAdminLogin");
            if (result.Success)
            {
                _isAdminCached = (DateTime.UtcNow - result.Value).TotalDays <= 7;
                logger.LogDebug("Admin status checked from storage: {IsAdmin}", _isAdminCached);
                return _isAdminCached.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "Could not retrieve admin status from storage");
            // Storage may not be available or entry may be invalid
        }
        return false;
    }

    /// <inheritdoc />
    public async Task<Result> LoginAsync(string password, string? ipAddress)
    {
        logger.LogInformation("Admin login attempt from IP {IpAddress}", ipAddress);
        await using var context = await contextFactory.CreateDbContextAsync();

        // Check for brute force: more than 5 failed attempts from this IP in the last 15 minutes
        var lockoutThreshold = DateTime.UtcNow.AddMinutes(-15);
        var failedAttempts = await context.AdminLoginAttempts
            .AsNoTracking()
            .CountAsync(a => a.IpAddress == ipAddress && a.CreatedAt >= lockoutThreshold && !a.IsSuccessful);

        if (failedAttempts >= 5)
        {
            logger.LogWarning("Admin login lockout for IP {IpAddress} (failed attempts: {Count})", ipAddress, failedAttempts);
            await RecordAttempt(ipAddress, false);
            return Result.Failure("Too many failed login attempts from this IP. Please try again in 15 minutes.");
        }

        var adminPassword = appOptions.Value.AdminPassword;
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

        if (isSuccessful)
        {
            logger.LogInformation("Successful admin login from IP {IpAddress}", ipAddress);
            return Result.Success();
        }
        
        logger.LogWarning("Failed admin login from IP {IpAddress}: Invalid password", ipAddress);
        return Result.Failure("Invalid password.");
    }

    private async Task RecordAttempt(string? ipAddress, bool isSuccessful)
    {
        logger.LogDebug("Recording admin login attempt from IP {IpAddress}, Successful: {IsSuccessful}", ipAddress, isSuccessful);
        await using var context = await contextFactory.CreateDbContextAsync();
        var attempt = new AdminLoginAttempt
        {
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful,
        };
        context.AdminLoginAttempts.Add(attempt);
        await context.SaveChangesAsync();
        eventService.NotifyAdminLoginAttempt(attempt);
    }

    /// <inheritdoc />
    public async Task<List<AdminLoginAttempt>> GetRecentLoginAttemptsAsync(int count = 50)
    {
        logger.LogDebug("Retrieving {Count} recent login attempts", count);
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AdminLoginAttempts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
