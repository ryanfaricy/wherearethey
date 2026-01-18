using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public abstract class BaseService<T>(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService) 
    where T : class, IAuditable
{
    protected readonly IDbContextFactory<ApplicationDbContext> ContextFactory = contextFactory;
    protected readonly IEventService EventService = eventService;

    public virtual async Task<List<T>> GetAllAsync(bool isAdmin = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = context.Set<T>().AsTracking();
        
        if (isAdmin)
        {
            query = query.IgnoreQueryFilters(); // Admins see deleted items
        }

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public virtual async Task<Result> SoftDeleteAsync(int id)
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

            // Generic notification
            EventService.NotifyEntityChanged(entity, EntityChangeType.Updated);
            EventService.NotifyEntityChanged(entity, EntityChangeType.Deleted);
            
            // Call the specialized notification methods for backward compatibility
            NotifyDeleted(entity);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred while deleting the entity: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Derived classes must implement this to call the legacy specialized event service methods.
    /// </summary>
    protected abstract void NotifyUpdated(T entity);
    
    /// <summary>
    /// Derived classes must implement this to call the legacy specialized event service methods.
    /// </summary>
    protected abstract void NotifyDeleted(T entity);
}
