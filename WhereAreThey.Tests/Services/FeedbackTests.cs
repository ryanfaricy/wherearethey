using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;

namespace WhereAreThey.Tests.Services;

public class FeedbackTests
{
    private static IStringLocalizer<App> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<App>>();
        mock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => 
        {
            var val = key switch
            {
                "Feedback_Links_Error" => "Links are not allowed in feedback to prevent spam.",
                "Feedback_Cooldown_Error" => "You can only submit one feedback every {0} minutes.",
                _ => key,
            };
            return new LocalizedString(key, val);
        });
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => 
        {
            var val = key switch
            {
                "Feedback_Cooldown_Error" => "You can only submit one feedback every {0} minutes.",
                _ => key,
            };
            return new LocalizedString(key, string.Format(val, args));
        });
        return mock.Object;
    }
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

    private static ISettingsService CreateSettingsService(IDbContextFactory<ApplicationDbContext> factory)
    {
        return new SettingsService(factory, new Mock<IEventService>().Object);
    }

    private static IFeedbackService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var settingsService = CreateSettingsService(factory);
        var validator = new FeedbackValidator(factory, settingsService, CreateLocalizer());
        return new FeedbackService(factory, new Mock<IEventService>().Object, validator);
    }

    [Fact]
    public async Task AddFeedback_ShouldSucceed_WhenValid()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var feedback = new Feedback
        {
            Type = "Bug",
            Message = "Test message",
            UserIdentifier = "UserA",
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
        var service = CreateService(factory);
        var feedback1 = new Feedback { Type = "Bug", Message = "Msg 1", UserIdentifier = "UserA" };
        var feedback2 = new Feedback { Type = "Bug", Message = "Msg 2", UserIdentifier = "UserA" };

        // Act
        await service.AddFeedbackAsync(feedback1);
        
        // Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddFeedbackAsync(feedback2));
        Assert.Contains("5 minutes", ex.Message);
    }

    [Fact]
    public async Task AddFeedback_ShouldFail_WhenMessageContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var feedback = new Feedback 
        { 
            Type = "Feature", 
            Message = "Spam link: http://evil.com", 
            UserIdentifier = "UserA",
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddFeedbackAsync(feedback));
        Assert.Contains("Links are not allowed", ex.Message);
    }

    [Fact]
    public async Task DeleteFeedback_ShouldSoftDeleteItem()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var feedback = new Feedback { Type = "Bug", Message = "To delete", UserIdentifier = "UserA" };
        
        await service.AddFeedbackAsync(feedback);
        var added = (await service.GetAllFeedbackAsync())[0];

        // Act
        await service.DeleteFeedbackAsync(added.Id);

        // Assert
        var all = await service.GetAllFeedbackAsync();
        Assert.Single(all);
        Assert.NotNull(all[0].DeletedAt);
    }

    [Fact]
    public async Task AddFeedback_ShouldSucceed_WhenAutoReportedContainsLinks()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var feedback = new Feedback 
        { 
            Type = "Bug", 
            Message = "[AUTO-REPORTED] Exception at http://localhost:5000/stacktrace", 
            UserIdentifier = "system",
        };

        // Act
        await service.AddFeedbackAsync(feedback);

        // Assert
        var all = await service.GetAllFeedbackAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task AddFeedback_ShouldSucceed_WhenAutoReportedIsFrequent()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var service = CreateService(factory);
        var feedback1 = new Feedback { Type = "Bug", Message = "[AUTO-REPORTED] Error 1", UserIdentifier = "system" };
        var feedback2 = new Feedback { Type = "Bug", Message = "[AUTO-REPORTED] Error 2", UserIdentifier = "system" };

        // Act
        await service.AddFeedbackAsync(feedback1);
        await service.AddFeedbackAsync(feedback2);

        // Assert
        var all = await service.GetAllFeedbackAsync();
        Assert.Equal(2, all.Count);
    }
}
