using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class SubmissionValidator(IDbContextFactory<ApplicationDbContext> contextFactory, IStringLocalizer<App> L) : ISubmissionValidator
{
    public void ValidateIdentifier(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new InvalidOperationException(L["Identifier_Error"]);
        }
    }

    public void ValidateNoLinks(string? message, string errorKey)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (message.Contains("http://") || message.Contains("https://") || message.Contains("www."))
            {
                throw new InvalidOperationException(L[errorKey]);
            }
        }
    }

    public async Task ValidateLocationReportCooldownAsync(string? identifier, int cooldownMinutes)
    {
        ValidateIdentifier(identifier);
        await using var context = await contextFactory.CreateDbContextAsync();
        var cutoff = DateTime.UtcNow.AddMinutes(-cooldownMinutes);
        
        var hasRecent = await context.LocationReports
            .AnyAsync(r => r.ReporterIdentifier == identifier && r.Timestamp >= cutoff);

        if (hasRecent)
        {
            throw new InvalidOperationException(string.Format(L["Cooldown_Error"], cooldownMinutes));
        }
    }

    public async Task ValidateFeedbackCooldownAsync(string? identifier, int cooldownMinutes)
    {
        ValidateIdentifier(identifier);
        await using var context = await contextFactory.CreateDbContextAsync();
        var cutoff = DateTime.UtcNow.AddMinutes(-cooldownMinutes);
        
        var hasRecent = await context.Feedbacks
            .AnyAsync(f => f.UserIdentifier == identifier && f.Timestamp >= cutoff);

        if (hasRecent)
        {
            throw new InvalidOperationException(string.Format(L["Feedback_Cooldown_Error"], cooldownMinutes));
        }
    }

    public async Task ValidateAlertLimitAsync(string? identifier, int cooldownMinutes, int maxCount)
    {
        ValidateIdentifier(identifier);
        await using var context = await contextFactory.CreateDbContextAsync();
        var cutoff = DateTime.UtcNow.AddMinutes(-cooldownMinutes);
        
        var recentCount = await context.Alerts
            .CountAsync(a => a.UserIdentifier == identifier && a.CreatedAt >= cutoff);

        if (recentCount >= maxCount)
        {
            throw new InvalidOperationException(string.Format(L["Alert_Cooldown_Error"], maxCount, cooldownMinutes));
        }
    }
}
