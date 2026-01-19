using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Validators;

/// <summary>
/// Validator for the <see cref="Feedback"/> model.
/// </summary>
public class FeedbackValidator : AbstractValidator<Feedback>
{
    public FeedbackValidator(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISettingsService settingsService,
        IStringLocalizer<App> l)
    {
        RuleFor(x => x.UserIdentifier)
            .NotEmpty().WithMessage(l["Identifier_Error"]);

        RuleFor(x => x.Message)
            .Must(m => string.IsNullOrEmpty(m) || m.StartsWith("[AUTO-REPORTED]") || (!m.Contains("http://") && !m.Contains("https://") && !m.Contains("www.")))
            .WithMessage(l["Feedback_Links_Error"]);

        RuleFor(x => x)
            .CustomAsync(async (feedback, context, cancellation) =>
            {
                if (feedback.Message.StartsWith("[AUTO-REPORTED]"))
                {
                    return;
                }

                var settings = await settingsService.GetSettingsAsync();

                // Cooldown check
                await using var dbContext = await contextFactory.CreateDbContextAsync(cancellation);
                var cutoff = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
                
                var hasRecent = await dbContext.Feedbacks
                    .AnyAsync(f => f.UserIdentifier == feedback.UserIdentifier && f.CreatedAt >= cutoff, cancellation);

                if (hasRecent)
                {
                    context.AddFailure(nameof(feedback.UserIdentifier), string.Format(l["Feedback_Cooldown_Error"], settings.ReportCooldownMinutes));
                }
            });
    }
}
