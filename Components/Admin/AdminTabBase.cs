using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using WhereAreThey.Helpers;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Components.Admin;

/// <summary>
/// Base class for administrative tab components that display a list of entities.
/// Handles common logic such as real-time updates via event aggregation and data loading.
/// </summary>
/// <typeparam name="TEntity">The type of entity displayed in the tab.</typeparam>
public abstract class AdminTabBase<TEntity> : LayoutComponentBase, IDisposable 
    where TEntity : class, IAuditable
{
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected IMapStateService MapState { get; set; } = null!;
    [Inject] protected IValidationService ValidationService { get; set; } = null!;
    [Inject] protected ILogger<AdminTabBase<TEntity>> Logger { get; set; } = null!;
    [Inject] protected Microsoft.Extensions.Localization.IStringLocalizer<App> L { get; set; } = null!;
    [Inject] protected IAdminDataService<TEntity> DataService { get; set; } = null!;
    [Inject] protected Radzen.DialogService DialogService { get; set; } = null!;

    /// <summary>
    /// The list of items currently displayed in the tab.
    /// </summary>
    protected List<TEntity> Items = [];

    /// <summary>
    /// Reference to the RadzenDataGrid if applicable.
    /// </summary>
    protected RadzenDataGrid<TEntity>? Grid;

    /// <summary>
    /// The currently selected items in the grid.
    /// </summary>
    protected IList<TEntity>? SelectedItems;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        EventService.OnEntityChanged += HandleEntityChanged;
        EventService.OnEntityBatchChanged += HandleEntityBatchChanged;
        MapState.OnStateChanged += HandleStateChanged;
    }

    private void HandleEntityBatchChanged(Type type)
    {
        if (type == typeof(TEntity))
        {
            _ = InvokeAsync(LoadData);
        }
    }

    private void HandleStateChanged() => _ = InvokeAsync(LoadData);

    /// <summary>
    /// Fetches the entities to be displayed. Must be implemented by derived classes.
    /// </summary>
    /// <returns>A list of entities.</returns>
    protected abstract Task<List<TEntity>> GetEntitiesAsync();

    /// <summary>
    /// Loads or reloads data from the service.
    /// </summary>
    protected virtual async Task LoadData()
    {
        try
        {
            var allItems = await GetEntitiesAsync();
            Items = allItems.Where(i => VisibilityPolicy.ShouldShow(i, true, MapState.ShowDeleted)).ToList();
            
            if (Grid != null)
            {
                await Grid.Reload();
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading items for {Type}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// Handles real-time entity change events.
    /// </summary>
    /// <param name="entity">The entity that changed.</param>
    /// <param name="type">The type of change (Added, Updated, Deleted).</param>
    protected virtual void HandleEntityChanged(object entity, EntityChangeType type)
    {
        if (entity is TEntity typedEntity)
        {
            _ = InvokeAsync(async () =>
            {
                var index = Items.FindIndex(i => i.Id == typedEntity.Id);
                
                if (!VisibilityPolicy.ShouldShow(typedEntity, true, MapState.ShowDeleted))
                {
                    if (index != -1)
                    {
                        Items.RemoveAt(index);
                        if (Grid != null)
                        {
                            await Grid.Reload();
                        }
                        StateHasChanged();
                    }
                    return;
                }
                
                switch (type)
                {
                    case EntityChangeType.Added:
                        if (index == -1)
                        {
                            Items.Insert(0, typedEntity);
                        }
                        break;
                    case EntityChangeType.Updated:
                        if (index != -1)
                        {
                            Items[index] = typedEntity;
                        }
                        else
                        {
                            // Might have been paged out or new
                            Items.Insert(0, typedEntity);
                        }
                        break;
                    case EntityChangeType.Deleted:
                        // A Deleted event now uniquely identifies a HARD delete.
                        // It should always be removed from the list.
                        if (index != -1)
                        {
                            Items.RemoveAt(index);
                        }
                        break;
                }

                if (Grid != null)
                {
                    await Grid.Reload();
                }

                StateHasChanged();
            });
        }
    }

    /// <summary>
    /// Deletes a single entity with a confirmation dialog.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    protected virtual async Task DeleteEntity(int id)
    {
        var entityName = typeof(TEntity).Name;
        var isHardDelete = MapState.ShowDeleted;
        var message = isHardDelete 
            ? $"Are you sure you want to PERMANENTLY delete this {entityName.ToLower()}? This cannot be undone." 
            : $"Are you sure you want to delete this {entityName.ToLower()}?";
        var title = isHardDelete ? $"Hard Delete {entityName}" : $"Delete {entityName}";

        var confirm = await DialogService.Confirm(message, title, 
            new Radzen.ConfirmOptions { OkButtonText = isHardDelete ? "PERMANENTLY DELETE" : "Yes", CancelButtonText = "No" });
        
        if (confirm == true)
        {
            await ValidationService.ExecuteAsync(
                () => DataService.DeleteAsync(id, MapState.ShowDeleted),
                successMessage: $"{entityName} deleted",
                errorTitle: "Delete failed",
                showHapticFeedback: false,
                logContext: $"Admin{entityName}Tab.DeleteEntity {id}"
            );
        }
    }

    /// <summary>
    /// Deletes the currently selected entities with a confirmation dialog.
    /// </summary>
    protected virtual async Task DeleteSelected()
    {
        if (SelectedItems == null || !SelectedItems.Any()) return;

        var entityName = typeof(TEntity).Name;
        var count = SelectedItems.Count;
        var isHardDelete = MapState.ShowDeleted;
        var message = isHardDelete 
            ? $"Are you sure you want to PERMANENTLY delete {count} selected {entityName.ToLower()}s? This cannot be undone." 
            : $"Are you sure you want to delete {count} selected {entityName.ToLower()}s?";
        var title = isHardDelete ? $"Hard Delete {entityName}s" : $"Delete {entityName}s";

        var confirm = await DialogService.Confirm(message, title, 
            new Radzen.ConfirmOptions { OkButtonText = isHardDelete ? "PERMANENTLY DELETE ALL" : "Yes", CancelButtonText = "No" });
        
        if (confirm == true)
        {
            var ids = SelectedItems.Select(i => i.Id).ToList();
            await ValidationService.ExecuteAsync(
                async () =>
                {
                    var result = await DataService.DeleteRangeAsync(ids, MapState.ShowDeleted);
                    if (result.IsSuccess)
                    {
                        SelectedItems = null;
                    }
                    return result;
                },
                successMessage: $"{count} {entityName.ToLower()}s deleted",
                errorTitle: "Mass delete failed",
                showHapticFeedback: false,
                logContext: $"Admin{entityName}Tab.DeleteSelected {count} items"
            );
        }
    }

    /// <summary>
    /// Unsubscribes from events when the component is disposed.
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        
        EventService.OnEntityChanged -= HandleEntityChanged;
        EventService.OnEntityBatchChanged -= HandleEntityBatchChanged;
        MapState.OnStateChanged -= HandleStateChanged;
    }
}
