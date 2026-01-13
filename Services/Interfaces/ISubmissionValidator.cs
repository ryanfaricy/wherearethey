using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface ISubmissionValidator
{
    void ValidateIdentifier(string? identifier);
    void ValidateNoLinks(string? message, string errorKey);
    Task ValidateLocationReportCooldownAsync(string? identifier, int cooldownMinutes);
    Task ValidateFeedbackCooldownAsync(string? identifier, int cooldownMinutes);
    Task ValidateAlertLimitAsync(string? identifier, int cooldownMinutes, int maxCount);
}
