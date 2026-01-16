namespace WhereAreThey.Models;

public class GeolocationPosition
{
    public GeolocationCoordinates Coords { get; set; } = new();
}

public class GeolocationCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
}
