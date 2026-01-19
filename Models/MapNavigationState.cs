namespace WhereAreThey.Models;

public record MapNavigationState
{
    public int? SelectedHours { get; init; }
    public int? FocusReportId { get; init; }
    public double? InitialLat { get; init; }
    public double? InitialLng { get; init; }
    public double? InitialRadius { get; init; }
}
