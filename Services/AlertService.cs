using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc cref="BaseService{T}" />
public class AlertService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IDataProtectionProvider provider,
    IEmailService emailService,
    IBackgroundJobClient backgroundJobClient,
    IEventService eventService,
    IBaseUrlProvider baseUrlProvider,
    IOptions<AppOptions> appOptions,
    IEmailTemplateService emailTemplateService,
    ILogger<AlertService> logger,
    IValidator<Alert> validator) : BaseService<Alert>(contextFactory, eventService), IAlertService
{
    private readonly IDataProtector _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");

    /// <inheritdoc />
    public virtual async Task<Result<Alert>> CreateAlertAsync(Alert alert, string email)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(alert);
            if (!validationResult.IsValid)
            {
                return Result<Alert>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            if (alert.RadiusKm > 160.9)
            {
                alert.RadiusKm = 160.9;
            }
            
            var emailHash = HashUtils.ComputeHash(email);
            var isVerified = await context.EmailVerifications
                .AnyAsync(v => v.EmailHash == emailHash && v.VerifiedAt != null);

            alert.ExternalId = Guid.NewGuid();
            alert.EncryptedEmail = _protector.Protect(email);
            alert.EmailHash = emailHash;
            alert.IsVerified = isVerified;
            alert.CreatedAt = DateTime.UtcNow;
            alert.DeletedAt = null;
            
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();

            EventService.NotifyEntityChanged(alert, EntityChangeType.Added);
            
            if (!alert.IsVerified)
            {
                var baseUrl = baseUrlProvider.GetBaseUrl();
                backgroundJobClient.Enqueue<IAlertService>(service => service.SendVerificationEmailAsync(email, emailHash, baseUrl));
            }

            return Result<Alert>.Success(alert);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating alert for email hash {EmailHash}", HashUtils.ComputeHash(email));
            return Result<Alert>.Failure("An error occurred while creating the alert.");
        }
    }


    /// <inheritdoc />
    public async Task<Result> SendVerificationEmailAsync(string email, string emailHash, string? baseUrl = null)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var verification = await context.EmailVerifications
                .FirstOrDefaultAsync(v => v.EmailHash == emailHash);

            if (verification == null)
            {
                verification = new EmailVerification
                {
                    EmailHash = emailHash,
                    Token = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow,
                };
                context.EmailVerifications.Add(verification);
                await context.SaveChangesAsync();
            }
            else if (verification.VerifiedAt != null)
            {
                return Result.Success(); // Already verified
            }

            var actualBaseUrl = baseUrl ?? appOptions.Value.BaseUrl;
            var verificationLink = $"{actualBaseUrl.TrimEnd('/')}/verify-email?token={verification.Token}";

            var subject = "Verify your email for alerts";
            var viewModel = new VerificationEmailViewModel { VerificationLink = verificationLink };
            var body = await emailTemplateService.RenderTemplateAsync("VerificationEmail", viewModel);

            await emailService.SendEmailAsync(email, subject, body);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification email to {Email}", email);
            return Result.Failure("Failed to send verification email.");
        }
    }

    /// <inheritdoc />
    public virtual async Task<Result> VerifyEmailAsync(string token)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var verification = await context.EmailVerifications
            .FirstOrDefaultAsync(v => v.Token == token);

        if (verification == null)
        {
            return Result.Failure("Invalid verification token.");
        }

        if (verification.VerifiedAt != null)
        {
            return Result.Success();
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
        
        foreach (var alert in alerts)
        {
            EventService.NotifyEntityChanged(alert, EntityChangeType.Updated);
        }

        EventService.NotifyEmailVerified(verification.EmailHash);
        return Result.Success();
    }

    /// <inheritdoc />
    public virtual string? DecryptEmail(string? encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(encryptedEmail);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<Alert>> GetAlertByExternalIdAsync(Guid externalId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var alert = await context.Alerts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExternalId == externalId);

        return alert != null ? Result<Alert>.Success(alert) : Result<Alert>.Failure("Alert not found.");
    }

    /// <inheritdoc />
    public virtual async Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true, bool includeDeleted = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = context.Alerts.AsNoTracking();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            query = query.Where(a => a.DeletedAt == null);
        }

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


    /// <inheritdoc />
    public virtual async Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude)
    {
        // For performance, we use a bounding box first.
        // The maximum radius for any alert is 160.9 km (100 miles).
        const double maxRadiusKm = 160.9;
        var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(latitude, longitude, maxRadiusKm);

        await using var context = await ContextFactory.CreateDbContextAsync();
        var candidateAlerts = await context.Alerts
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.IsVerified)
            .Where(a => a.Latitude >= minLat && a.Latitude <= maxLat &&
                       a.Longitude >= minLon && a.Longitude <= maxLon)
            .ToListAsync();
            
        return candidateAlerts
            .Where(a => GeoUtils.CalculateDistance(latitude, longitude, a.Latitude, a.Longitude) <= a.RadiusKm)
            .ToList();
    }

    // Admin methods
    /// <inheritdoc />
    public virtual async Task<List<Alert>> GetAllAlertsAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAlertAsync(int id, bool hardDelete = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var alert = await context.Alerts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (alert == null)
        {
            return Result.Failure("Alert not found.");
        }

        // If it's already deleted and we are an admin, we hard delete it.
        // OR if hardDelete flag is explicitly set.
        if (hardDelete || alert.DeletedAt != null)
        {
            return await HardDeleteAsync(id);
        }
        return await SoftDeleteAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<Result> UpdateAlertAsync(Alert alert, string? email = null)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(alert);
            if (!validationResult.IsValid)
            {
                return Result.Failure(validationResult);
            }

            // ReSharper disable once InvertIf
            if (!string.IsNullOrEmpty(email))
            {
                alert.EncryptedEmail = _protector.Protect(email);
                alert.EmailHash = HashUtils.ComputeHash(email);
            }

            return await UpdateAsync(alert);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating alert {AlertId}", alert.Id);
            return Result.Failure("An error occurred while updating the alert.");
        }
    }
}
