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
        MapState.OnStateChanged += HandleStateChanged;
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
    /// Unsubscribes from events when the component is disposed.
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        
        EventService.OnEntityChanged -= HandleEntityChanged;
        MapState.OnStateChanged -= HandleStateChanged;
    }
}
