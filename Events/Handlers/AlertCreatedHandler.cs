using MediatR;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Events.Handlers;

public class AlertCreatedHandler(IAlertService alertService) : INotificationHandler<AlertCreatedEvent>
{
    public async Task Handle(AlertCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Alert.IsVerified)
        {
            var emailHash = AlertService.ComputeHash(notification.Email);
            await alertService.SendVerificationEmailAsync(notification.Email, emailHash);
        }
    }
}
