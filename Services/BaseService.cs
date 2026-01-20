using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IEventService eventService,
    ILogger logger,
    IValidator<T>? validator = null) : IAdminDataService<T>
    where T : class, IAuditable
{
    protected readonly IDbContextFactory<ApplicationDbContext> ContextFactory = contextFactory;
    protected readonly IEventService EventService = eventService;
    protected readonly ILogger Logger = logger;
    protected readonly IValidator<T>? Validator = validator;

    /// <inheritdoc />
    public virtual async Task<List<T>> GetAllAsync(bool isAdmin = false)
    {
        Logger.LogDebug("Retrieving all entities of type {EntityType} (isAdmin: {IsAdmin})", typeof(T).Name, isAdmin);
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = context.Set<T>().AsTracking();
        
        if (isAdmin)
        {
            query = query.IgnoreQueryFilters(); // Admins see deleted items
        }

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<Result> DeleteAsync(int id, bool hardDelete = false)
    {
        Logger.LogInformation("Deleting {EntityType} with ID {Id} (hardDelete: {HardDelete})", typeof(T).Name, id, hardDelete);
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var entity = await context.Set<T>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entity == null)
            {
                Logger.LogWarning("{EntityType} with ID {Id} not found for deletion", typeof(T).Name, id);
                return Result.Failure($"{typeof(T).Name} not found.");
            }

            // If it's already deleted and we are an admin, we hard delete it.
            // OR if hardDelete flag is explicitly set.
            if (hardDelete || entity.DeletedAt != null)
            {
                Logger.LogDebug("Proceeding with hard delete for {EntityType} {Id}", typeof(T).Name, id);
                return await HardDeleteAsync(id);
            }
            Logger.LogDebug("Proceeding with soft delete for {EntityType} {Id}", typeof(T).Name, id);
            return await SoftDeleteAsync(id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while deleting {EntityType} {Id}", typeof(T).Name, id);
            return Result.Failure($"An error occurred while deleting the {typeof(T).Name.ToLower()}: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public virtual async Task<Result> DeleteRangeAsync(IEnumerable<int> ids, bool hardDelete = false)
    {
        Logger.LogInformation("Deleting range of {EntityType} (IDs: {Ids}, hardDelete: {HardDelete})", typeof(T).Name, string.Join(", ", ids), hardDelete);
        if (hardDelete)
        {
            return await HardDeleteRangeAsync(ids);
        }
        return await SoftDeleteRangeAsync(ids);
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

            Logger.LogInformation("Soft-deleted {EntityType} {Id}", typeof(T).Name, id);
            // Notify generic change
            EventService.NotifyEntityChanged(entity, EntityChangeType.Updated);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during soft delete of {EntityType} {Id}", typeof(T).Name, id);
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

            Logger.LogInformation("Soft-deleted {Count} entities of type {EntityType}", entities.Count, typeof(T).Name);
            EventService.NotifyEntityBatchChanged(typeof(T));

            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during soft delete range of {EntityType}", typeof(T).Name);
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

            Logger.LogInformation("Hard-deleted {EntityType} {Id}", typeof(T).Name, id);
            EventService.NotifyEntityChanged(entity, EntityChangeType.Deleted);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during hard delete of {EntityType} {Id}", typeof(T).Name, id);
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

            Logger.LogInformation("Hard-deleted {Count} entities of type {EntityType}", entities.Count, typeof(T).Name);
            EventService.NotifyEntityBatchChanged(typeof(T));

            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during hard delete range of {EntityType}", typeof(T).Name);
            return Result.Failure($"An error occurred while hard-deleting the entities: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public virtual async Task<Result> UpdateAsync(T entity)
    {
        Logger.LogDebug("Updating {EntityType} {Id}", typeof(T).Name, entity.Id);
        if (Validator != null)
        {
            var validationResult = await Validator.ValidateAsync(entity);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Validation failed for {EntityType} {Id}: {Errors}", typeof(T).Name, entity.Id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return Result.Failure(validationResult);
            }
        }
        return await UpdateInternalAsync(entity);
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// Notifies subscribers via <see cref="IEventService.OnEntityChanged"/>.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>A success result if updated; otherwise, a failure result.</returns>
    protected virtual async Task<Result> UpdateInternalAsync(T entity)
    {
        try
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var existing = await context.Set<T>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == entity.Id);

            if (existing == null)
            {
                Logger.LogWarning("{EntityType} with ID {Id} not found for update", typeof(T).Name, entity.Id);
                return Result.Failure($"{typeof(T).Name} not found.");
            }

            context.Entry(existing).CurrentValues.SetValues(entity);
            await context.SaveChangesAsync();

            Logger.LogInformation("Updated {EntityType} {Id}", typeof(T).Name, entity.Id);
            EventService.NotifyEntityChanged(existing, EntityChangeType.Updated);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during update of {EntityType} {Id}", typeof(T).Name, entity.Id);
            return Result.Failure($"An error occurred while updating the entity: {ex.Message}");
        }
    }
}
