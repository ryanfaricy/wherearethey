using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class UserTimeZoneServiceTests
{
    [Fact]
    public void SetTimeZone_WithValidIanaId_ShouldInitialize()
    {
        var service = new UserTimeZoneService();
        service.SetTimeZone("America/New_York");
        Assert.True(service.IsInitialized);
    }

    [Fact]
    public void ToLocal_ShouldConvertCorrectly()
    {
        var service = new UserTimeZoneService();
        service.SetTimeZone("America/New_York"); // UTC-5 (or UTC-4 in summer)
        
        // 2026-01-13 22:00 UTC is 2026-01-13 17:00 EST
        var utc = new DateTime(2026, 1, 13, 22, 0, 0, DateTimeKind.Utc);
        var local = service.ToLocal(utc);
        
        Assert.Equal(17, local.Hour);
    }

    [Fact]
    public void FormatLocal_ShouldReturnFormattedString()
    {
        var service = new UserTimeZoneService();
        service.SetTimeZone("Europe/London");
        
        var utc = new DateTime(2026, 1, 13, 12, 0, 0, DateTimeKind.Utc);
        var formatted = service.FormatLocal(utc, "HH:mm");
        
        Assert.Equal("12:00", formatted); // GMT in winter
    }

    [Fact]
    public void SetTimeZone_WithWindowsId_ShouldInitialize()
    {
        var service = new UserTimeZoneService();
        service.SetTimeZone("Eastern Standard Time");
        Assert.True(service.IsInitialized);
        
        // 2026-01-13 22:00 UTC is 2026-01-13 17:00 EST
        var utc = new DateTime(2026, 1, 13, 22, 0, 0, DateTimeKind.Utc);
        var local = service.ToLocal(utc);
        
        Assert.Equal(17, local.Hour);
    }
}
