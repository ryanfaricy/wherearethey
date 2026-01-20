using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc cref="BaseService{T}" />
public class FeedbackService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService,
    IValidator<Feedback> validator) : BaseService<Feedback>(contextFactory, eventService), IFeedbackService
{
    /// <inheritdoc />
    public async Task<Result<Feedback>> CreateFeedbackAsync(Feedback feedback)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(feedback);
            if (!validationResult.IsValid)
            {
                return Result<Feedback>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            EventService.NotifyEntityChanged(feedback, EntityChangeType.Added);
            return Result<Feedback>.Success(feedback);
        }
        catch (Exception ex)
        {
            return Result<Feedback>.Failure($"An error occurred while creating feedback: {ex.Message}");
        }
    }


    /// <inheritdoc />
    public async Task<Result> UpdateFeedbackAsync(Feedback feedback)
    {
        var validationResult = await validator.ValidateAsync(feedback);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult);
        }

        return await UpdateAsync(feedback);
    }
}
