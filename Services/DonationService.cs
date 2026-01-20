using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Square;
using Square.Models;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using Environment = Square.Environment;

namespace WhereAreThey.Services;

/// <inheritdoc cref="BaseService{T}" />
public class DonationService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IEventService eventService,
    IOptions<SquareOptions> squareOptions,
    IValidator<Donation> validator) : BaseService<Donation>(contextFactory, eventService), IDonationService
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
    public async Task<Result<Donation>> CreateDonationAsync(Donation donation)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(donation);
            if (!validationResult.IsValid)
            {
                return Result<Donation>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();
            donation.CreatedAt = DateTime.UtcNow;
            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            EventService.NotifyEntityChanged(donation, EntityChangeType.Added);
            return Result<Donation>.Success(donation);
        }
        catch (Exception ex)
        {
            return Result<Donation>.Failure($"An error occurred while recording the donation: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDonationStatusAsync(string paymentId, string status)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ExternalPaymentId == paymentId);
        
        if (donation == null)
        {
            return false;
        }

        donation.Status = status;
        await context.SaveChangesAsync();
        EventService.NotifyEntityChanged(donation, EntityChangeType.Updated);
        return true;
    }

    // Admin methods
    /// <inheritdoc />
    public async Task<List<Donation>> GetAllDonationsAsync()
    {
        return await GetAllAsync(isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateDonationAsync(Donation donation)
    {
        var validationResult = await validator.ValidateAsync(donation);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult);
        }

        return await UpdateAsync(donation);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteDonationAsync(int id, bool hardDelete = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null)
        {
            return Result.Failure("Donation not found.");
        }

        if (hardDelete || donation.DeletedAt != null)
        {
            return await HardDeleteAsync(id);
        }
        return await SoftDeleteAsync(id);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteDonationsAsync(IEnumerable<int> ids, bool hardDelete = false)
    {
        if (hardDelete)
        {
            return await HardDeleteRangeAsync(ids);
        }
        return await SoftDeleteRangeAsync(ids);
    }
}
