using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface IFeedbackService
{
    event Action<Feedback>? OnFeedbackAdded;
    Task AddFeedbackAsync(Feedback feedback);
    Task<List<Feedback>> GetAllFeedbackAsync();
    Task DeleteFeedbackAsync(int id);
}
