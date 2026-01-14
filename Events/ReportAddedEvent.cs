using MediatR;
using WhereAreThey.Models;

namespace WhereAreThey.Events;

public record ReportAddedEvent(LocationReport Report) : INotification;
