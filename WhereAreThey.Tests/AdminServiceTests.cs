using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class AdminServiceTests
{
    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mock.Setup(f => f.CreateDbContextAsync(default))
            .Returns(() => Task.FromResult(new ApplicationDbContext(options)));
        mock.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationDbContext(options));
        return mock.Object;
    }

    private IConfiguration CreateMockConfiguration(string password = "test-password")
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["AdminPassword"]).Returns(password);
        return mock.Object;
    }

    [Fact]
    public async Task Login_ShouldSucceed_WithCorrectPassword()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var config = CreateMockConfiguration("secret");
        IAdminService service = new AdminService(factory, config);
        var ip = "127.0.0.1";

        // Act
        var result = await service.LoginAsync("secret", ip);

        // Assert
        Assert.True(result);
        using var context = new ApplicationDbContext(options);
        var attempt = await context.AdminLoginAttempts.SingleAsync();
        Assert.True(attempt.IsSuccessful);
        Assert.Equal(ip, attempt.IpAddress);
    }

    [Fact]
    public async Task Login_ShouldFail_WithIncorrectPassword()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var config = CreateMockConfiguration("secret");
        var service = new AdminService(factory, config);
        var ip = "127.0.0.1";

        // Act
        var result = await service.LoginAsync("wrong", ip);

        // Assert
        Assert.False(result);
        using var context = new ApplicationDbContext(options);
        var attempt = await context.AdminLoginAttempts.SingleAsync();
        Assert.False(attempt.IsSuccessful);
    }

    [Fact]
    public async Task Login_ShouldLockout_AfterFiveFailedAttempts()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var config = CreateMockConfiguration("secret");
        var service = new AdminService(factory, config);
        var ip = "1.2.3.4";

        // 5 failed attempts
        for (int i = 0; i < 5; i++)
        {
            await service.LoginAsync("wrong", ip);
        }

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("secret", ip));
        Assert.Contains("Too many failed login attempts", ex.Message);

        using var context = new ApplicationDbContext(options);
        Assert.Equal(6, await context.AdminLoginAttempts.CountAsync());
        Assert.Equal(6, await context.AdminLoginAttempts.CountAsync(a => !a.IsSuccessful));
    }

    [Fact]
    public async Task Login_ShouldNotLockout_DifferentIps()
    {
        // Arrange
        var options = CreateOptions();
        var factory = CreateFactory(options);
        var config = CreateMockConfiguration("secret");
        var service = new AdminService(factory, config);

        // 5 failed attempts from IP 1
        for (int i = 0; i < 5; i++)
        {
            await service.LoginAsync("wrong", "1.1.1.1");
        }

        // Act
        var result = await service.LoginAsync("secret", "2.2.2.2");

        // Assert
        Assert.True(result);
    }
}
