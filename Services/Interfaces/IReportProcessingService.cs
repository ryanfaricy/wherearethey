using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IReportProcessingService
{
    Task ProcessReportAsync(LocationReport report);
}
