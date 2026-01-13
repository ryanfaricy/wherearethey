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
    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContextAsync(default))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        mock.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationDbContext(options));
        return mock.Object;
    }

    private IConfiguration CreateMockConfiguration()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Square:AccessToken"]).Returns("sandbox-mock-token");
        mock.Setup(c => c["Square:LocationId"]).Returns("mock-location");
        return mock.Object;
    }

    [Fact]
    public async Task RecordDonation_ShouldSaveDonation()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
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
        
        using var context = new ApplicationDbContext(options);
        var saved = await context.Donations.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Donor", saved.DonorName);
    }

    [Fact]
    public async Task UpdateDonationStatus_ShouldUpdateStatus()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new DonationService(factory, CreateMockConfiguration());
        var piId = "pi_123";
        
        using (var context = new ApplicationDbContext(options))
        {
            context.Donations.Add(new Donation
            {
                Amount = 10,
                ExternalPaymentId = piId,
                Status = "pending"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.UpdateDonationStatusAsync(piId, "completed");

        // Assert
        Assert.True(result);
        using (var context = new ApplicationDbContext(options))
        {
            var updated = await context.Donations.FirstAsync(d => d.ExternalPaymentId == piId);
            Assert.Equal("completed", updated.Status);
        }
    }

    [Fact]
    public async Task UpdateDonationStatus_ShouldReturnFalseIfNotFound()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = new DonationService(factory, CreateMockConfiguration());

        // Act
        var result = await service.UpdateDonationStatusAsync("non_existent", "completed");

        // Assert
        Assert.False(result);
    }
}
