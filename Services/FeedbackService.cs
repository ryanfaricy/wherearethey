using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class FeedbackService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ISettingsService settingsService,
    IAdminNotificationService adminNotificationService,
    IValidator<Feedback> validator,
    IStringLocalizer<App> L) : IFeedbackService
{
    public event Action<Feedback>? OnFeedbackAdded;

    public async Task AddFeedbackAsync(Feedback feedback)
    {
        await validator.ValidateAndThrowAsync(feedback);

        await using var context = await contextFactory.CreateDbContextAsync();
        feedback.Timestamp = DateTime.UtcNow;
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        OnFeedbackAdded?.Invoke(feedback);
        adminNotificationService.NotifyFeedbackAdded(feedback);
    }

    public async Task<List<Feedback>> GetAllFeedbackAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Feedbacks
            .AsNoTracking()
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
            adminNotificationService.NotifyFeedbackDeleted(id);
        }
    }
}
