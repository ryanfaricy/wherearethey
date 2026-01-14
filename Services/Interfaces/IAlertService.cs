using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface IAlertService
{
    Task<Alert> CreateAlertAsync(Alert alert, string email);
    Task SendVerificationEmailAsync(string email, string emailHash);
    Task<bool> VerifyEmailAsync(string token);
    string? DecryptEmail(string? encryptedEmail);
    Task<Alert?> GetAlertByExternalIdAsync(Guid externalId);
    Task<List<Alert>> GetActiveAlertsAsync(string? userIdentifier = null, bool onlyVerified = true);
    Task<bool> DeactivateAlertAsync(int id);
    Task<List<Alert>> GetMatchingAlertsAsync(double latitude, double longitude);
    Task<List<Alert>> GetAllAlertsAdminAsync();
    Task DeleteAlertAsync(int id);
}
