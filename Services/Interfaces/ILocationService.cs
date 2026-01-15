using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface ILocationService
{
    Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm);
    string GetFormattedLocalTime(double latitude, double longitude, DateTime utcTimestamp);
}
