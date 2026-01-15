using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using MediatR;
using WhereAreThey.Events;

namespace WhereAreThey.Services;

public class AlertService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IDataProtectionProvider provider,
    IEmailService emailService,
    IMediator mediator,
    IAdminNotificationService adminNotificationService,
    IConfiguration configuration,
    ILogger<AlertService> logger,
    ISettingsService settingsService,
    IValidator<Alert> validator,
    IStringLocalizer<App> L) : IAlertService
{
    private readonly IDataProtector _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");

    public virtual async Task<Alert> CreateAlertAsync(Alert alert, string email)
    {
        try
        {
            await validator.ValidateAndThrowAsync(alert);

            await using var context = await contextFactory.CreateDbContextAsync();

            if (alert.RadiusKm > 160.9)
            {
                alert.RadiusKm = 160.9;
            }
            
            var emailHash = ComputeHash(email);
            var isVerified = await context.EmailVerifications
                .AnyAsync(v => v.EmailHash == emailHash && v.VerifiedAt != null);

            alert.ExternalId = Guid.NewGuid();
            alert.EncryptedEmail = _protector.Protect(email);
            alert.EmailHash = emailHash;
            alert.IsVerified = isVerified;
            alert.CreatedAt = DateTime.UtcNow;
            alert.IsActive = true;
            
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();

            adminNotificationService.NotifyAlertAdded(alert);
            await mediator.Publish(new AlertCreatedEvent(alert, email));

            return alert;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating alert for email hash {EmailHash}", ComputeHash(email));
            throw;
        }
    }

    public static string ComputeHash(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public async Task SendVerificationEmailAsync(string email, string emailHash)
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

            var baseUrl = configuration["BaseUrl"] ?? "https://www.aretheyhere.com";
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
        adminNotificationService.NotifyEmailVerified(verification.EmailHash);
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

    public async Task<Alert?> GetAlertByExternalIdAsync(Guid externalId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExternalId == externalId);
    }

    public virtual async Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Alerts
            .AsNoTracking()
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
        // For performance, we use a bounding box first.
        // The maximum radius for any alert is 160.9 km (100 miles).
        const double maxRadiusKm = 160.9;
        var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(latitude, longitude, maxRadiusKm);

        await using var context = await contextFactory.CreateDbContextAsync();
        var candidateAlerts = await context.Alerts
            .AsNoTracking()
            .Where(a => a.IsActive && a.IsVerified && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
            .Where(a => a.Latitude >= minLat && a.Latitude <= maxLat &&
                       a.Longitude >= minLon && a.Longitude <= maxLon)
            .ToListAsync();
            
        return candidateAlerts
            .Where(a => GeoUtils.CalculateDistance(latitude, longitude, a.Latitude, a.Longitude) <= a.RadiusKm)
            .ToList();
    }

    // Admin methods
    public virtual async Task<List<Alert>> GetAllAlertsAdminAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Alerts
            .AsNoTracking()
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
            adminNotificationService.NotifyAlertDeleted(id);
        }
    }
}
