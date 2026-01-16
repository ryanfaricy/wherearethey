using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class ClientLocationService : IClientLocationService
{
    private readonly IClientStorageService _storageService;
    private readonly ILogger<ClientLocationService> _logger;
    private TaskCompletionSource<GeolocationPosition?>? _manualPickTcs;

    /// <inheritdoc />
    public bool IsLocating { get; private set; }

    /// <inheritdoc />
    public bool ShowManualPick { get; private set; }

    /// <inheritdoc />
    public GeolocationPosition? LastKnownPosition { get; private set; }

    /// <inheritdoc />
    public DateTime LastLocationUpdate { get; private set; }

    /// <inheritdoc />
    public event Action? OnStateChanged;

    public ClientLocationService(
        IClientStorageService storageService,
        ILogger<ClientLocationService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeolocationPosition?> GetLocationWithFallbackAsync(bool allowManual = true, bool showUI = true)
    {
        if (showUI)
        {
            IsLocating = true;
            ShowManualPick = false;
            _manualPickTcs = new TaskCompletionSource<GeolocationPosition?>();
            OnStateChanged?.Invoke();
        }

        using var cts = new CancellationTokenSource();
        var locationTask = _storageService.GetLocationAsync();
        var timerTask = Task.Delay(7000, cts.Token);

        try
        {
            var firstTask = await Task.WhenAny(locationTask, timerTask);
            if (firstTask == timerTask && !locationTask.IsCompleted)
            {
                if (showUI)
                {
                    ShowManualPick = allowManual;
                    OnStateChanged?.Invoke();

                    var secondTask = await Task.WhenAny(locationTask, _manualPickTcs!.Task);
                    if (secondTask == _manualPickTcs.Task)
                    {
                        await cts.CancelAsync();
                        var manualResult = await _manualPickTcs.Task;
                        if (manualResult != null)
                        {
                            UpdateLastKnownPosition(manualResult);
                        }
                        return manualResult;
                    }
                }
            }

            await cts.CancelAsync();
            var position = await locationTask;
            if (position != null)
            {
                UpdateLastKnownPosition(position);
            }
            return position;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get location");
            return null;
        }
        finally
        {
            if (showUI)
            {
                IsLocating = false;
                ShowManualPick = false;
                OnStateChanged?.Invoke();
            }
        }
    }

    /// <inheritdoc />
    public void ConfirmManualPick(GeolocationPosition? position)
    {
        _manualPickTcs?.TrySetResult(position);
    }

    /// <inheritdoc />
    public void UpdateLastKnownPosition(GeolocationPosition position)
    {
        LastKnownPosition = position;
        LastLocationUpdate = DateTime.UtcNow;
        OnStateChanged?.Invoke();
    }
}
