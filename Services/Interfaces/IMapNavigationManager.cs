using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IMapNavigationManager
{
    Task<MapNavigationState> GetNavigationStateAsync();
}
