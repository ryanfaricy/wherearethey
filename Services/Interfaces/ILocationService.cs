using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface ILocationService
{
    event Action<LocationReport?>? OnReportAdded;
    Task<LocationReport> AddLocationReportAsync(LocationReport report);
    Task<LocationReport?> GetReportByExternalIdAsync(Guid externalId);
    Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null);
    Task<List<LocationReport>> GetReportsInRadiusAsync(double latitude, double longitude, double radiusKm);
    Task<List<LocationReport>> GetAllReportsAsync();
    Task DeleteReportAsync(int id);
}
