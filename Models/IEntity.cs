namespace WhereAreThey.Models;

/// <summary>
/// Base interface for all entities in the system.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    int Id { get; set; }
}
