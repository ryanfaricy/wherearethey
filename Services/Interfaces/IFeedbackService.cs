using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing user feedback and bug reports.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Creates a new feedback entry.
    /// </summary>
    /// <param name="feedback">The feedback details.</param>
    /// <returns>A Result containing the created feedback or an error message.</returns>
    Task<Result<Feedback>> CreateFeedbackAsync(Feedback feedback);

    /// <summary>
    /// Gets all feedback entries for administrative review.
    /// </summary>
    /// <returns>A list of all feedback.</returns>
    Task<List<Feedback>> GetAllFeedbackAsync();

    /// <summary>
    /// Deletes a feedback entry.
    /// </summary>
    /// <param name="id">The internal ID of the feedback entry.</param>
    /// <param name="hardDelete">Whether to permanently delete the feedback (Admin only).</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteFeedbackAsync(int id, bool hardDelete = false);

    /// <summary>
    /// Deletes multiple feedback entries.
    /// </summary>
    /// <param name="ids">The internal IDs of the feedback entries.</param>
    /// <param name="hardDelete">Whether to permanently delete the feedback (Admin only).</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteFeedbacksAsync(IEnumerable<int> ids, bool hardDelete = false);

    /// <summary>
    /// Updates a feedback entry.
    /// </summary>
    /// <param name="feedback">The updated feedback details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<Result> UpdateFeedbackAsync(Feedback feedback);
}
