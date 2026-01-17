using WhereAreThey.Models;
using Xunit;

namespace WhereAreThey.Tests;

public class ModelTests
{
    [Fact]
    public void LocationReport_LocationDisplay_ReturnsFormattedString()
    {
        var report = new LocationReport { Latitude = 10.123456, Longitude = -20.654321 };
        Assert.Equal("10.1235, -20.6543", report.LocationDisplay());
        Assert.Equal("10.12, -20.65", report.LocationDisplay(2));
    }

    [Fact]
    public void LocationReport_HasReporterLocation_ChecksValues()
    {
        var report = new LocationReport();
        Assert.False(report.HasReporterLocation());
        
        report.ReporterLatitude = 0;
        Assert.False(report.HasReporterLocation());
        
        report.ReporterLongitude = 0;
        Assert.True(report.HasReporterLocation());
    }

    [Fact]
    public void LocationReport_ReporterLocationDisplay_ReturnsFormattedOrNA()
    {
        var report = new LocationReport();
        Assert.Equal("N/A", report.ReporterLocationDisplay());
        
        report.ReporterLatitude = 1.23456;
        report.ReporterLongitude = 7.89012;
        Assert.Equal("1.2346, 7.8901", report.ReporterLocationDisplay());
    }
}
