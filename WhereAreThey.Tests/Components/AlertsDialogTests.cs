using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using Radzen.Blazor;

namespace WhereAreThey.Tests.Components;

[SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.")]
public class AlertsDialogTests : ComponentTestBase
{
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<IClientStorageService> _storageServiceMock;

    public AlertsDialogTests()
    {
        _alertServiceMock = new Mock<IAlertService>();
        var geocodingServiceMock = new Mock<IGeocodingService>();
        _storageServiceMock = new Mock<IClientStorageService>();
        var hapticServiceMock = new Mock<IHapticFeedbackService>();
        var settingsServiceMock = new Mock<ISettingsService>();
        var pwaServiceMock = new Mock<IPwaService>();

        Services.AddSingleton(_alertServiceMock.Object);
        Services.AddSingleton(geocodingServiceMock.Object);
        Services.AddSingleton(_storageServiceMock.Object);
        Services.AddSingleton(hapticServiceMock.Object);
        Services.AddSingleton(settingsServiceMock.Object);
        Services.AddSingleton(pwaServiceMock.Object);
        Services.AddSingleton(new HttpClient());

        settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSettings());
        _storageServiceMock.Setup(s => s.GetUserIdentifierAsync()).ReturnsAsync("test-user");
        _alertServiceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);

        ValidationServiceMock.Setup(v => v.ExecuteAsync<Alert>(It.IsAny<Func<Task<Result<Alert>>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Alert, Task>>(), It.IsAny<Func<string, Task>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns<Func<Task<Result<Alert>>>, string, string, Func<Alert, Task>, Func<string, Task>, bool, bool, bool, string>(async (op, _, _, success, _, _, _, _, _) => {
                var result = await op();
                if (result.IsSuccess && success != null) await success(result.Value!);
                return result.Value;
            });
    }

    [Fact]
    public void AlertsDialog_PreFillsEmail_FromStorage()
    {
        // Arrange
        _storageServiceMock.Setup(s => s.GetItemAsync("last-alert-email")).ReturnsAsync("last@example.com");

        // Act
        var cut = Render<AlertsDialog>();

        // Assert
        // Better way to find it is by name if possible, or just check all textboxes
        var textboxes = cut.FindComponents<RadzenTextBox>();
        var emailTextBox = textboxes.FirstOrDefault(t => t.Instance.Name == "Email");
        
        Assert.NotNull(emailTextBox);
        Assert.Equal("last@example.com", emailTextBox.Instance.Value);
    }

    [Fact]
    public async Task AlertsDialog_SavesEmail_ToStorage_AfterSuccess()
    {
        // Arrange
        _storageServiceMock.Setup(s => s.GetItemAsync("last-alert-email")).ReturnsAsync((string)null!);
        _alertServiceMock.Setup(s => s.CreateAlertAsync(It.IsAny<Alert>(), It.IsAny<string>()))
            .ReturnsAsync(Result<Alert>.Success(new Alert()));

        var cut = Render<AlertsDialog>();
        var emailTextBox = cut.FindComponents<RadzenTextBox>().First(t => t.Instance.Name == "Email");
        
        // Set email
        await cut.InvokeAsync(() => {
            emailTextBox.Instance.Value = "new@example.com";
            emailTextBox.Instance.ValueChanged.InvokeAsync("new@example.com");
        });

        // Act
        var form = cut.FindComponent<RadzenTemplateForm<Alert>>();
        await cut.InvokeAsync(() => form.Instance.Submit.InvokeAsync(new Alert()));

        // Assert
        _storageServiceMock.Verify(s => s.SetItemAsync("last-alert-email", "new@example.com"), Times.Once);
    }
}
