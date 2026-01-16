using Microsoft.AspNetCore.Hosting;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests;

public class EmailTemplateServiceTests
{
    private readonly Mock<IWebHostEnvironment> _envMock = new();
    private readonly string _tempPath;

    public EmailTemplateServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_tempPath, "Resources", "EmailTemplates"));
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempPath);
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldRenderCorrectly()
    {
        // Arrange
        var templateContent = "Hello {{ Name }}! Welcome to {{ Place }}.";
        var templatePath = Path.Combine(_tempPath, "Resources", "EmailTemplates", "TestTemplate.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var service = new EmailTemplateService(_envMock.Object);
        var model = new { Name = "John", Place = "Earth" };

        // Act
        var result = await service.RenderTemplateAsync("TestTemplate", model);

        // Assert
        Assert.Equal("Hello John! Welcome to Earth.", result);
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldHandleAlertEmailViewModel()
    {
        // Arrange
        var templateContent = "Alert: {{ AlertMessage }}, Address: {{ Address }}, Time: {{ LocalTimeStr }}, Emergency: {{ IsEmergency }}, Lat: {{ Latitude }}";
        var templatePath = Path.Combine(_tempPath, "Resources", "EmailTemplates", "AlertEmailTest.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var service = new EmailTemplateService(_envMock.Object);
        var model = new AlertEmailViewModel
        {
            AlertMessage = "Test Alert",
            Address = "123 Main St",
            LocalTimeStr = "2026-01-15 12:00",
            IsEmergency = true,
            Latitude = "40.1234",
            Longitude = "-74.1234"
        };

        // Act
        var result = await service.RenderTemplateAsync("AlertEmailTest", model);
        
        // Assert
        Assert.Contains("Alert: Test Alert", result);
        Assert.Contains("Address: 123 Main St", result);
        Assert.Contains("Time: 2026-01-15 12:00", result);
        Assert.Contains("Emergency: true", result); // Fluid renders bool as true/false lowercase
        Assert.Contains("Lat: 40.1234", result);
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldThrowIfTemplateNotFound()
    {
        // Arrange
        var service = new EmailTemplateService(_envMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.RenderTemplateAsync("NonExistent", new { }));
    }
}
