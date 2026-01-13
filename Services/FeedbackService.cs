using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Services;

public class FeedbackService(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    public async Task AddFeedbackAsync(Feedback feedback)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Anti-spam: check cooldown (5 minutes)
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        bool hasRecentFeedback = false;

        if (!string.IsNullOrEmpty(feedback.UserIdentifier))
        {
            hasRecentFeedback = await context.Feedbacks
                .AnyAsync(f => f.UserIdentifier == feedback.UserIdentifier && f.Timestamp >= fiveMinutesAgo);
        }

        if (hasRecentFeedback)
        {
            throw new InvalidOperationException("You can only submit one feedback every five minutes.");
        }

        // Anti-spam: basic message validation
        if (!string.IsNullOrEmpty(feedback.Message))
        {
            if (feedback.Message.Contains("http://") || feedback.Message.Contains("https://") || feedback.Message.Contains("www."))
            {
                throw new InvalidOperationException("Links are not allowed in feedback to prevent spam.");
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
