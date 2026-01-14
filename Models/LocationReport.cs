namespace WhereAreThey.Models;

public class LocationReport
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
    public string? ReporterIdentifier { get; set; } // Anonymous identifier (e.g., session ID)
    public double? ReporterLatitude { get; set; }
    public double? ReporterLongitude { get; set; }
    public bool IsEmergency { get; set; }
}
