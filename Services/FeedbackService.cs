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
    ILogger<FeedbackService> logger,
    IValidator<Feedback> validator) : BaseService<Feedback>(contextFactory, eventService, logger, validator), IFeedbackService
{
    /// <inheritdoc />
    public async Task<Result<Feedback>> CreateFeedbackAsync(Feedback feedback)
    {
        Logger.LogInformation("Creating new feedback from user {UserIdentifier}", feedback.UserIdentifier);
        try
        {
            var validationResult = await Validator!.ValidateAsync(feedback);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Feedback validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result<Feedback>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            Logger.LogInformation("Feedback {FeedbackId} created successfully", feedback.Id);
            EventService.NotifyEntityChanged(feedback, EntityChangeType.Added);
            return Result<Feedback>.Success(feedback);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating feedback");
            return Result<Feedback>.Failure($"An error occurred while creating feedback: {ex.Message}");
        }
    }
}
