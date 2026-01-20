using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing user feedback and bug reports.
/// </summary>
public interface IFeedbackService : IAdminDataService<Feedback>
{
    /// <summary>
    /// Creates a new feedback entry.
    /// </summary>
    /// <param name="feedback">The feedback details.</param>
    /// <returns>A Result containing the created feedback or an error message.</returns>
    Task<Result<Feedback>> CreateFeedbackAsync(Feedback feedback);


    /// <summary>
    /// Updates a feedback entry.
    /// </summary>
    /// <param name="feedback">The updated feedback details.</param>
    /// <returns>The result of the update operation.</returns>
    Task<Result> UpdateFeedbackAsync(Feedback feedback);
}
