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
    /// Soft-deletes an entity by setting its <see cref="IAuditable.DeletedAt"/> property.
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

            entity.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Notify generic change
            EventService.NotifyEntityChanged(entity, EntityChangeType.Updated);
            EventService.NotifyEntityChanged(entity, EntityChangeType.Deleted);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while deleting the entity: {ex.Message}");
        }
    }
}
