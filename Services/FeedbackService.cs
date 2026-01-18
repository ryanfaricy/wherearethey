using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class FeedbackService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService,
    IValidator<Feedback> validator) : IFeedbackService
{
    /// <inheritdoc />
    public async Task AddFeedbackAsync(Feedback feedback)
    {
        await validator.ValidateAndThrowAsync(feedback);

        await using var context = await contextFactory.CreateDbContextAsync();
        feedback.Timestamp = DateTime.UtcNow;
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        eventService.NotifyFeedbackAdded(feedback);
    }

    /// <inheritdoc />
    public async Task<List<Feedback>> GetAllFeedbackAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Feedbacks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFeedbackAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var feedback = await context.Feedbacks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id);
        if (feedback != null)
        {
            feedback.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            eventService.NotifyFeedbackUpdated(feedback);
            eventService.NotifyFeedbackDeleted(id);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFeedbackAsync(Feedback feedback)
    {
        try
        {
            await validator.ValidateAndThrowAsync(feedback);
            await using var context = await contextFactory.CreateDbContextAsync();
            var existing = await context.Feedbacks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == feedback.Id);
            if (existing == null)
            {
                return Result.Failure("Feedback not found.");
            }

            existing.Type = feedback.Type;
            existing.Message = feedback.Message;
            existing.UserIdentifier = feedback.UserIdentifier;
            existing.DeletedAt = feedback.DeletedAt;

            await context.SaveChangesAsync();
            eventService.NotifyFeedbackUpdated(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while updating feedback: {ex.Message}");
        }
    }
}
