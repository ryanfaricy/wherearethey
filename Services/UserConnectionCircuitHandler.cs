using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WhereAreThey.Services;

public class UserConnectionCircuitHandler(UserConnectionService connectionService) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        connectionService.Increment();
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        connectionService.Decrement();
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}
