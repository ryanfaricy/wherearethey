using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class SubmissionValidatorTests
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

    private IStringLocalizer<App> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<App>>();
        mock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => new LocalizedString(key, key));
        return mock.Object;
    }

    [Fact]
    public void ValidateIdentifier_ShouldThrow_WhenNullOrEmpty()
    {
        // Arrange
        var validator = new SubmissionValidator(CreateFactory(CreateOptions()), CreateLocalizer());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => validator.ValidateIdentifier(null));
        Assert.Throws<InvalidOperationException>(() => validator.ValidateIdentifier(string.Empty));
    }

    [Fact]
    public void ValidateNoLinks_ShouldThrow_WhenLinksPresent()
    {
        // Arrange
        var validator = new SubmissionValidator(CreateFactory(CreateOptions()), CreateLocalizer());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => validator.ValidateNoLinks("Check https://test.com", "Error"));
        Assert.Throws<InvalidOperationException>(() => validator.ValidateNoLinks("Check http://test.com", "Error"));
        Assert.Throws<InvalidOperationException>(() => validator.ValidateNoLinks("Check www.test.com", "Error"));
    }

    [Fact]
    public async Task ValidateLocationReportCooldown_ShouldThrow_WhenRecentReportExists()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var validator = new SubmissionValidator(factory, CreateLocalizer());
        var userId = "user1";

        using (var context = new ApplicationDbContext(options))
        {
            context.LocationReports.Add(new LocationReport { ReporterIdentifier = userId, Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateLocationReportCooldownAsync(userId, 5));
    }

    [Fact]
    public async Task ValidateAlertLimit_ShouldThrow_WhenLimitExceeded()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var validator = new SubmissionValidator(factory, CreateLocalizer());
        var userId = "user1";

        using (var context = new ApplicationDbContext(options))
        {
            for (int i = 0; i < 3; i++)
            {
                context.Alerts.Add(new Alert { UserIdentifier = userId, CreatedAt = DateTime.UtcNow });
            }
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAlertLimitAsync(userId, 5, 3));
    }
}
