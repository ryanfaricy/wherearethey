using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class FeedbackService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    SettingsService settingsService,
    IStringLocalizer<App> L)
{
    public async Task AddFeedbackAsync(Feedback feedback)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettingsAsync();

        // Anti-spam: check cooldown
        var cooldownLimit = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
        bool hasRecentFeedback = false;

        if (!string.IsNullOrEmpty(feedback.UserIdentifier))
        {
            hasRecentFeedback = await context.Feedbacks
                .AnyAsync(f => f.UserIdentifier == feedback.UserIdentifier && f.Timestamp >= cooldownLimit);
        }

        if (hasRecentFeedback)
        {
            throw new InvalidOperationException(string.Format(L["Feedback_Cooldown_Error"], settings.ReportCooldownMinutes));
        }

        // Anti-spam: basic message validation
        if (!string.IsNullOrEmpty(feedback.Message))
        {
            if (feedback.Message.Contains("http://") || feedback.Message.Contains("https://") || feedback.Message.Contains("www."))
            {
                throw new InvalidOperationException(L["Feedback_Links_Error"]);
            }
        }

        feedback.Timestamp = DateTime.UtcNow;
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();
    }

    public async Task<List<Feedback>> GetAllFeedbackAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Feedbacks
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync();
    }

    public async Task DeleteFeedbackAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var feedback = await context.Feedbacks.FindAsync(id);
        if (feedback != null)
        {
            context.Feedbacks.Remove(feedback);
            await context.SaveChangesAsync();
        }
    }
}
