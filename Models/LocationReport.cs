namespace WhereAreThey.Models;

/// <summary>
/// Represents a location report submitted by a user.
/// </summary>
public class LocationReport : IAuditable
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for public URLs.
    /// </summary>
    public Guid ExternalId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Latitude of the incident.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the incident.
    /// </summary>
    public double Longitude { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional message describing the incident.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Anonymous identifier of the reporter.
    /// </summary>
    public string? ReporterIdentifier { get; set; }

    /// <summary>
    /// Latitude of the reporter at the time of submission.
    /// </summary>
    public double? ReporterLatitude { get; set; }

    /// <summary>
    /// Longitude of the reporter at the time of submission.
    /// </summary>
    public double? ReporterLongitude { get; set; }

    /// <summary>
    /// Whether this is an emergency report.
    /// </summary>
    public bool IsEmergency { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Returns a string representation of the incident location.
    /// </summary>
    public string LocationDisplay(int digits = 4) => $"{Latitude.ToString($"F{digits}")}, {Longitude.ToString($"F{digits}")}";
    
    /// <summary>
    /// Checks if the reporter's location is available.
    /// </summary>
    public bool HasReporterLocation() => ReporterLatitude.HasValue && ReporterLongitude.HasValue;
    
    /// <summary>
    /// Returns a string representation of the reporter's location.
    /// </summary>
    public string ReporterLocationDisplay() => HasReporterLocation()
        ? $"{ReporterLatitude!.Value:F4}, {ReporterLongitude!.Value:F4}"
        : "N/A";
}
