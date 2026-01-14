using MediatR;
using WhereAreThey.Models;

namespace WhereAreThey.Events;

public record AlertCreatedEvent(Alert Alert, string Email) : INotification;
