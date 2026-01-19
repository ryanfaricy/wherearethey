using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Validators;

public class OptionsValidationTests
{
    [Fact]
    public void AppOptions_Validation_ShouldFail_WhenRequiredFieldsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseUrl"] = "", // Empty BaseUrl
                ["AdminPassword"] = "short", // Too short password
            })
            .Build();

        services.AddOptions<AppOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<AppOptions>>();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains("BaseUrl", exception.Message);
        Assert.Contains("AdminPassword", exception.Message);
    }

    [Fact]
    public void SquareOptions_Validation_ShouldFail_WhenRequiredFieldsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Square:ApplicationId"] = "",
                ["Square:AccessToken"] = "",
                ["Square:LocationId"] = "",
                ["Square:Environment"] = "",
            })
            .Build();

        services.AddOptions<SquareOptions>()
            .Bind(configuration.GetSection("Square"))
            .ValidateDataAnnotations();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<SquareOptions>>();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains("ApplicationId", exception.Message);
        Assert.Contains("AccessToken", exception.Message);
        Assert.Contains("LocationId", exception.Message);
        Assert.Contains("Environment", exception.Message);
    }

    [Fact]
    public void EmailOptions_Validation_ShouldFail_WhenRequiredFieldsMissingOrInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromEmail"] = "not-an-email",
                ["Email:FromName"] = "",
                ["Email:SmtpPort"] = "70000", // Out of range
            })
            .Build();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection("Email"))
            .ValidateDataAnnotations();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains("FromEmail", exception.Message);
        Assert.Contains("FromName", exception.Message);
        Assert.Contains("SmtpPort", exception.Message);
    }
}
