using GeoTimeZone;
using TimeZoneConverter;
using Xunit;

namespace WhereAreThey.Tests;

public class TimezoneTests
{
    [Theory]
    [InlineData(40.7128, -74.0060, "America/New_York")] // NYC
    [InlineData(51.5074, -0.1278, "Europe/London")] // London
    [InlineData(35.6762, 139.6503, "Asia/Tokyo")] // Tokyo
    [InlineData(-33.8688, 151.2093, "Australia/Sydney")] // Sydney
    public void GetTimeZone_ShouldReturnCorrectId(double lat, double lng, string expectedIanaId)
    {
        // Act
        var result = TimeZoneLookup.GetTimeZone(lat, lng);

        // Assert
        Assert.Equal(expectedIanaId, result.Result);
    }

    [Fact]
    public void ConvertToLocalTime_ShouldWork()
    {
        // Arrange
        var utcNow = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var lat = 40.7128; // NYC
        var lng = -74.0060;

        // Act
        var tzResult = TimeZoneLookup.GetTimeZone(lat, lng);
        var tzInfo = TZConvert.GetTimeZoneInfo(tzResult.Result);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tzInfo);

        // Assert
        // NYC is UTC-5 in January
        Assert.Equal(7, localTime.Hour); 
    }

    [Fact]
    public void TZConvert_ShouldHandleBothNamingSchemes()
    {
        // This test verifies that TZConvert can handle both IANA and Windows IDs 
        // regardless of which platform the test is running on.
        
        // IANA to TimeZoneInfo
        var ianaInfo = TZConvert.GetTimeZoneInfo("America/New_York");
        Assert.NotNull(ianaInfo);
        
        // Windows to TimeZoneInfo
        var winInfo = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        Assert.NotNull(winInfo);
        
        // They should represent the same timezone logic
        Assert.Equal(ianaInfo.BaseUtcOffset, winInfo.BaseUtcOffset);
    }

    [Fact]
    public void DateTimeKind_ShouldBeHandledRobustly()
    {
        // Arrange
        var localTimeSource = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var tzInfo = TZConvert.GetTimeZoneInfo("America/New_York");

        // Act & Assert
        // TimeZoneInfo.ConvertTimeFromUtc throws if kind is Local
        Assert.Throws<ArgumentException>(() => TimeZoneInfo.ConvertTimeFromUtc(localTimeSource, tzInfo));

        // Our logic handles it by specifying kind (or ensuring UTC)
        var fixedTime = DateTime.SpecifyKind(localTimeSource, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(fixedTime, tzInfo);
        Assert.Equal(7, localTime.Hour);
    }
}
