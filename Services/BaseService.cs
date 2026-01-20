using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <summary>
/// Abstract base class for services managing <see cref="IAuditable"/> entities.
/// Provides centralized logic for soft-deletion and admin-specific data access.
/// </summary>
/// <typeparam name="T">The type of entity managed by the service.</typeparam>
public abstract class BaseService<T>(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService) 
    where T : class, IAuditable
{
    protected readonly IDbContextFactory<ApplicationDbContext> ContextFactory = contextFactory;
    protected readonly IEventService EventService = eventService;

    /// <summary>
    /// Gets all entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="isAdmin">If true, ignores global query filters to include deleted items.</param>
    /// <returns>A list of entities ordered by creation date descending.</returns>
    protected virtual async Task<List<T>> GetAllAsync(bool isAdmin = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = context.Set<T>().AsTracking();
        
        if (isAdmin)
        {
            query = query.IgnoreQueryFilters(); // Admins see deleted items
        }

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Soft-deletes an entity.
    /// Logic for setting DeletedAt is handled by ApplicationDbContext.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="id">The primary key of the entity to delete.</param>
    /// <returns>A success result if deleted; otherwise, a failure result.</returns>
    protected virtual async Task<Result> SoftDeleteAsync(int id)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.Set<T>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (entity == null)
            {
                return Result.Failure("Not found");
            }

            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();

            // Notify generic change
            EventService.NotifyEntityChanged(entity, EntityChangeType.Updated);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while deleting the entity: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft-deletes multiple entities.
    /// Logic for setting DeletedAt is handled by ApplicationDbContext.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="ids">The primary keys of the entities to delete.</param>
    /// <returns>A success result if deleted; otherwise, a failure result.</returns>
    protected virtual async Task<Result> SoftDeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entities = await context.Set<T>()
                .IgnoreQueryFilters()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();

            if (!entities.Any())
            {
                return Result.Success();
            }

            foreach (var entity in entities)
            {
                context.Set<T>().Remove(entity);
            }
            await context.SaveChangesAsync();

            foreach (var entity in entities)
            {
                EventService.NotifyEntityChanged(entity, EntityChangeType.Updated);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while deleting the entities: {ex.Message}");
        }
    }

    /// <summary>
    /// Permanently deletes an entity using ExecuteDeleteAsync to bypass soft-delete logic in SaveChangesAsync.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="id">The primary key of the entity to delete.</param>
    /// <returns>A success result if deleted; otherwise, a failure result.</returns>
    protected virtual async Task<Result> HardDeleteAsync(int id)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.Set<T>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (entity == null)
            {
                return Result.Failure("Not found");
            }

            await context.Set<T>()
                .IgnoreQueryFilters()
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            EventService.NotifyEntityChanged(entity, EntityChangeType.Deleted);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while hard-deleting the entity: {ex.Message}");
        }
    }

    /// <summary>
    /// Permanently deletes multiple entities using ExecuteDeleteAsync to bypass soft-delete logic in SaveChangesAsync.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="ids">The primary keys of the entities to delete.</param>
    /// <returns>A success result if deleted; otherwise, a failure result.</returns>
    protected virtual async Task<Result> HardDeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entities = await context.Set<T>()
                .IgnoreQueryFilters()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();

            if (!entities.Any())
            {
                return Result.Success();
            }

            await context.Set<T>()
                .IgnoreQueryFilters()
                .Where(e => ids.Contains(e.Id))
                .ExecuteDeleteAsync();

            foreach (var entity in entities)
            {
                EventService.NotifyEntityChanged(entity, EntityChangeType.Deleted);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while hard-deleting the entities: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing entity.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>A success result if updated; otherwise, a failure result.</returns>
    protected virtual async Task<Result> UpdateAsync(T entity)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var existing = await context.Set<T>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == entity.Id);

            if (existing == null)
            {
                return Result.Failure($"{typeof(T).Name} not found.");
            }

            context.Entry(existing).CurrentValues.SetValues(entity);
            await context.SaveChangesAsync();

            EventService.NotifyEntityChanged(existing, EntityChangeType.Updated);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while updating the entity: {ex.Message}");
        }
    }
}
