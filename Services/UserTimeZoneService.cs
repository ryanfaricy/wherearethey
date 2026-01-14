using TimeZoneConverter;

namespace WhereAreThey.Services;

public class UserTimeZoneService
{
    private TimeZoneInfo _userTimeZone = TimeZoneInfo.Utc;
    public bool IsInitialized { get; private set; }

    public void SetTimeZone(string ianaTimeZoneId)
    {
        if (string.IsNullOrEmpty(ianaTimeZoneId)) return;

        try
        {
            _userTimeZone = TZConvert.GetTimeZoneInfo(ianaTimeZoneId);
            IsInitialized = true;
        }
        catch
        {
            _userTimeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTime ToLocal(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        else if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _userTimeZone);
    }
    
    public string FormatLocal(DateTime utcDateTime, string format = "g")
    {
        return ToLocal(utcDateTime).ToString(format);
    }
}
