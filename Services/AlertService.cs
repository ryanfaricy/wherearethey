using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
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
    IEmailTemplateService emailTemplateService,
    ILogger<AlertService> logger,
    IValidator<Alert> validator) : BaseService<Alert>(contextFactory, eventService, logger, validator), IAlertService
{
    private readonly IDataProtector _protector = provider.CreateProtector("WhereAreThey.Alerts.Email");

    /// <inheritdoc />
    public virtual async Task<Result<Alert>> CreateAlertAsync(Alert alert, string email)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["UserIdentifier"] = alert.UserIdentifier });
        logger.LogInformation("Creating new alert at {Latitude}, {Longitude}", alert.Latitude, alert.Longitude);
        try
        {
            var validationResult = await Validator!.ValidateAsync(alert);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Alert validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result<Alert>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            if (alert.RadiusKm > 160.9)
            {
                logger.LogInformation("Capping alert radius to 160.9km (was {RadiusKm})", alert.RadiusKm);
                alert.RadiusKm = 160.9;
            }
            
            var emailProvided = !string.IsNullOrWhiteSpace(email);
            var isVerified = false;

            if (emailProvided)
            {
                var emailHash = HashUtils.ComputeHash(email);
                isVerified = await context.EmailVerifications
                    .AnyAsync(v => v.EmailHash == emailHash && v.VerifiedAt != null);

                alert.EncryptedEmail = _protector.Protect(email);
                alert.EmailHash = emailHash;
                logger.LogDebug("Email provided for alert, verified: {IsVerified}", isVerified);
            }
            else
            {
                alert.EncryptedEmail = null;
                alert.EmailHash = null;
            }

            // Email hash is already set above
            
            alert.ExternalId = Guid.NewGuid();
            alert.IsVerified = isVerified;
            alert.CreatedAt = DateTime.UtcNow;
            alert.DeletedAt = null;
            
            context.Alerts.Add(alert);
            await context.SaveChangesAsync();

            logger.LogInformation("Created alert {AlertId} with ExternalId {ExternalId}", alert.Id, alert.ExternalId);
            EventService.NotifyEntityChanged(alert, EntityChangeType.Added);
            
            if (emailProvided && !isVerified)
            {
                var baseUrl = baseUrlProvider.GetBaseUrl();
                var emailHash = alert.EmailHash!;
                logger.LogInformation("Enqueuing verification email for {EmailHash}", emailHash);
                backgroundJobClient.Enqueue<IAlertService>(service => service.SendVerificationEmailAsync(email, emailHash, baseUrl));
            }

            return Result<Alert>.Success(alert);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating alert");
            return Result<Alert>.Failure("An error occurred while creating the alert.");
        }
    }


    /// <inheritdoc />
    public async Task<Result> SendVerificationEmailAsync(string email, string emailHash, string? baseUrl = null)
    {
        logger.LogInformation("Sending verification email to {EmailHash}", emailHash);
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var verification = await context.EmailVerifications
                .FirstOrDefaultAsync(v => v.EmailHash == emailHash);

            if (verification == null)
            {
                logger.LogDebug("No existing verification found for {EmailHash}, creating new one", emailHash);
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
                logger.LogInformation("Email {EmailHash} is already verified", emailHash);
                return Result.Success(); // Already verified
            }

            var actualBaseUrl = baseUrl ?? baseUrlProvider.GetBaseUrl();
            var verificationLink = $"{actualBaseUrl.TrimEnd('/')}/verify-email?token={verification.Token}";

            var subject = "Verify your email for alerts";
            var viewModel = new VerificationEmailViewModel { VerificationLink = verificationLink };
            var body = await emailTemplateService.RenderTemplateAsync("VerificationEmail", viewModel);

            logger.LogDebug("Dispatching email to {EmailHash} with token {Token}", emailHash, verification.Token);
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
        logger.LogInformation("Verifying email with token {Token}", token);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var verification = await context.EmailVerifications
            .FirstOrDefaultAsync(v => v.Token == token);

        if (verification == null)
        {
            logger.LogWarning("Verification failed: invalid token {Token}", token);
            return Result.Failure("Invalid verification token.");
        }

        if (verification.VerifiedAt != null)
        {
            logger.LogInformation("Token {Token} was already verified at {VerifiedAt}", token, verification.VerifiedAt);
            return Result.Success();
        }

        verification.VerifiedAt = DateTime.UtcNow;
        
        // Mark all alerts with this email hash as verified
        var alerts = await context.Alerts
            .Where(a => a.EmailHash == verification.EmailHash)
            .ToListAsync();

        logger.LogInformation("Verified email hash {EmailHash}. Marking {AlertCount} alerts as verified.", verification.EmailHash, alerts.Count);
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
        logger.LogDebug("Retrieving alert by ExternalId {ExternalId}", externalId);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var alert = await context.Alerts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExternalId == externalId);

        if (alert == null)
        {
            logger.LogWarning("Alert with ExternalId {ExternalId} not found", externalId);
            return Result<Alert>.Failure("Alert not found.");
        }

        return Result<Alert>.Success(alert);
    }

    /// <inheritdoc />
    public virtual async Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true, bool includeDeleted = false)
    {
        logger.LogDebug("Retrieving active alerts for User {UserIdentifier} (onlyVerified: {OnlyVerified}, includeDeleted: {IncludeDeleted})", userIdentifier, onlyVerified, includeDeleted);
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

        var results = await query.ToListAsync();
        logger.LogDebug("Found {Count} active alerts", results.Count);
        return results;
    }


    /// <inheritdoc />
    public virtual async Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude)
    {
        logger.LogDebug("Searching for alerts matching coordinates {Latitude}, {Longitude}", latitude, longitude);
        // For performance, we use a bounding box first.
        // The maximum radius for any alert is 160.9 km (100 miles).
        const double maxRadiusKm = 160.9;
        var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(latitude, longitude, maxRadiusKm);

        await using var context = await ContextFactory.CreateDbContextAsync();
        var candidateAlerts = await context.Alerts
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && (a.IsVerified || a.UsePush))
            .Where(a => a.Latitude >= minLat && a.Latitude <= maxLat &&
                       a.Longitude >= minLon && a.Longitude <= maxLon)
            .ToListAsync();
            
        var matchingAlerts = candidateAlerts
            .Where(a => GeoUtils.CalculateDistance(latitude, longitude, a.Latitude, a.Longitude) <= a.RadiusKm)
            .ToList();

        logger.LogDebug("Found {Count} matching alerts among {CandidateCount} candidates in bounding box", matchingAlerts.Count, candidateAlerts.Count);
        return matchingAlerts;
    }


    /// <inheritdoc />
    public async Task<Result> AddPushSubscriptionAsync(WebPushSubscription subscription)
    {
        logger.LogInformation("Adding/updating push subscription for User {UserIdentifier} (Endpoint: {Endpoint})", subscription.UserIdentifier, subscription.Endpoint);
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var existing = await context.WebPushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);

            if (existing != null)
            {
                logger.LogDebug("Updating existing subscription {SubscriptionId}", existing.Id);
                existing.P256DH = subscription.P256DH;
                existing.Auth = subscription.Auth;
                existing.UserIdentifier = subscription.UserIdentifier;
                existing.DeletedAt = null;
            }
            else
            {
                logger.LogDebug("Creating new push subscription");
                subscription.CreatedAt = DateTime.UtcNow;
                context.WebPushSubscriptions.Add(subscription);
            }

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding push subscription for endpoint {Endpoint}", subscription.Endpoint);
            return Result.Failure("An error occurred while saving the push subscription.");
        }
    }

    /// <inheritdoc />
    public async Task<List<WebPushSubscription>> GetPushSubscriptionsAsync(string userIdentifier)
    {
        logger.LogDebug("Retrieving push subscriptions for User {UserIdentifier}", userIdentifier);
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.WebPushSubscriptions
            .AsNoTracking()
            .Where(s => s.UserIdentifier == userIdentifier && s.DeletedAt == null)
            .ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<Result> UpdateAlertAsync(Alert alert, string? email = null)
    {
        logger.LogInformation("Updating alert {AlertId}", alert.Id);
        try
        {
            var validationResult = await Validator!.ValidateAsync(alert);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Alert validation failed for update: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();

            if (!string.IsNullOrEmpty(email))
            {
                var emailHash = HashUtils.ComputeHash(email);
                alert.EncryptedEmail = _protector.Protect(email);
                alert.EmailHash = emailHash;

                var isVerified = await context.EmailVerifications
                    .AnyAsync(v => v.EmailHash == emailHash && v.VerifiedAt != null);

                alert.IsVerified = isVerified;

                if (isVerified)
                {
                    logger.LogDebug("Email for alert is already verified");
                    return await UpdateInternalAsync(alert);
                }

                logger.LogInformation("Email changed or not verified for alert {AlertId}. Sending verification email.", alert.Id);
                var baseUrl = baseUrlProvider.GetBaseUrl();
                backgroundJobClient.Enqueue<IAlertService>(service => service.SendVerificationEmailAsync(email, emailHash, baseUrl));
            }
            return await UpdateInternalAsync(alert);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating alert {AlertId}", alert.Id);
            return Result.Failure("An error occurred while updating the alert.");
        }
    }
}
