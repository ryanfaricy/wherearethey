using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Validators;

/// <summary>
/// Validator for the <see cref="Alert"/> model.
/// Enforces business rules such as cooldowns, limits, and content restrictions.
/// </summary>
public class AlertValidator : AbstractValidator<Alert>
{
    public AlertValidator(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISettingsService settingsService,
        IStringLocalizer<App> l)
    {
        RuleFor(x => x.UserIdentifier)
            .NotEmpty().WithMessage(l["Identifier_Error"]);

        RuleFor(x => x.Message)
            .Must(m => string.IsNullOrEmpty(m) || (!m.Contains("http://") && !m.Contains("https://") && !m.Contains("www.")))
            .WithMessage(l["Links_Error"]);

        RuleFor(x => x)
            .CustomAsync(async (alert, context, cancellation) =>
            {
                var settings = await settingsService.GetSettingsAsync();

                // Alert limit check
                await using var dbContext = await contextFactory.CreateDbContextAsync(cancellation);
                var cutoff = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
                
                var recentCount = await dbContext.Alerts
                    .CountAsync(a => a.UserIdentifier == alert.UserIdentifier && a.CreatedAt >= cutoff, cancellation);

                if (recentCount >= settings.AlertLimitCount)
                {
                    context.AddFailure(nameof(alert.UserIdentifier), string.Format(l["Alert_Cooldown_Error"], settings.AlertLimitCount, settings.ReportCooldownMinutes));
                }
            });
    }
}
