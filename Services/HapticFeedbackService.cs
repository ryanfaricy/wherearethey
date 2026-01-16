using Microsoft.JSInterop;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class HapticFeedbackService(IJSRuntime jsRuntime) : IHapticFeedbackService
{
    public ValueTask VibrateSuccessAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateSuccess");

    public ValueTask VibrateErrorAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateError");

    public ValueTask VibrateWarningAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateWarning");

    public ValueTask VibrateEmergencyAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateEmergency");

    public ValueTask VibrateUpdateAsync() => jsRuntime.InvokeVoidAsync("haptics.vibrateUpdate");

    public ValueTask VibrateAsync(int duration) => jsRuntime.InvokeVoidAsync("haptics.vibrate", duration);

    public ValueTask VibrateAsync(int[] pattern) => jsRuntime.InvokeVoidAsync("haptics.vibrate", pattern);
}
