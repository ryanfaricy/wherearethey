namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for providing haptic feedback via the Web Vibration API.
/// </summary>
public interface IHapticFeedbackService
{
    /// <summary>Triggers a success haptic pattern.</summary>
    ValueTask VibrateSuccessAsync();

    /// <summary>Triggers an error haptic pattern.</summary>
    ValueTask VibrateErrorAsync();

    /// <summary>Triggers a warning haptic pattern.</summary>
    ValueTask VibrateWarningAsync();

    /// <summary>Triggers an emergency haptic pattern.</summary>
    ValueTask VibrateEmergencyAsync();

    /// <summary>Triggers an update/notification haptic pattern.</summary>
    ValueTask VibrateUpdateAsync();

    /// <summary>
    /// Triggers a vibration for a specific duration.
    /// </summary>
    /// <param name="duration">Duration in milliseconds.</param>
    ValueTask VibrateAsync(int duration);

    /// <summary>
    /// Triggers a vibration pattern.
    /// </summary>
    /// <param name="pattern">An array of durations (vibrate, pause, vibrate, ...).</param>
    ValueTask VibrateAsync(int[] pattern);
}
