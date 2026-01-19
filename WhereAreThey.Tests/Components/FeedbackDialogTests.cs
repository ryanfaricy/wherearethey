using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen.Blazor;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

[SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.")]
public class FeedbackDialogTests : ComponentTestBase
{
    private readonly Mock<IFeedbackService> _feedbackServiceMock;
    private readonly Mock<IHapticFeedbackService> _hapticServiceMock;

    public FeedbackDialogTests()
    {
        _feedbackServiceMock = new Mock<IFeedbackService>();
        _hapticServiceMock = new Mock<IHapticFeedbackService>();
        
        _feedbackServiceMock.Setup(s => s.CreateFeedbackAsync(It.IsAny<Feedback>()))
            .ReturnsAsync(Result<Feedback>.Success(new Feedback()));

        // Use real Radzen services instead of mocks to avoid constructor issues
        // They are already registered by AddRadzenComponents in ComponentTestBase

        Services.AddSingleton(_feedbackServiceMock.Object);
        Services.AddSingleton(_hapticServiceMock.Object);
        
        // Mock JSInterop for getUserIdentifier
        JSInterop.Setup<string>("getUserIdentifier").SetResult("test-user");
    }

    [Fact]
    public void FeedbackDialog_Renders_Correctly()
    {
        // Arrange
        // Act
        var cut = Render<FeedbackDialog>();

        // Assert
        Assert.NotNull(cut.FindComponent<RadzenTemplateForm<Feedback>>());
        Assert.NotNull(cut.FindComponent<RadzenDropDown<string>>());
        Assert.NotNull(cut.FindComponent<RadzenTextArea>());
        Assert.Contains("Feedback_Title", cut.Markup);
    }

    [Fact]
    public async Task FeedbackDialog_Submission_CallsService()
    {
        // Arrange
        var cut = Render<FeedbackDialog>();
        var textarea = cut.FindComponent<RadzenTextArea>();
        
        // Act
        // Set message
        await cut.InvokeAsync(() => textarea.Instance.Value = "Test feedback message");
        await cut.InvokeAsync(() => textarea.Instance.ValueChanged.InvokeAsync("Test feedback message"));

        // Submit form
        var form = cut.FindComponent<RadzenTemplateForm<Feedback>>();
        await cut.InvokeAsync(() => form.Instance.Submit.InvokeAsync(new Feedback()));

        // Assert
        _feedbackServiceMock.Verify(s => s.CreateFeedbackAsync(It.Is<Feedback>(f => f.Message == "Test feedback message")), Times.Once);
        _hapticServiceMock.Verify(s => s.VibrateSuccessAsync(), Times.Once);
    }

    [Fact]
    public async Task FeedbackDialog_EasterEgg_Redirects()
    {
        // Arrange
        var cut = Render<FeedbackDialog>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var textarea = cut.FindComponent<RadzenTextArea>();
        var dropdown = cut.FindComponent<RadzenDropDown<string>>();

        // Act
        // Set Type to Feature (easter egg requires Feature type and specific message)
        await cut.InvokeAsync(() => dropdown.Instance.Value = "Feature");
        await cut.InvokeAsync(() => dropdown.Instance.ValueChanged.InvokeAsync("Feature"));
        
        // Set Easter Egg message
        await cut.InvokeAsync(() => textarea.Instance.Value = "let me into cp");
        await cut.InvokeAsync(() => textarea.Instance.ValueChanged.InvokeAsync("let me into cp"));

        // Submit form
        var form = cut.FindComponent<RadzenTemplateForm<Feedback>>();
        await cut.InvokeAsync(() => form.Instance.Submit.InvokeAsync(new Feedback()));

        // Assert
        Assert.EndsWith("/cp", nav.Uri);
        _feedbackServiceMock.Verify(s => s.CreateFeedbackAsync(It.IsAny<Feedback>()), Times.Never);
    }
}
