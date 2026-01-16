namespace WhereAreThey.Models;

/// <summary>
/// ViewModel for the alert email.
/// </summary>
public class AlertEmailViewModel
{
    /// <summary>
    /// Gets or sets the message from the alert.
    /// </summary>
    public string? AlertMessage { get; set; }

    /// <summary>
    /// Gets or sets the approximate address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the formatted local time.
    /// </summary>
    public string LocalTimeStr { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is an emergency.
    /// </summary>
    public bool IsEmergency { get; set; }

    /// <summary>
    /// Gets or sets the message from the report.
    /// </summary>
    public string? ReportMessage { get; set; }

    /// <summary>
    /// Gets or sets the HTML for the map thumbnail.
    /// </summary>
    public string MapThumbnailHtml { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latitude.
    /// </summary>
    public string Latitude { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the longitude.
    /// </summary>
    public string Longitude { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the heat map.
    /// </summary>
    public string HeatMapUrl { get; set; } = string.Empty;
}
