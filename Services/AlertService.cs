using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using System.Security.Cryptography;
using System.Text;

namespace WhereAreThey.Services;

public class AlertService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IDataProtectionProvider provider,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<AlertService> logger)
{
    private readonly IDataProtector _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");

    public virtual async Task<Alert> CreateAlertAsync(Alert alert, string email)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        if (alert.RadiusKm > 160.9)
        {
            alert.RadiusKm = 160.9;
        }
        
        var emailHash = ComputeHash(email);
        var isVerified = await context.EmailVerifications
            .AnyAsync(v => v.EmailHash == emailHash && v.VerifiedAt != null);

        alert.EncryptedEmail = _protector.Protect(email);
        alert.EmailHash = emailHash;
        alert.IsVerified = isVerified;
        alert.CreatedAt = DateTime.UtcNow;
        alert.IsActive = true;
        
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        if (!isVerified)
        {
            await SendVerificationEmailAsync(email, emailHash);
        }

        return alert;
    }

    private static string ComputeHash(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private async Task SendVerificationEmailAsync(string email, string emailHash)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var verification = await context.EmailVerifications
                .FirstOrDefaultAsync(v => v.EmailHash == emailHash);

            if (verification == null)
            {
                verification = new EmailVerification
                {
                    EmailHash = emailHash,
                    Token = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow
                };
                context.EmailVerifications.Add(verification);
                await context.SaveChangesAsync();
            }
            else if (verification.VerifiedAt != null)
            {
                return; // Already verified
            }

            var baseUrl = configuration["BaseUrl"] ?? "https://aretheyhere.com";
            var verificationLink = $"{baseUrl}/verify-email?token={verification.Token}";

            var subject = "Verify your email for alerts";
            var body = $@"
                <h3>Verify your email address</h3>
                <p>Someone (hopefully you) signed up for alerts on AreTheyHere using this email address.</p>
                <p>To receive alert notifications, please verify your email by clicking the link below:</p>
                <p><a href='{verificationLink}'>Verify Email Address</a></p>
                <p>If you didn't sign up for these alerts, you can safely ignore this email.</p>
                <hr/>
                <small>AreTheyHere - Know where they are.</small>";

            await emailService.SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification email to {Email}", email);
        }
    }

    public virtual async Task<bool> VerifyEmailAsync(string token)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var verification = await context.EmailVerifications
            .FirstOrDefaultAsync(v => v.Token == token);

        if (verification == null)
        {
            return false;
        }

        if (verification.VerifiedAt != null)
        {
            return true;
        }

        verification.VerifiedAt = DateTime.UtcNow;
        
        // Mark all alerts with this email hash as verified
        var alerts = await context.Alerts
            .Where(a => a.EmailHash == verification.EmailHash)
            .ToListAsync();

        foreach (var alert in alerts)
        {
            alert.IsVerified = true;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public virtual string? DecryptEmail(string? encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail)) return null;
        try
        {
            return _protector.Unprotect(encryptedEmail);
        }
        catch
        {
            return null;
        }
    }

    public virtual async Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Alerts
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow));

        if (onlyVerified)
        {
            query = query.Where(a => a.IsVerified);
        }

        if (!string.IsNullOrEmpty(userIdentifier))
        {
            query = query.Where(a => a.UserIdentifier == userIdentifier);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<bool> DeactivateAlertAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var alert = await context.Alerts.FindAsync(id);
        if (alert == null) return false;

        alert.IsActive = false;
        await context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude)
    {
        // For performance, we could use a bounding box first if we had thousands of alerts
        // But for now, we'll fetch active ones and filter in memory as the number of active alerts is likely manageable
        var activeAlerts = await GetActiveAlertsAsync(onlyVerified: true);
        
        return activeAlerts
            .Where(a => GeoUtils.CalculateDistance(latitude, longitude, a.Latitude, a.Longitude) <= a.RadiusKm)
            .ToList();
    }

    // Admin methods
    public virtual async Task<List<Alert>> GetAllAlertsAdminAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public virtual async Task DeleteAlertAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var alert = await context.Alerts.FindAsync(id);
        if (alert != null)
        {
            context.Alerts.Remove(alert);
            await context.SaveChangesAsync();
        }
    }
}
