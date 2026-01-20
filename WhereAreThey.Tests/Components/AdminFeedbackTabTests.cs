using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using WhereAreThey.Components.Admin;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class AdminFeedbackTabTests : ComponentTestBase
{
    private readonly Mock<IFeedbackService> _feedbackServiceMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IMapStateService> _mapStateServiceMock = new();
    private readonly Mock<ILogger<AdminFeedbackTab>> _loggerMock = new();

    public AdminFeedbackTabTests()
    {
        var timeZoneService = new UserTimeZoneService(); 
        
        Services.AddSingleton(_feedbackServiceMock.Object);
        Services.AddSingleton<IAdminDataService<Feedback>>(_feedbackServiceMock.Object);
        Services.AddSingleton(_eventServiceMock.Object);
        Services.AddSingleton(_mapStateServiceMock.Object);
        Services.AddSingleton(_loggerMock.Object);
        Services.AddSingleton(timeZoneService);
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
    }

    [Fact]
    public void AdminFeedbackTab_ShouldLoadDataOnInitialized()
    {
        // Arrange
        var feedback = new List<Feedback>
        {
            new() { Id = 1, Message = "Test 1", Type = "Bug", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Message = "Test 2", Type = "Suggestion", CreatedAt = DateTime.UtcNow },
        };
        _feedbackServiceMock.Setup(s => s.GetAllAsync(true)).ReturnsAsync(feedback);

        // Act
        var cut = Render<AdminFeedbackTab>();

        // Assert
        _feedbackServiceMock.Verify(s => s.GetAllAsync(true), Times.Once);
        Assert.Contains("Test 1", cut.Markup);
        Assert.Contains("Test 2", cut.Markup);
    }

    [Fact]
    public async Task AdminFeedbackTab_ShouldUpdateWhenEntityAdded()
    {
        // Arrange
        _feedbackServiceMock.Setup(s => s.GetAllAsync(true)).ReturnsAsync(new List<Feedback>());
        var cut = Render<AdminFeedbackTab>();

        var newFeedback = new Feedback { Id = 3, Message = "New Feedback", Type = "Bug", CreatedAt = DateTime.UtcNow };

        // Act
        await cut.InvokeAsync(() => 
        {
            _eventServiceMock.Raise(e => e.OnEntityChanged += null, newFeedback, EntityChangeType.Added);
        });

        // Assert
        Assert.Contains("New Feedback", cut.Markup);
    }
}
