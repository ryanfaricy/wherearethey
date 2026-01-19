using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class AdminServiceTests
{
    private readonly AppOptions _appOptions = new();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        var eventServiceMock = new Mock<IEventService>();
        
        // Use real ProtectedLocalStorage with mocked dependencies
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        var localStorage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);

        _service = new AdminService(mockFactory.Object, eventServiceMock.Object, localStorage, Options.Create(_appOptions));
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsSuccess()
    {
        // Arrange
        _appOptions.AdminPassword = "correct_password";

        // Act
        var result = await _service.LoginAsync("correct_password", "127.0.0.1");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsFailure()
    {
        // Arrange
        _appOptions.AdminPassword = "correct_password";

        // Act
        var result = await _service.LoginAsync("wrong_password", "127.0.0.1");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid password.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_AfterFiveFailures_ReturnsFailure()
    {
        // Arrange
        _appOptions.AdminPassword = "correct_password";
        var ip = "1.2.3.4";

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            await _service.LoginAsync("wrong", ip);
        }

        var result = await _service.LoginAsync("wrong", ip);
        Assert.True(result.IsFailure);
        Assert.Contains("Too many failed login attempts", result.Error);
    }

    [Fact]
    public void NotifyAdminLogin_SetsCachedStatusAndTriggersEvent()
    {
        // Arrange
        bool eventTriggered = false;
        _service.OnAdminLogin += () => eventTriggered = true;

        // Act
        _service.NotifyAdminLogin();

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void NotifyAdminLogout_ResetsCachedStatusAndTriggersEvent()
    {
        // Arrange
        bool eventTriggered = false;
        _service.OnAdminLogout += () => eventTriggered = true;
        _service.NotifyAdminLogin(); // Set it to true first

        // Act
        _service.NotifyAdminLogout();

        // Assert
        Assert.True(eventTriggered);
    }
}
