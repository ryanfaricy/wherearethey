using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using WhereAreThey.Components;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class ReportCardTests : ComponentTestBase
{
    public ReportCardTests()
    {
        Services.AddSingleton<UserTimeZoneService>();
    }

    [Fact]
    public void ReportCard_ShowsDeletedBadge_ForRegularUser()
    {
        // Arrange
        var report = new Report
        {
            Id = 1,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow,
            Message = "Test report"
        };

        // Act
        var cut = Render<ReportCard>(parameters => parameters
            .Add(p => p.Report, report)
            .Add(p => p.IsAdmin, false)
        );

        // Assert
        var badge = cut.FindComponents<Radzen.Blazor.RadzenBadge>()
            .FirstOrDefault(b => b.Instance.Text == "DELETED");
        
        Assert.NotNull(badge);
    }

    [Fact]
    public void ReportCard_DoesNotShowDeletedBadge_WhenNotDeleted()
    {
        // Arrange
        var report = new Report
        {
            Id = 1,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
            Message = "Test report"
        };

        // Act
        var cut = Render<ReportCard>(parameters => parameters
            .Add(p => p.Report, report)
            .Add(p => p.IsAdmin, false)
        );

        // Assert
        var badge = cut.FindComponents<Radzen.Blazor.RadzenBadge>()
            .FirstOrDefault(b => b.Instance.Text == "DELETED");
        
        Assert.Null(badge);
    }

    [Fact]
    public void ReportCard_AppliesDeletedClass_WhenDeleted()
    {
        // Arrange
        var report = new Report
        {
            Id = 1,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow,
            Message = "Test report"
        };

        // Act
        var cut = Render<ReportCard>(parameters => parameters
            .Add(p => p.Report, report)
        );

        // Assert
        var card = cut.Find(".app-card");
        Assert.Contains("app-card-deleted", card.ClassName);
    }
}
