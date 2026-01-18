namespace WhereAreThey.Models;

/// <summary>
/// Interface for entities that support auditing and soft-deletion.
/// </summary>
public interface IAuditable : IEntity
{
    /// <summary>
    /// Gets or sets when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
