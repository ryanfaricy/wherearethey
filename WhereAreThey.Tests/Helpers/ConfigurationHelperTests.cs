using Microsoft.Extensions.Configuration;
using WhereAreThey.Helpers;

namespace WhereAreThey.Tests.Helpers;

public class ConfigurationHelperTests
{
    [Fact]
    public void GetConnectionString_FromConfig_WhenEnvVarMissing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Host=config;Database=db"},
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        Environment.SetEnvironmentVariable("DATABASE_URL", null);

        // Act
        var result = ConfigurationHelper.GetConnectionString(configuration);

        // Assert
        Assert.Equal("Host=config;Database=db", result);
    }

    [Fact]
    public void GetConnectionString_FromEnvVar_WhenNotPostgresUri()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var rawConnStr = "Host=env;Database=db";
        Environment.SetEnvironmentVariable("DATABASE_URL", rawConnStr);

        // Act
        var result = ConfigurationHelper.GetConnectionString(configuration);

        // Assert
        Assert.Equal(rawConnStr, result);
    }

    [Fact]
    public void GetConnectionString_ParsesPostgresUri()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var pgUrl = "postgres://user:pass@host:5432/dbname";
        Environment.SetEnvironmentVariable("DATABASE_URL", pgUrl);

        // Act
        var result = ConfigurationHelper.GetConnectionString(configuration);

        // Assert
        Assert.Contains("Host=host", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=dbname", result);
        Assert.Contains("Username=user", result);
        Assert.Contains("Password=pass", result);
    }

    [Fact]
    public void GetConnectionString_ParsesPostgresqlUri()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var pgUrl = "postgresql://user:pass@host:5432/dbname";
        Environment.SetEnvironmentVariable("DATABASE_URL", pgUrl);

        // Act
        var result = ConfigurationHelper.GetConnectionString(configuration);

        // Assert
        Assert.Contains("Host=host", result);
    }

    [Fact]
    public void GetConnectionString_ReturnsRaw_WhenNoValidHost()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var invalidPgUrl = "not-a-uri";
        Environment.SetEnvironmentVariable("DATABASE_URL", invalidPgUrl);

        // Act
        var result = ConfigurationHelper.GetConnectionString(configuration);

        // Assert
        Assert.Equal(invalidPgUrl, result);
    }
}
