using TimeZoneConverter;

namespace WhereAreThey.Services;

/// <summary>
/// Service for handling user-specific time zone conversions.
/// </summary>
public class UserTimeZoneService(ILogger<UserTimeZoneService> logger)
{
    private TimeZoneInfo _userTimeZone = TimeZoneInfo.Utc;

    /// <summary>
    /// Gets a value indicating whether the time zone has been initialized from the user's browser.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Sets the user's time zone from an IANA ID.
    /// </summary>
    /// <param name="ianaTimeZoneId">The IANA time zone ID (e.g. "America/New_York").</param>
    public void SetTimeZone(string ianaTimeZoneId)
    {
        if (string.IsNullOrEmpty(ianaTimeZoneId))
        {
            return;
        }

        try
        {
            _userTimeZone = TZConvert.GetTimeZoneInfo(ianaTimeZoneId);
            IsInitialized = true;
            logger.LogInformation("User time zone set to {TimeZoneId}", ianaTimeZoneId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set user time zone to {TimeZoneId}, falling back to UTC", ianaTimeZoneId);
            _userTimeZone = TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Converts a UTC DateTime to the user's local time.
    /// </summary>
    /// <param name="utcDateTime">The UTC DateTime to convert.</param>
    /// <returns>The local DateTime.</returns>
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
    
    /// <summary>
    /// Formats a UTC DateTime as local time string.
    /// </summary>
    /// <param name="utcDateTime">The UTC DateTime to format.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The formatted local time string.</returns>
    public string FormatLocal(DateTime utcDateTime, string format = "g")
    {
        return ToLocal(utcDateTime).ToString(format);
    }
}
