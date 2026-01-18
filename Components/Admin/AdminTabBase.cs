using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Components.Admin;

public abstract class AdminTabBase<TEntity> : ComponentBase, IDisposable 
    where TEntity : class, IAuditable
{
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected ILogger<AdminTabBase<TEntity>> Logger { get; set; } = null!;

    [Parameter] public bool IsMobile { get; set; }

    protected List<TEntity> Items = [];
    protected RadzenDataGrid<TEntity>? Grid;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        EventService.OnEntityChanged += HandleEntityChanged;
    }

    protected abstract Task<List<TEntity>> GetEntitiesAsync();

    protected virtual async Task LoadData()
    {
        try
        {
            Items = await GetEntitiesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading items for {Type}", typeof(TEntity).Name);
        }
    }

    protected virtual void HandleEntityChanged(object entity, EntityChangeType type)
    {
        if (entity is TEntity typedEntity)
        {
            InvokeAsync(async () =>
            {
                var index = Items.FindIndex(i => i.Id == typedEntity.Id);
                
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
                        // For admins, we usually keep it in the list but show it as deleted
                        // The Updated event should have already set DeletedAt
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

    public virtual void Dispose()
    {
        EventService.OnEntityChanged -= HandleEntityChanged;
    }
}
