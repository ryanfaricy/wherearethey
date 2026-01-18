namespace WhereAreThey.Helpers;

using WhereAreThey.Models;

public static class VisibilityPolicy
{
    public static bool ShouldShow(IAuditable entity, bool isAdmin) 
        => isAdmin || entity.DeletedAt == null;
}
