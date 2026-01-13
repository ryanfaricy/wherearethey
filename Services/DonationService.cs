using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using Stripe;

namespace WhereAreThey.Services;

public class DonationService(IDbContextFactory<ApplicationDbContext> contextFactory, IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe expects amount in cents
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);
        return paymentIntent.ClientSecret;
    }

    public async Task<Donation> RecordDonationAsync(Donation donation)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        donation.CreatedAt = DateTime.UtcNow;
        context.Donations.Add(donation);
        await context.SaveChangesAsync();
        return donation;
    }

    public async Task<bool> UpdateDonationStatusAsync(string paymentIntentId, string status)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var donation = await context.Donations
            .FirstOrDefaultAsync(d => d.StripePaymentIntentId == paymentIntentId);
        
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
