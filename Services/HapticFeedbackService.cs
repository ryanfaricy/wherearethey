using Microsoft.JSInterop;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class HapticFeedbackService(IJSRuntime jsRuntime) : IHapticFeedbackService
{
    /// <inheritdoc />
    public ValueTask VibrateSuccessAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateSuccess");

    /// <inheritdoc />
    public ValueTask VibrateErrorAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateError");

    /// <inheritdoc />
    public ValueTask VibrateWarningAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateWarning");

    /// <inheritdoc />
    public ValueTask VibrateEmergencyAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateEmergency");

    /// <inheritdoc />
    public ValueTask VibrateUpdateAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateUpdate");

    /// <inheritdoc />
    public ValueTask VibrateAsync(int duration) => jsRuntime.InvokeVoidAsync("haptics.vibrate", duration);

    /// <inheritdoc />
    public ValueTask VibrateAsync(int[] pattern) => jsRuntime.InvokeVoidAsync("haptics.vibrate", pattern);
}
