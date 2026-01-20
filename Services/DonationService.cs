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
    ILogger<DonationService> logger,
    IValidator<Donation> validator) : BaseService<Donation>(contextFactory, eventService, logger, validator), IDonationService
{
    private readonly SquareOptions _options = squareOptions.Value;
    private readonly ISquareClient _squareClient = new SquareClient.Builder()
        .AccessToken(squareOptions.Value.AccessToken)
        .Environment(squareOptions.Value.Environment == "Production" ? Environment.Production : Environment.Sandbox)
        .Build();

    /// <inheritdoc />
    public async Task<CreatePaymentResponse> CreateSquarePaymentAsync(decimal amount, string sourceId)
    {
        Logger.LogInformation("Creating Square payment for amount {Amount}", amount);
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

        if (result.Errors != null && result.Errors.Any())
        {
            Logger.LogError("Square payment creation failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Detail)));
        }
        else
        {
            Logger.LogInformation("Square payment created successfully: {PaymentId}", result.Payment.Id);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<Donation>> CreateDonationAsync(Donation donation)
    {
        Logger.LogInformation("Recording new donation of {Amount}", donation.Amount);
        try
        {
            var validationResult = await Validator!.ValidateAsync(donation);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Donation validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result<Donation>.Failure(validationResult);
            }

            await using var context = await ContextFactory.CreateDbContextAsync();
            donation.CreatedAt = DateTime.UtcNow;
            context.Donations.Add(donation);
            await context.SaveChangesAsync();

            Logger.LogInformation("Donation {DonationId} recorded successfully", donation.Id);
            EventService.NotifyEntityChanged(donation, EntityChangeType.Added);
            return Result<Donation>.Success(donation);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error recording donation");
            return Result<Donation>.Failure($"An error occurred while recording the donation: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDonationStatusAsync(string paymentId, string status)
    {
        Logger.LogInformation("Updating donation status for payment {PaymentId} to {Status}", paymentId, status);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ExternalPaymentId == paymentId);
        
        if (donation == null)
        {
            Logger.LogWarning("Donation with payment ID {PaymentId} not found for status update", paymentId);
            return false;
        }

        donation.Status = status;
        await context.SaveChangesAsync();
        Logger.LogInformation("Donation {DonationId} status updated to {Status}", donation.Id, status);
        EventService.NotifyEntityChanged(donation, EntityChangeType.Updated);
        return true;
    }
}
