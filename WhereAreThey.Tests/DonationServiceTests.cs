using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class DonationServiceTests
{
    private ApplicationDbContext CreateInMemoryContext(out DbContextOptions<ApplicationDbContext> options)
    {
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private IDbContextFactory<ApplicationDbContext> CreateDbFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContext()).Returns(() => new ApplicationDbContext(options));
        mock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        return mock.Object;
    }

    private IConfiguration CreateMockConfiguration()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_mock");
        return mock.Object;
    }

    [Fact]
    public async Task RecordDonation_ShouldSaveDonation()
    {
        // Arrange
        using var context = CreateInMemoryContext(out var options);
        var factory = CreateDbFactory(options);
        var service = new DonationService(factory, CreateMockConfiguration());
        var donation = new Donation
        {
            Amount = 25.00m,
            DonorName = "Test Donor",
            Status = "pending"
        };

        // Act
        var result = await service.RecordDonationAsync(donation);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(25.00m, result.Amount);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        
        var saved = await context.Donations.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Donor", saved.DonorName);
    }

    [Fact]
    public async Task UpdateDonationStatus_ShouldUpdateStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext(out var options);
        var factory = CreateDbFactory(options);
        var service = new DonationService(factory, CreateMockConfiguration());
        var piId = "pi_123";
        context.Donations.Add(new Donation
        {
            Amount = 10,
            StripePaymentIntentId = piId,
            Status = "pending"
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateDonationStatusAsync(piId, "completed");

        // Assert
        Assert.True(result);
        using var assertContext = new ApplicationDbContext(options);
        var updated = await assertContext.Donations.FirstAsync(d => d.StripePaymentIntentId == piId);
        Assert.Equal("completed", updated.Status);
    }

    [Fact]
    public async Task UpdateDonationStatus_ShouldReturnFalseIfNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext(out var options);
        var factory = CreateDbFactory(options);
        var service = new DonationService(factory, CreateMockConfiguration());

        // Act
        var result = await service.UpdateDonationStatusAsync("non_existent", "completed");

        // Assert
        Assert.False(result);
    }
}
