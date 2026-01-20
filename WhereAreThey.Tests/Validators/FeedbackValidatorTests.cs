using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Validators;
using Xunit;

namespace WhereAreThey.Tests.Validators;

public class FeedbackValidatorTests
{
    private readonly FeedbackValidator _validator;
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IStringLocalizer<App>> _localizerMock = new();
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public FeedbackValidatorTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(default))
            .ReturnsAsync(() => new ApplicationDbContext(_options));

        _localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));

        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { ReportCooldownMinutes = 5 });

        _validator = new FeedbackValidator(_contextFactoryMock.Object, _settingsServiceMock.Object, _localizerMock.Object);
    }

    [Fact]
    public async Task Should_Have_Error_When_UserIdentifier_Is_Empty()
    {
        var model = new Feedback { UserIdentifier = "" };
        var result = await _validator.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.UserIdentifier);
    }

    [Fact]
    public async Task Should_Have_Error_When_Message_Contains_Links()
    {
        var model = new Feedback { UserIdentifier = "user1", Message = "Check out https://evil.com" };
        var result = await _validator.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.Message)
            .WithErrorMessage("Feedback_Links_Error");
    }

    [Fact]
    public async Task Should_Not_Have_Error_When_AutoReported_Message_Contains_Links()
    {
        var model = new Feedback { UserIdentifier = "user1", Message = "[AUTO-REPORTED] Location: https://maps.google.com" };
        var result = await _validator.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public async Task Should_Have_Error_When_In_Cooldown()
    {
        // Arrange
        var userIdentifier = "user1";
        await using (var db = new ApplicationDbContext(_options))
        {
            db.Feedbacks.Add(new Feedback 
            { 
                UserIdentifier = userIdentifier, 
                Message = "Prev message", 
                Type = "Bug",
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            });
            await db.SaveChangesAsync();
        }

        var model = new Feedback { UserIdentifier = userIdentifier, Message = "New message" };

        // Act
        var result = await _validator.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIdentifier)
            .WithErrorMessage("Feedback_Cooldown_Error");
    }
}
