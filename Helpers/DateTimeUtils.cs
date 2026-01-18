namespace WhereAreThey.Helpers;

public static class DateTimeUtils
{
    public static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalSeconds < 30)
        {
            return "just now";
        }

        if (span.TotalMinutes < 1)
        {
            return "less than a minute ago";
        }

        if (span.TotalMinutes < 60)
        {
            return $"{(int)span.TotalMinutes}m ago";
        }

        if (span.TotalHours < 24)
        {
            return $"{(int)span.TotalHours}h ago";
        }

        return $"{(int)span.TotalDays}d ago";
    }
}
