using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WhereAreThey.Data;
using WhereAreThey.Models;
using Square;
using Square.Models;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace WhereAreThey.Services;

public class DonationService(IDbContextFactory<ApplicationDbContext> contextFactory, IConfiguration configuration)
{
    private readonly ISquareClient _squareClient = new SquareClient.Builder()
        .AccessToken(configuration["Square:AccessToken"] ?? "")
        .Environment(configuration["Square:Environment"] == "Production" ? Square.Environment.Production : Square.Environment.Sandbox)
        .Build();

    public async Task<CreatePaymentResponse> CreateSquarePaymentAsync(decimal amount, string sourceId)
    {
        var amountMoney = new Money.Builder()
            .Amount((long)(amount * 100))
            .Currency("USD")
            .Build();

        var body = new CreatePaymentRequest.Builder(sourceId, Guid.NewGuid().ToString())
            .AmountMoney(amountMoney)
            .Autocomplete(true)
            .LocationId(configuration["Square:LocationId"] ?? "")
            .Build();

        var result = await _squareClient.PaymentsApi.CreatePaymentAsync(body);

        return result;
    }
    public async Task<Donation> RecordDonationAsync(Donation donation)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        donation.CreatedAt = DateTime.UtcNow;
        context.Donations.Add(donation);
        await context.SaveChangesAsync();
        return donation;
    }

    public async Task<bool> UpdateDonationStatusAsync(string paymentId, string status)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .FirstOrDefaultAsync(d => d.ExternalPaymentId == paymentId);
        
        if (donation == null) return false;

        donation.Status = status;
        await context.SaveChangesAsync();
        return true;
    }

    // Admin methods
    public async Task<List<Donation>> GetAllDonationsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Donations
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
