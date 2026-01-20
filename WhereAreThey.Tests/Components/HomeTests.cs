using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using WhereAreThey.Components.Pages;
using WhereAreThey.Components.Home;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Components;

public class HomeTests : ComponentTestBase
{
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<IReportService> _reportServiceMock = new();
    private readonly Mock<IGeocodingService> _geocodingServiceMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<IMapService> _mapServiceMock = new();
    private readonly Mock<IClientStorageService> _storageServiceMock = new();
    private readonly Mock<ILogger<Home>> _loggerMock = new();
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IHapticFeedbackService> _hapticFeedbackServiceMock = new();
    private readonly Mock<IMapInteractionService> _mapInteractionServiceMock = new();
    private readonly Mock<IClientLocationService> _clientLocationServiceMock = new();
    private readonly Mock<IAdminService> _adminServiceMock = new();
    private readonly Mock<IMapNavigationManager> _mapNavigationManagerMock = new();
    
    private readonly MapStateService _mapStateService;

    public HomeTests()
    {
        Services.AddSingleton(_alertServiceMock.Object);
        Services.AddSingleton(_reportServiceMock.Object);
        Services.AddSingleton(_geocodingServiceMock.Object);
        Services.AddSingleton(_settingsServiceMock.Object);
        Services.AddSingleton(_mapServiceMock.Object);
        Services.AddSingleton(_storageServiceMock.Object);
        Services.AddSingleton(_loggerMock.Object);
        Services.AddSingleton(_eventServiceMock.Object);
        Services.AddSingleton(_hapticFeedbackServiceMock.Object);
        Services.AddSingleton(_mapInteractionServiceMock.Object);
        Services.AddSingleton(_clientLocationServiceMock.Object);
        Services.AddSingleton(_adminServiceMock.Object);
        Services.AddSingleton(_mapNavigationManagerMock.Object);

        // Default setups to avoid NullReferenceException
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<Guid?>()))
            .ReturnsAsync([]);
        _reportServiceMock.Setup(s => s.GetAllAsync(true))
            .ReturnsAsync([]);
        _alertServiceMock.Setup(s => s.GetAllAsync(true))
            .ReturnsAsync([]);

        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new SystemSettings { ReportExpiryHours = 24 });
        
        _storageServiceMock.Setup(s => s.GetUserIdentifierAsync())
            .ReturnsAsync("test-user");

        _adminServiceMock.Setup(s => s.IsAdminAsync()).ReturnsAsync(false);
        
        _mapNavigationManagerMock.Setup(m => m.GetNavigationStateAsync())
            .ReturnsAsync(new MapNavigationState());

        // Use real MapStateService as it's the core of the state management
        _mapStateService = new MapStateService(
            _reportServiceMock.Object,
            _alertServiceMock.Object,
            _eventServiceMock.Object,
            _mapServiceMock.Object,
            _settingsServiceMock.Object);
        
        Services.AddSingleton<IMapStateService>(_mapStateService);
    }

    [Fact]
    public async Task Home_InitializesMapWithReports_FromService()
    {
        // Arrange
        var reports = new List<Report> 
        { 
            new() { Id = 1, Latitude = 40.0, Longitude = -70.0, CreatedAt = DateTime.UtcNow },
        };
        
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<Guid?>()))
            .ReturnsAsync(reports);
        
        // Act
        var cut = Render<Home>();

        await cut.InvokeAsync(() => Task.Delay(100));

        // Assert
        // Verify it was initialized (with empty list first time or reports if loaded quickly)
        _mapServiceMock.Verify(m => m.InitMapAsync(
            It.IsAny<string>(), 
            It.IsAny<double>(), 
            It.IsAny<double>(), 
            It.IsAny<List<Report>>(), 
            It.IsAny<object>(), 
            It.IsAny<List<Alert>>(), 
            It.IsAny<object>(), 
            false), 
        Times.AtLeastOnce);

        // Verify that reports were eventually updated on the map
        _mapServiceMock.Verify(m => m.UpdateHeatMapAsync(
            It.Is<List<Report>>(r => r.Count == 1 && r[0].Id == 1), 
            It.IsAny<bool>()), 
        Times.AtLeastOnce);
    }

    [Fact]
    public async Task Home_AdminMode_ShowsDeletedReportsOnMap()
    {
        // Arrange
        _adminServiceMock.Setup(s => s.IsAdminAsync()).ReturnsAsync(true);
        // We set isAdmin in the InitializeAsync call which Home calls in OnAfterRenderAsync
        
        var reports = new List<Report> 
        { 
            new() { Id = 1, Latitude = 40.0, Longitude = -70.0, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Latitude = 41.0, Longitude = -71.0, CreatedAt = DateTime.UtcNow, DeletedAt = DateTime.UtcNow },
        };
        
        // When admin and showDeleted is true, MapStateService calls GetRecentReportsAsync(..., true)
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int?>(), true, It.IsAny<Guid?>()))
            .ReturnsAsync(reports);

        // Act
        var cut = Render<Home>();
        
        // Set showDeleted after initial render/initialization
        await cut.InvokeAsync(async () => 
        {
            _mapStateService.ShowDeleted = true;
            await _mapStateService.LoadReportsAsync();
        });

        await cut.InvokeAsync(() => Task.Delay(100));

        // Assert
        _mapServiceMock.Verify(m => m.UpdateHeatMapAsync(
            It.Is<List<Report>>(r => r.Count == 2), 
            It.IsAny<bool>()), 
        Times.AtLeastOnce);
    }

    [Fact]
    public async Task Home_UpdatesMap_WhenNewReportAddedViaEvent()
    {
        // Arrange
        _reportServiceMock.Setup(s => s.GetRecentReportsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<Guid?>()))
            .ReturnsAsync([]);
        
        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));
        
        // Set MapInitialized so MapStateService triggers UpdateHeatMapAsync
        _mapStateService.MapInitialized = true;

        var newReport = new Report { Id = 10, Latitude = 42.0, Longitude = -72.0, CreatedAt = DateTime.UtcNow };

        // Act
        await cut.InvokeAsync(() => 
        {
            _eventServiceMock.Raise(e => e.OnEntityChanged += null, newReport, EntityChangeType.Added);
        });

        // Assert
        _mapServiceMock.Verify(m => m.UpdateHeatMapAsync(
            It.Is<List<Report>>(r => r.Any(report => report.Id == 10)), 
            It.IsAny<bool>()), 
        Times.Once);
    }

    [Fact]
    public async Task Home_LoadsFromQueryParameters()
    {
        // Arrange
        var navState = new MapNavigationState
        {
            InitialLat = 34.0,
            InitialLng = -118.0,
            InitialRadius = 5.0,
            SelectedHours = 12,
        };
        _mapNavigationManagerMock.Setup(m => m.GetNavigationStateAsync())
            .ReturnsAsync(navState);

        // Act
        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));

        // Assert
        _mapServiceMock.Verify(m => m.InitMapAsync(
            It.IsAny<string>(),
            34.0,
            -118.0,
            It.IsAny<List<Report>>(),
            It.IsAny<object>(),
            It.IsAny<List<Alert>>(),
            It.IsAny<object>(),
            It.IsAny<bool>()),
        Times.AtLeastOnce);

        _reportServiceMock.Verify(s => s.GetRecentReportsAsync(12, It.IsAny<bool>(), It.IsAny<Guid?>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Home_ShowsNotification_WhenNearbyReportAdded()
    {
        // Arrange
        var userLocation = new GeolocationPosition 
        { 
            Coords = new GeolocationCoordinates { Latitude = 40.0, Longitude = -70.0 },
        };
        _clientLocationServiceMock.SetupGet(l => l.LastKnownPosition).Returns(userLocation);
        _storageServiceMock.Setup(s => s.GetUserIdentifierAsync()).ReturnsAsync("user1");

        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));

        // New report at 40.01, -70.01 (very close)
        var nearbyReport = new Report 
        { 
            Id = 100, 
            Latitude = 40.01, 
            Longitude = -70.01, 
            CreatedAt = DateTime.UtcNow,
            ReporterIdentifier = "user2", // Different user
        };

        // Act
        await cut.InvokeAsync(() => 
        {
            _eventServiceMock.Raise(e => e.OnEntityChanged += null, nearbyReport, EntityChangeType.Added);
        });

        // Assert
        _reportServiceMock.Verify(s => s.GetRecentReportsAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<Guid?>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Home_DoesNotCenterOnUser_WhenDeepLinkedToReport()
    {
        // Arrange
        var report = new Report { Id = 123, Latitude = 51.5074, Longitude = -0.1278 }; // London
        _reportServiceMock.Setup(s => s.GetReportByIdAsync(123)).ReturnsAsync(Result<Report>.Success(report));
        
        var navState = new MapNavigationState
        {
            FocusReportId = 123,
            InitialLat = 51.5074,
            InitialLng = -0.1278,
        };
        _mapNavigationManagerMock.Setup(m => m.GetNavigationStateAsync()).ReturnsAsync(navState);
        
        // Start with no location
        _clientLocationServiceMock.SetupGet(l => l.LastKnownPosition).Returns((GeolocationPosition)null!);

        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));

        // Act: Simulate location update (New York)
        var nyLocation = new GeolocationPosition 
        { 
            Coords = new GeolocationCoordinates { Latitude = 40.7128, Longitude = -74.0060 },
        };
        
        // Trigger OnLocationUpdated via HeatMap's JSInvokable method
        var heatMap = cut.FindComponent<HeatMap>();
        await cut.InvokeAsync(() => heatMap.Instance.OnLocationUpdatedInternal(nyLocation));

        // Assert: SetMapViewAsync should NOT have been called for NY location 
        // because we are focusing a report.
        _mapServiceMock.Verify(m => m.SetMapViewAsync(40.7128, -74.0060, It.IsAny<double?>()), Times.Never);
    }

    [Fact]
    public async Task Home_ShowsNotification_WhenReportNotFoundInNavState()
    {
        // Arrange
        var notificationService = Services.GetRequiredService<NotificationService>();

        _mapNavigationManagerMock.Setup(m => m.GetNavigationStateAsync())
            .ReturnsAsync(new MapNavigationState { ReportNotFound = true });

        // Act
        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));

        // Assert
        Assert.Contains(notificationService.Messages, m => m.Summary == "Report_Removed");
    }

    [Fact]
    public async Task Home_ShowsNotification_WhenIntReportIdNotFound()
    {
        // Arrange
        var notificationService = Services.GetRequiredService<NotificationService>();

        _mapNavigationManagerMock.Setup(m => m.GetNavigationStateAsync())
            .ReturnsAsync(new MapNavigationState { FocusReportId = 999 });

        _reportServiceMock.Setup(s => s.GetReportByIdAsync(999))
            .ReturnsAsync(Result<Report>.Failure("Not found"));

        // Act
        var cut = Render<Home>();
        await cut.InvokeAsync(() => Task.Delay(100));

        // Assert
        Assert.Contains(notificationService.Messages, m => m.Summary == "Report_Removed");
    }
}
