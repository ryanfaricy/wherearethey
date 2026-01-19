using System.Text;
using Microsoft.AspNetCore.Http;
using WhereAreThey.Helpers;

namespace WhereAreThey.Tests.Helpers;

public class HangfireDashboardAuthorizationFilterTests
{
    private const string AdminPassword = "test-password";
    private readonly HangfireDashboardAuthorizationFilter _filter = new(AdminPassword);

    [Fact]
    public void Authorize_NoAuthHeader_ReturnsFalseAndSetsChallenge()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var result = _filter.Authorize(httpContext);

        // Assert
        Assert.False(result);
        Assert.Equal(401, httpContext.Response.StatusCode);
        Assert.Equal("Basic realm=\"Hangfire Dashboard\"", httpContext.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public void Authorize_WrongPassword_ReturnsFalseAndSetsChallenge()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var credentials = Convert.ToBase64String("admin:wrong-password"u8.ToArray());
        httpContext.Request.Headers.Authorization = $"Basic {credentials}";

        // Act
        var result = _filter.Authorize(httpContext);

        // Assert
        Assert.False(result);
        Assert.Equal(401, httpContext.Response.StatusCode);
    }

    [Fact]
    public void Authorize_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"admin:{AdminPassword}"));
        httpContext.Request.Headers.Authorization = $"Basic {credentials}";

        // Act
        var result = _filter.Authorize(httpContext);

        // Assert
        Assert.True(result);
    }
}
