using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Validators;

public class FeedbackValidator : AbstractValidator<Feedback>
{
    public FeedbackValidator(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISettingsService settingsService,
        IStringLocalizer<App> L)
    {
        RuleFor(x => x.UserIdentifier)
            .NotEmpty().WithMessage(L["Identifier_Error"]);

        RuleFor(x => x.Message)
            .Must(m => string.IsNullOrEmpty(m) || (!m.Contains("http://") && !m.Contains("https://") && !m.Contains("www.")))
            .WithMessage(L["Feedback_Links_Error"]);

        RuleFor(x => x)
            .CustomAsync(async (feedback, context, cancellation) =>
            {
                var settings = await settingsService.GetSettingsAsync();

                // Cooldown check
                await using var dbContext = await contextFactory.CreateDbContextAsync(cancellation);
                var cutoff = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
                
                var hasRecent = await dbContext.Feedbacks
                    .AnyAsync(f => f.UserIdentifier == feedback.UserIdentifier && f.Timestamp >= cutoff, cancellation);

                if (hasRecent)
                {
                    context.AddFailure(nameof(feedback.UserIdentifier), string.Format(L["Feedback_Cooldown_Error"], settings.ReportCooldownMinutes));
                }
            });
    }
}
