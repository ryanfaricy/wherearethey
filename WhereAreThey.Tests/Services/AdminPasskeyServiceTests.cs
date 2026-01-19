using System.Text.Json;
using Fido2NetLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Tests.Services;

public class AdminPasskeyServiceTests
{
    [Fact]
    public async Task GetRegistrationOptionsAsync_StructureCheck()
    {
        // Arrange
        var config = new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "Test",
        };
        var fido2 = new Fido2(config);
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        var eventServiceMock = new Mock<IEventService>();
        var loggerMock = new Mock<ILogger<AdminPasskeyService>>();

        var service = new AdminPasskeyService(fido2, mockFactory.Object, eventServiceMock.Object, loggerMock.Object);

        // Act
        var registrationOptions = await service.GetRegistrationOptionsAsync("admin@test.com");
        var json = registrationOptions.ToJson();

        // Assert
        // In Fido2 v4.0.0, ToJson() returns the options directly
        using var doc = JsonDocument.Parse(json);
        var hasChallenge = doc.RootElement.TryGetProperty("challenge", out _);
        var hasPublicKey = doc.RootElement.TryGetProperty("publicKey", out _);
        
        Assert.True(hasChallenge, $"JSON should have 'challenge' property at root. Actual JSON: {json}");
        Assert.False(hasPublicKey, "JSON should NOT have 'publicKey' property at root (it's the root itself).");
        Assert.False(doc.RootElement.TryGetProperty("status", out _), "JSON should NOT have 'status' property at root.");
    }

    [Fact]
    public async Task GetAssertionOptionsAsync_StructureCheck()
    {
        // Arrange
        var config = new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "Test",
        };
        var fido2 = new Fido2(config);
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        var eventServiceMock = new Mock<IEventService>();
        var loggerMock = new Mock<ILogger<AdminPasskeyService>>();

        var service = new AdminPasskeyService(fido2, mockFactory.Object, eventServiceMock.Object, loggerMock.Object);

        // Act
        var assertionOptions = await service.GetAssertionOptionsAsync();
        var json = assertionOptions.ToJson();

        // Assert
        using var doc = JsonDocument.Parse(json);
        var hasChallenge = doc.RootElement.TryGetProperty("challenge", out _);
        var hasPublicKey = doc.RootElement.TryGetProperty("publicKey", out _);
        
        Assert.True(hasChallenge, $"JSON should have 'challenge' property at root. Actual JSON: {json}");
        Assert.False(hasPublicKey, "JSON should NOT have 'publicKey' property at root.");
    }
}
