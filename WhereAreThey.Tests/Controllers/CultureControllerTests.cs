using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using WhereAreThey.Controllers;

namespace WhereAreThey.Tests.Controllers;

public class CultureControllerTests
{
    [Fact]
    public void Set_WithCulture_SetsCookieAndRedirects()
    {
        // Arrange
        var controller = new CultureController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        var culture = "es-ES";
        var redirectUri = "/home";

        // Act
        var result = controller.Set(culture, redirectUri);

        // Assert
        var cookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(CookieRequestCultureProvider.DefaultCookieName, cookieHeader);
        Assert.Contains(culture, cookieHeader);

        var redirectResult = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(redirectUri, redirectResult.Url);
    }

    [Fact]
    public void Set_WithoutCulture_OnlyRedirects()
    {
        // Arrange
        var controller = new CultureController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        var redirectUri = "/home";

        // Act
        var result = controller.Set(null, redirectUri);

        // Assert
        Assert.False(httpContext.Response.Headers.ContainsKey("Set-Cookie"));

        var redirectResult = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(redirectUri, redirectResult.Url);
    }
}
