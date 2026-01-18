using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing user feedback and bug reports.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Adds a new feedback entry.
    /// </summary>
    /// <param name="feedback">The feedback details.</param>
    Task AddFeedbackAsync(Feedback feedback);

    /// <summary>
    /// Gets all feedback entries for administrative review.
    /// </summary>
    /// <returns>A list of all feedback.</returns>
    Task<List<Feedback>> GetAllFeedbackAsync();

    /// <summary>
    /// Deletes a feedback entry.
    /// </summary>
    /// <param name="id">The internal ID of the feedback entry.</param>
    Task DeleteFeedbackAsync(int id);

    /// <summary>
    /// Updates a feedback entry.
    /// </summary>
    /// <param name="feedback">The updated feedback details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<Result> UpdateFeedbackAsync(Feedback feedback);
}
