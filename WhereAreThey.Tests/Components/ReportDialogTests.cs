using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using WhereAreThey.Components.Pages;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Components;

public class ReportDialogTests : ComponentTestBase
{
    private readonly Mock<IReportService> _reportServiceMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<IMapService> _mapServiceMock = new();
    private readonly Mock<IClientStorageService> _storageServiceMock = new();
    private readonly Mock<IHapticFeedbackService> _hapticFeedbackServiceMock = new();
    private readonly Mock<ILogger<ReportDialog>> _loggerMock = new();

    public ReportDialogTests()
    {
        Services.AddSingleton(_reportServiceMock.Object);
        Services.AddSingleton(_geocodingServiceMock.Object);
        Services.AddSingleton(_mapServiceMock.Object);
        Services.AddSingleton(_storageServiceMock.Object);
        Services.AddSingleton(_hapticFeedbackServiceMock.Object);
        Services.AddSingleton(_loggerMock.Object);

        // Customize localizer for coordinate display
        LocalizerMock.Setup(l => l["Report_Coordinates"])
            .Returns(new Microsoft.Extensions.Localization.LocalizedString("Report_Coordinates", "Coordinates: {0}"));
    }

    [Fact]
    public void Render_InitialState()
    {
        // Arrange
        var report = new Report { Latitude = 40.0, Longitude = -70.0 };
        
        // Act
        var cut = Render<ReportDialog>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        Assert.Contains("40.00", cut.Markup);
        Assert.Contains("-70.00", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_ShouldReverseGeocode()
    {
        // Arrange
        var report = new Report { Latitude = 40.0, Longitude = -70.0 };
        _geocodingServiceMock.Setup(s => s.ReverseGeocodeAsync(40.0, -70.0))
            .ReturnsAsync("123 Main St");

        // Act
        var cut = Render<ReportDialog>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        _geocodingServiceMock.Verify(s => s.ReverseGeocodeAsync(40.0, -70.0), Times.Once);
        // AddressSearch is a child component, we can check if it received the value if we want to be thorough
    }

    [Fact]
    public async Task SubmitReport_Success_ShouldCloseDialogAndNotify()
    {
        // Arrange
        var report = new Report { Latitude = 40.0, Longitude = -70.0, Message = "Test" };
        _reportServiceMock.Setup(s => s.CreateReportAsync(It.IsAny<Report>()))
            .ReturnsAsync(Result<Report>.Success(report));

        var cut = Render<ReportDialog>(parameters => parameters
            .Add(p => p.Report, report));

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        _reportServiceMock.Verify(s => s.CreateReportAsync(report), Times.Once);
        _hapticFeedbackServiceMock.Verify(h => h.VibrateSuccessAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitReport_Emergency_ShouldVibrateEmergency()
    {
        // Arrange
        var report = new Report { Latitude = 40.0, Longitude = -70.0, IsEmergency = true };
        _reportServiceMock.Setup(s => s.CreateReportAsync(It.IsAny<Report>()))
            .ReturnsAsync(Result<Report>.Success(report));

        var cut = Render<ReportDialog>(parameters => parameters
            .Add(p => p.Report, report));

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        _hapticFeedbackServiceMock.Verify(h => h.VibrateEmergencyAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitReport_Failure_ShouldNotifyError()
    {
        // Arrange
        var report = new Report { Latitude = 40.0, Longitude = -70.0 };
        _reportServiceMock.Setup(s => s.CreateReportAsync(It.IsAny<Report>()))
            .ReturnsAsync(Result<Report>.Failure("Error message"));

        var cut = Render<ReportDialog>(parameters => parameters
            .Add(p => p.Report, report));

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        _hapticFeedbackServiceMock.Verify(h => h.VibrateErrorAsync(), Times.Once);
    }
}
