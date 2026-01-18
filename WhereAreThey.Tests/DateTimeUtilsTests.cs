using WhereAreThey.Helpers;
using Xunit;

namespace WhereAreThey.Tests;

public class DateTimeUtilsTests
{
    [Fact]
    public void GetTimeAgo_JustNow()
    {
        var now = DateTime.UtcNow;
        var result = DateTimeUtils.GetTimeAgo(now.AddSeconds(-10));
        Assert.Equal("just now", result);
    }

    [Fact]
    public void GetTimeAgo_LessThanAMinute()
    {
        var now = DateTime.UtcNow;
        var result = DateTimeUtils.GetTimeAgo(now.AddSeconds(-45));
        Assert.Equal("less than a minute ago", result);
    }

    [Fact]
    public void GetTimeAgo_Minutes()
    {
        var now = DateTime.UtcNow;
        var result = DateTimeUtils.GetTimeAgo(now.AddMinutes(-5));
        Assert.Equal("5m ago", result);
    }

    [Fact]
    public void GetTimeAgo_Hours()
    {
        var now = DateTime.UtcNow;
        var result = now.AddHours(-3);
        var timeAgoResult = DateTimeUtils.GetTimeAgo(result);
        Assert.Equal("3h ago", timeAgoResult);
    }

    [Fact]
    public void GetTimeAgo_Days()
    {
        var now = DateTime.UtcNow;
        var result = DateTimeUtils.GetTimeAgo(now.AddDays(-2));
        Assert.Equal("2d ago", result);
    }
}

// Helper to avoid issues with UtcNow moving during test if needed, 
// though for these spans it should be stable enough.
// Actually, I should probably mock time if I wanted to be super precise, 
// but for static util it's harder without passing time.
