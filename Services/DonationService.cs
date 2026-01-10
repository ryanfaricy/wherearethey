using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using Stripe;

namespace WhereAreThey.Services;

public class DonationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public DonationService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
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
        donation.CreatedAt = DateTime.UtcNow;
        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();
        return donation;
    }

    public async Task<bool> UpdateDonationStatusAsync(string paymentIntentId, string status)
    {
        var donation = await _context.Donations
            .FirstOrDefaultAsync(d => d.StripePaymentIntentId == paymentIntentId);
        
        if (donation == null) return false;

        donation.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
}
