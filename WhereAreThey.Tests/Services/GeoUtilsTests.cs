using WhereAreThey.Models;
using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class GeoUtilsTests
{
    private class TestLocatable : ILocatable
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    [Fact]
    public void ToLocationString_ShouldFormatCoordinates()
    {
        // Arrange
        var loc = new TestLocatable { Latitude = 40.7128, Longitude = -74.0060 };

        // Act & Assert
        Assert.Equal("40.71, -74.01", loc.ToLocationString());
        Assert.Equal("40.7128, -74.0060", loc.ToLocationString(4));
    }

    [Theory]
    [InlineData(40.7128, -74.0060, 40.7128, -74.0060, 0)] // Same point
    [InlineData(40.7128, -74.0060, 40.7580, -73.9855, 5.33)] // NYC to Times Square approx
    [InlineData(51.5074, -0.1278, 48.8566, 2.3522, 343.5)] // London to Paris approx
    public void CalculateDistance_ShouldReturnCorrectDistance(double lat1, double lon1, double lat2, double lon2, double expectedKm)
    {
        // Act
        var result = GeoUtils.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        if (expectedKm == 0)
        {
            Assert.Equal(0, result);
        }
        else
        {
            // Allow 1% margin of error for Haversine approximations
            Assert.InRange(result, expectedKm * 0.99, expectedKm * 1.01);
        }
    }

    [Fact]
    public void GetBoundingBox_ReturnsCorrectRange()
    {
        // Arrange
        var lat = 45.0;
        var lon = 45.0;
        var radius = 111.0; // roughly 1 degree

        // Act
        var (minLat, maxLat, minLon, maxLon) = GeoUtils.GetBoundingBox(lat, lon, radius);

        // Assert
        Assert.True(minLat < lat);
        Assert.True(maxLat > lat);
        Assert.True(minLon < lon);
        Assert.True(maxLon > lon);
        
        // 1 degree lat is approx 111km
        Assert.Equal(1.0, maxLat - lat, 1);
    }
}
