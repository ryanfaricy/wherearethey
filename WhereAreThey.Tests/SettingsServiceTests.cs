using Microsoft.EntityFrameworkCore;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class SettingsServiceTests
{
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _mockFactory;
    private readonly Mock<IAdminNotificationService> _adminNotifyMock;
    private readonly SettingsService _service;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public SettingsServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _mockFactory.Setup(f => f.CreateDbContextAsync(default))
            .ReturnsAsync(() => new ApplicationDbContext(_options));
            
        _adminNotifyMock = new Mock<IAdminNotificationService>();

        _service = new SettingsService(_mockFactory.Object, _adminNotifyMock.Object);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesAllFields()
    {
        // Arrange
        var newSettings = new SystemSettings
        {
            ReportExpiryHours = 10,
            ReportCooldownMinutes = 15,
            AlertLimitCount = 5,
            MaxReportDistanceMiles = 20.5m,
            MapboxToken = "test_token",
            DonationsEnabled = false,
            DataRetentionDays = 60
        };

        // Act
        await _service.UpdateSettingsAsync(newSettings);
        var retrieved = await _service.GetSettingsAsync();

        // Assert
        Assert.Equal(newSettings.ReportExpiryHours, retrieved.ReportExpiryHours);
        Assert.Equal(newSettings.ReportCooldownMinutes, retrieved.ReportCooldownMinutes);
        Assert.Equal(newSettings.AlertLimitCount, retrieved.AlertLimitCount);
        Assert.Equal(newSettings.MaxReportDistanceMiles, retrieved.MaxReportDistanceMiles);
        Assert.Equal(newSettings.MapboxToken, retrieved.MapboxToken);
        Assert.Equal(newSettings.DonationsEnabled, retrieved.DonationsEnabled);
        Assert.Equal(newSettings.DataRetentionDays, retrieved.DataRetentionDays);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsCachedValue()
    {
        // Act
        var first = await _service.GetSettingsAsync();
        var second = await _service.GetSettingsAsync();

        // Assert
        Assert.Same(first, second);
    }
}
