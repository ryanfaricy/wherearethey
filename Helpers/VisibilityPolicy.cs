namespace WhereAreThey.Helpers;

using Models;

/// <summary>
/// Centralizes visibility rules for auditable entities across the application.
/// </summary>
public static class VisibilityPolicy
{
    /// <summary>
    /// Determines whether an entity should be shown to a user based on their role and the entity's deletion status.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="isAdmin">Whether the current user has administrative privileges.</param>
    /// <param name="showDeleted">Whether to show soft-deleted items (Admin only).</param>
    /// <returns>True if the entity should be visible; otherwise, false.</returns>
    public static bool ShouldShow(IAuditable entity, bool isAdmin, bool showDeleted = false) 
        => (isAdmin && showDeleted) || entity.DeletedAt == null;
}
