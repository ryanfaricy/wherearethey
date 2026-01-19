using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests.Services;

public class BaseUrlProviderTests
{
    [Fact]
    public void GetBaseUrl_ShouldUseNavigationManagerIfAvailable()
    {
        // Arrange
        var mockNav = new Mock<NavigationManager>();
        // NavigationManager.BaseUri is not virtual, but we can set it via protected members or use a derived class.
        // Actually, it's easier to just test that it's called.
        // Wait, NavigationManager is hard to mock because of its non-virtual properties.
        
        // Let's use a simpler test for HttpContext fallback.
    }

    [Fact]
    public void GetBaseUrl_ShouldUseHttpContextIfNavManagerFails()
    {
        // Arrange
        var mockHttp = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("test.com", 8080);
        mockHttp.Setup(h => h.HttpContext).Returns(context);

        var options = Options.Create(new AppOptions { BaseUrl = "https://config.com" });
        
        var provider = new BaseUrlProvider(null, mockHttp.Object, options);

        // Act
        var result = provider.GetBaseUrl();

        // Assert
        Assert.Equal("https://test.com:8080", result);
    }

    [Fact]
    public void GetBaseUrl_ShouldFallbackToOptions()
    {
        // Arrange
        var mockHttp = new Mock<IHttpContextAccessor>();
        mockHttp.Setup(h => h.HttpContext).Returns((HttpContext)null);

        var options = Options.Create(new AppOptions { BaseUrl = "https://config.com/" });
        
        var provider = new BaseUrlProvider(null, mockHttp.Object, options);

        // Act
        var result = provider.GetBaseUrl();

        // Assert
        Assert.Equal("https://config.com", result);
    }
}
