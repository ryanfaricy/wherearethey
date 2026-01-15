using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IReportService
{
    Task<LocationReport> AddReportAsync(LocationReport report);
    Task<LocationReport?> GetReportByExternalIdAsync(Guid externalId);
    Task<List<LocationReport>> GetRecentReportsAsync(int? hours = null);
    Task<List<LocationReport>> GetAllReportsAsync();
    Task DeleteReportAsync(int id);
}
