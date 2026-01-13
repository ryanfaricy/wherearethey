using Microsoft.EntityFrameworkCore;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class FeedbackTests
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

    private SettingsService CreateSettingsService(IDbContextFactory<ApplicationDbContext> factory)
    {
        return new SettingsService(factory);
    }

    [Fact]
    public async Task AddFeedback_ShouldSucceed_WhenValid()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var settingsService = CreateSettingsService(factory);
        var service = new FeedbackService(factory, settingsService);
        var feedback = new Feedback
        {
            Type = "Bug",
            Message = "Test message",
            UserIdentifier = "UserA"
        };

        // Act
        await service.AddFeedbackAsync(feedback);

        // Assert
        var all = await service.GetAllFeedbackAsync();
        Assert.Single(all);
        Assert.Equal("Test message", all[0].Message);
    }

    [Fact]
    public async Task AddFeedback_ShouldFail_WhenTooFrequent()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var settingsService = CreateSettingsService(factory);
        var service = new FeedbackService(factory, settingsService);
        var feedback1 = new Feedback { Type = "Bug", Message = "Msg 1", UserIdentifier = "UserA" };
        var feedback2 = new Feedback { Type = "Bug", Message = "Msg 2", UserIdentifier = "UserA" };

        // Act
        await service.AddFeedbackAsync(feedback1);
        
        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddFeedbackAsync(feedback2));
        Assert.Contains("5 minutes", ex.Message);
    }

    [Fact]
    public async Task AddFeedback_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var settingsService = CreateSettingsService(factory);
        var service = new FeedbackService(factory, settingsService);
        var feedback = new Feedback 
        { 
            Type = "Feature", 
            Message = "Spam link: http://evil.com", 
            UserIdentifier = "UserA" 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddFeedbackAsync(feedback));
        Assert.Contains("Links are not allowed", ex.Message);
    }

    [Fact]
    public async Task DeleteFeedback_ShouldRemoveItem()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var settingsService = CreateSettingsService(factory);
        var service = new FeedbackService(factory, settingsService);
        var feedback = new Feedback { Type = "Bug", Message = "To delete", UserIdentifier = "UserA" };
        
        await service.AddFeedbackAsync(feedback);
        var added = (await service.GetAllFeedbackAsync())[0];

        // Act
        await service.DeleteFeedbackAsync(added.Id);

        // Assert
        var all = await service.GetAllFeedbackAsync();
        Assert.Empty(all);
    }
}
