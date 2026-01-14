using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface IGeocodingService
{
    Task<List<GeocodingResult>> SearchAsync(string query);
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude);
}
