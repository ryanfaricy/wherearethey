namespace WhereAreThey.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public int ReportExpiryHours { get; set; } = 6;
    public int ReportCooldownMinutes { get; set; } = 5;
    public int AlertLimitCount { get; set; } = 3;
    public decimal MaxReportDistanceMiles { get; set; } = 5.0m;
    public string? MapboxToken { get; set; }
    public bool DonationsEnabled { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public int DataRetentionDays { get; set; } = 30;
    public string? VapidPublicKey { get; set; }
    public string? VapidPrivateKey { get; set; }
}
