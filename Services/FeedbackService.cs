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
    IValidator<Feedback> validator) : BaseService<Feedback>(contextFactory, eventService), IFeedbackService
{
    /// <inheritdoc />
    public async Task AddFeedbackAsync(Feedback feedback)
    {
        await validator.ValidateAndThrowAsync(feedback);

        await using var context = await ContextFactory.CreateDbContextAsync();
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        EventService.NotifyEntityChanged(feedback, EntityChangeType.Added);
    }

    /// <inheritdoc />
    public async Task<List<Feedback>> GetAllFeedbackAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task DeleteFeedbackAsync(int id)
    {
        await SoftDeleteAsync(id);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFeedbackAsync(Feedback feedback)
    {
        try
        {
            await validator.ValidateAndThrowAsync(feedback);
            await using var context = await ContextFactory.CreateDbContextAsync();
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
            EventService.NotifyEntityChanged(existing, EntityChangeType.Updated);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while updating feedback: {ex.Message}");
        }
    }
}
