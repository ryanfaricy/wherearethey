using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests;

public class AdminServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        var adminNotifyMock = new Mock<IAdminNotificationService>();
        _mockConfig = new Mock<IConfiguration>();
        _service = new AdminService(mockFactory.Object, adminNotifyMock.Object, _mockConfig.Object);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        _mockConfig.Setup(c => c["AdminPassword"]).Returns("correct_password");

        // Act
        var result = await _service.LoginAsync("correct_password", "127.0.0.1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        _mockConfig.Setup(c => c["AdminPassword"]).Returns("correct_password");

        // Act
        var result = await _service.LoginAsync("wrong_password", "127.0.0.1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LoginAsync_AfterFiveFailures_ThrowsException()
    {
        // Arrange
        _mockConfig.Setup(c => c["AdminPassword"]).Returns("correct_password");
        var ip = "1.2.3.4";

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            await _service.LoginAsync("wrong", ip);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoginAsync("wrong", ip));
    }
}
