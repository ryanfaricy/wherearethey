using MediatR;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Events.Handlers;

public class ReportAddedHandler(IReportProcessingService reportProcessingService) : INotificationHandler<ReportAddedEvent>
{
    public async Task Handle(ReportAddedEvent notification, CancellationToken cancellationToken)
    {
        // Process alerts in the background to not block the reporter
        // We use Task.Run here because MediatR Publish awaits all handlers, 
        // and we want to return to the user quickly.
        _ = Task.Run(async () => await reportProcessingService.ProcessReportAsync(notification.Report), cancellationToken);
    }
}
