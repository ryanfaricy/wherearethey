using Square.Models;
using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for processing and recording donations.
/// </summary>
public interface IDonationService
{
    /// <summary>
    /// Processes a payment through Square.
    /// </summary>
    /// <param name="amount">The donation amount.</param>
    /// <param name="sourceId">The payment source identifier from Square.</param>
    /// <returns>The response from the Square API.</returns>
    Task<CreatePaymentResponse> CreateSquarePaymentAsync(decimal amount, string sourceId);

    /// <summary>
    /// Records a new donation in the system.
    /// </summary>
    /// <param name="donation">The donation details.</param>
    /// <returns>A Result containing the recorded donation or an error message.</returns>
    Task<Result<Donation>> CreateDonationAsync(Donation donation);

    /// <summary>
    /// Updates the status of a donation.
    /// </summary>
    /// <param name="paymentId">The external payment identifier.</param>
    /// <param name="status">The new status.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateDonationStatusAsync(string paymentId, string status);

    /// <summary>
    /// Gets a list of all donations.
    /// </summary>
    /// <returns>A list of donations.</returns>
    Task<List<Donation>> GetAllDonationsAsync();

    /// <summary>
    /// Updates a donation's details.
    /// </summary>
    /// <param name="donation">The updated donation.</param>
    /// <returns>The result of the update operation.</returns>
    Task<Result> UpdateDonationAsync(Donation donation);

    /// <summary>
    /// Soft deletes a donation.
    /// </summary>
    /// <param name="id">The donation identifier.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<Result> DeleteDonationAsync(int id);
}
