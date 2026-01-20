using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Generic interface for administrative data operations.
/// </summary>
/// <typeparam name="TEntity">The type of entity.</typeparam>
public interface IAdminDataService<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets all entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="isAdmin">If true, ignores global query filters to include deleted items.</param>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> GetAllAsync(bool isAdmin = false);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="hardDelete">Whether to permanently delete the entity.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, bool hardDelete = false);

    /// <summary>
    /// Deletes multiple entities.
    /// </summary>
    /// <param name="ids">The identifiers of the entities to delete.</param>
    /// <param name="hardDelete">Whether to permanently delete the entities.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteRangeAsync(IEnumerable<int> ids, bool hardDelete = false);
}
