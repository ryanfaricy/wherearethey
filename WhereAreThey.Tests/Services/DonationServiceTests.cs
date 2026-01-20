using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests.Services;

public class DonationServiceTests
{
    private readonly IValidator<Donation> _validator = new DonationValidator();

    private static DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        mock.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationDbContext(options));
        return mock.Object;
    }

    private static IOptions<SquareOptions> CreateOptionsWrapper()
    {
        return Options.Create(new SquareOptions
        {
            AccessToken = "sandbox-mock-token",
            LocationId = "mock-location",
        });
    }

    [Fact]
    public async Task CreateDonation_ShouldSaveDonation()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var eventServiceMock = new Mock<IEventService>();
        IDonationService service = new DonationService(factory, eventServiceMock.Object, CreateOptionsWrapper(), NullLogger<DonationService>.Instance, _validator);
        var donation = new Donation
        {
            Amount = 25.00m,
            DonorName = "Test Donor",
            Status = "pending",
        };

        // Act
        var result = await service.CreateDonationAsync(donation);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value!.Id);
        Assert.Equal(25.00m, result.Value.Amount);
        Assert.True(result.Value.CreatedAt <= DateTime.UtcNow);

        await using var context = new ApplicationDbContext(options);
        var saved = await context.Donations.FindAsync(result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Donor", saved.DonorName);
    }

    [Fact]
    public async Task UpdateDonationStatus_ShouldUpdateStatus()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var eventServiceMock = new Mock<IEventService>();
        var service = new DonationService(factory, eventServiceMock.Object, CreateOptionsWrapper(), NullLogger<DonationService>.Instance, _validator);
        var piId = "pi_123";

        await using (var context = new ApplicationDbContext(options))
        {
            context.Donations.Add(new Donation
            {
                Amount = 10,
                ExternalPaymentId = piId,
                Status = "pending",
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.UpdateDonationStatusAsync(piId, "completed");

        // Assert
        Assert.True(result);
        await using (var context = new ApplicationDbContext(options))
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
        var eventServiceMock = new Mock<IEventService>();
        var service = new DonationService(factory, eventServiceMock.Object, CreateOptionsWrapper(), NullLogger<DonationService>.Instance, _validator);

        // Act
        var result = await service.UpdateDonationStatusAsync("non_existent", "completed");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateDonation_ShouldUpdateFields()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var eventServiceMock = new Mock<IEventService>();
        var service = new DonationService(factory, eventServiceMock.Object, CreateOptionsWrapper(), NullLogger<DonationService>.Instance, _validator);

        Donation initial;
        await using (var context = new ApplicationDbContext(options))
        {
            initial = new Donation
            {
                Amount = 10,
                DonorName = "Initial Name",
                Status = "pending",
            };
            context.Donations.Add(initial);
            await context.SaveChangesAsync();
        }

        // Act
        initial.Amount = 50;
        initial.DonorName = "Updated Name";
        initial.Status = "completed";
        var result = await service.UpdateAsync(initial);

        // Assert
        Assert.True(result.IsSuccess);
        await using (var context = new ApplicationDbContext(options))
        {
            var updated = await context.Donations.FindAsync(initial.Id);
            Assert.NotNull(updated);
            Assert.Equal(50, updated.Amount);
            Assert.Equal("Updated Name", updated.DonorName);
            Assert.Equal("completed", updated.Status);
        }
        
        eventServiceMock.Verify(e => e.NotifyEntityChanged(It.IsAny<Donation>(), EntityChangeType.Updated), Times.Once);
    }
}
