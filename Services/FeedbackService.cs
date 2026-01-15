using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class FeedbackService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IAdminNotificationService adminNotificationService,
    IValidator<Feedback> validator) : IFeedbackService
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
