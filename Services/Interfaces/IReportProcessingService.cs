using WhereAreThey.Models;

namespace WhereAreThey.Services;

public interface IReportProcessingService
{
    Task ProcessReportAsync(LocationReport report);
}
