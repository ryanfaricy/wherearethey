namespace WhereAreThey.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public int ReportExpiryHours { get; set; } = 6;
    public int ReportCooldownMinutes { get; set; } = 5;
    public decimal MaxReportDistanceMiles { get; set; } = 5.0m;
}
