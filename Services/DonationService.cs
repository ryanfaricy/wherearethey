using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Square;
using Square.Models;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using Environment = Square.Environment;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class DonationService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IEventService eventService,
    IOptions<SquareOptions> squareOptions) : IDonationService
{
    private readonly SquareOptions _options = squareOptions.Value;
    private readonly ISquareClient _squareClient = new SquareClient.Builder()
        .AccessToken(squareOptions.Value.AccessToken)
        .Environment(squareOptions.Value.Environment == "Production" ? Environment.Production : Environment.Sandbox)
        .Build();

    /// <inheritdoc />
    public async Task<CreatePaymentResponse> CreateSquarePaymentAsync(decimal amount, string sourceId)
    {
        var amountMoney = new Money.Builder()
            .Amount((long)(amount * 100))
            .Currency("USD")
            .Build();

        var body = new CreatePaymentRequest.Builder(sourceId, Guid.NewGuid().ToString())
            .AmountMoney(amountMoney)
            .Autocomplete(true)
            .LocationId(_options.LocationId)
            .Build();

        var result = await _squareClient.PaymentsApi.CreatePaymentAsync(body);

        return result;
    }
    /// <inheritdoc />
    public async Task<Donation> RecordDonationAsync(Donation donation)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        donation.CreatedAt = DateTime.UtcNow;
        context.Donations.Add(donation);
        await context.SaveChangesAsync();
        eventService.NotifyDonationAdded(donation);
        return donation;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDonationStatusAsync(string paymentId, string status)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ExternalPaymentId == paymentId);
        
        if (donation == null)
        {
            return false;
        }

        donation.Status = status;
        await context.SaveChangesAsync();
        eventService.NotifyDonationUpdated(donation);
        return true;
    }

    // Admin methods
    /// <inheritdoc />
    public async Task<List<Donation>> GetAllDonationsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Donations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateDonationAsync(Donation donation)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var existing = await context.Donations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == donation.Id);
            if (existing == null)
            {
                return Result.Failure("Donation not found.");
            }

            existing.Amount = donation.Amount;
            existing.DonorName = donation.DonorName;
            existing.DonorEmail = donation.DonorEmail;
            existing.Status = donation.Status;
            existing.Currency = donation.Currency;
            existing.DeletedAt = donation.DeletedAt;

            await context.SaveChangesAsync();
            eventService.NotifyDonationUpdated(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while updating the donation: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteDonationAsync(int id)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var existing = await context.Donations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (existing == null)
            {
                return Result.Failure("Donation not found.");
            }

            existing.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            eventService.NotifyDonationUpdated(existing);
            eventService.NotifyDonationDeleted(id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while deleting the donation: {ex.Message}");
        }
    }
}
