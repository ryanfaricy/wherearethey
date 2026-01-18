namespace WhereAreThey.Models;

/// <summary>
/// Specifies the type of change that occurred to an entity.
/// </summary>
public enum EntityChangeType 
{ 
    /// <summary>The entity was newly created.</summary>
    Added, 
    /// <summary>The entity was modified.</summary>
    Updated, 
    /// <summary>The entity was soft-deleted.</summary>
    Deleted,
}
