using WhereAreThey.Models;
using Square.Models;

namespace WhereAreThey.Services;

public interface IDonationService
{
    Task<CreatePaymentResponse> CreateSquarePaymentAsync(decimal amount, string sourceId);
    Task<Donation> RecordDonationAsync(Donation donation);
    Task<bool> UpdateDonationStatusAsync(string paymentId, string status);
    Task<List<Donation>> GetAllDonationsAsync();
}
