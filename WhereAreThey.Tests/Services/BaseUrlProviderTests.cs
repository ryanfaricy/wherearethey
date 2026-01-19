using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class BaseUrlProviderTests
{
    [Fact]
    public void GetBaseUrl_ShouldUseHttpContextIfNavManagerFails()
    {
        // Arrange
        var mockHttp = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("test.com", 8080),
            },
        };
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
        mockHttp.Setup(h => h.HttpContext).Returns((HttpContext)null!);

        var options = Options.Create(new AppOptions { BaseUrl = "https://config.com/" });
        
        var provider = new BaseUrlProvider(null, mockHttp.Object, options);

        // Act
        var result = provider.GetBaseUrl();

        // Assert
        Assert.Equal("https://config.com", result);
    }
}
