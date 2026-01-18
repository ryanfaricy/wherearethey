using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests;

public class DatabaseCleanupServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<ILogger<DatabaseCleanupService>> _loggerMock = new();

    private static async Task<(ApplicationDbContext, IDbContextFactory<ApplicationDbContext>)> CreateContextAndFactoryAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        return (context, factoryMock.Object);
    }

    [Fact]
    public async Task CleanupDatabaseAsync_ShouldDeleteOldRecords()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        
        var settings = new SystemSettings 
        { 
            DataRetentionDays = 30,
        };
        _settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

        var now = DateTime.UtcNow;
        
        // 1. Reports
        context.LocationReports.Add(new LocationReport { Timestamp = now.AddDays(-31), Latitude = 0, Longitude = 0 }); // Old
        context.LocationReports.Add(new LocationReport { Timestamp = now.AddDays(-29), Latitude = 0, Longitude = 0 }); // New
        
        // 2. Verifications
        context.EmailVerifications.Add(new EmailVerification { CreatedAt = now.AddHours(-25), EmailHash = "old", Token = "t1" }); // Old
        context.EmailVerifications.Add(new EmailVerification { CreatedAt = now.AddHours(-23), EmailHash = "new", Token = "t2" }); // New
        context.EmailVerifications.Add(new EmailVerification { CreatedAt = now.AddHours(-25), EmailHash = "verified", Token = "t3", VerifiedAt = now }); // Old but verified
        
        // 3. Alerts
        context.Alerts.Add(new Alert { DeletedAt = now.AddDays(-31), Latitude = 0, Longitude = 0, EncryptedEmail = "e" }); // Old soft-deleted
        context.Alerts.Add(new Alert { DeletedAt = now.AddDays(-29), Latitude = 0, Longitude = 0, EncryptedEmail = "e" }); // New soft-deleted
        context.Alerts.Add(new Alert { DeletedAt = null, Latitude = 0, Longitude = 0, EncryptedEmail = "e" }); // Not deleted
        
        // 4. Login attempts
        context.AdminLoginAttempts.Add(new AdminLoginAttempt { Timestamp = now.AddDays(-91), IpAddress = "1" }); // Old
        context.AdminLoginAttempts.Add(new AdminLoginAttempt { Timestamp = now.AddDays(-89), IpAddress = "2" }); // New
        
        // 5. Feedback
        context.Feedbacks.Add(new Feedback { Timestamp = now.AddYears(-1).AddDays(-1), Message = "old" }); // Old
        context.Feedbacks.Add(new Feedback { Timestamp = now.AddYears(-1).AddDays(1), Message = "new" }); // New

        await context.SaveChangesAsync();

        var service = new DatabaseCleanupServiceWrapper(factory, _settingsServiceMock.Object, _loggerMock.Object);

        // Act
        await service.PublicCleanupDatabaseAsync();

        // Assert
        Assert.Equal(1, await context.LocationReports.CountAsync());
        Assert.Equal(2, await context.EmailVerifications.CountAsync()); // One new, one old-but-verified
        Assert.Equal(2, await context.Alerts.CountAsync());
        Assert.Equal(1, await context.AdminLoginAttempts.CountAsync());
        Assert.Equal(1, await context.Feedbacks.CountAsync());
    }

    // Wrapper to expose the private/protected method for testing
    private class DatabaseCleanupServiceWrapper : DatabaseCleanupService
    {
        public DatabaseCleanupServiceWrapper(IDbContextFactory<ApplicationDbContext> factory, ISettingsService settingsService, ILogger<DatabaseCleanupService> logger) 
            : base(factory, settingsService, logger) { }

        public async Task PublicCleanupDatabaseAsync()
        {
            // Use reflection to call the private method CleanupDatabaseAsync
            var method = typeof(DatabaseCleanupService).GetMethod("CleanupDatabaseAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method!.Invoke(this, null)!;
        }
    }
}
