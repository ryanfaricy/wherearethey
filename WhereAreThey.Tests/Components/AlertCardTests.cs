using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class AlertCardTests : ComponentTestBase
{
    private readonly Mock<IAlertService> _alertServiceMock = new();

    public AlertCardTests()
    {
        Services.AddSingleton(_alertServiceMock.Object);
        _alertServiceMock.Setup(s => s.DecryptEmail(It.IsAny<string>())).Returns("test@example.com");
    }

    [Fact]
    public void AlertCard_ShowsDeletedBadge_ForRegularUser()
    {
        // Arrange
        var alert = new Alert
        {
            Id = 1,
            DeletedAt = DateTime.UtcNow,
            Message = "Test alert",
            EncryptedEmail = "encrypted"
        };

        // Act
        var cut = Render<AlertCard>(parameters => parameters
            .Add(p => p.Alert, alert)
            .Add(p => p.IsAdmin, false)
        );

        // Assert
        var badge = cut.FindComponents<Radzen.Blazor.RadzenBadge>()
            .FirstOrDefault(b => b.Instance.Text == "DELETED");
        
        Assert.NotNull(badge);
    }

    [Fact]
    public void AlertCard_AppliesDeletedClass_WhenDeleted()
    {
        // Arrange
        var alert = new Alert
        {
            Id = 1,
            DeletedAt = DateTime.UtcNow,
            Message = "Test alert",
            EncryptedEmail = "encrypted"
        };

        // Act
        var cut = Render<AlertCard>(parameters => parameters
            .Add(p => p.Alert, alert)
        );

        // Assert
        var card = cut.Find(".app-card");
        Assert.Contains("app-card-deleted", card.ClassName);
    }
}
