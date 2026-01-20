namespace WhereAreThey.Models;

/// <summary>
/// Represents an object with geographical coordinates.
/// </summary>
public interface ILocatable
{
    /// <summary>
    /// Latitude of the location.
    /// </summary>
    double Latitude { get; }

    /// <summary>
    /// Longitude of the location.
    /// </summary>
    double Longitude { get; }
}
