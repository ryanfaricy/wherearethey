namespace WhereAreThey.Services.Interfaces;

public interface IHapticFeedbackService
{
    ValueTask VibrateSuccessAsync();
    ValueTask VibrateErrorAsync();
    ValueTask VibrateWarningAsync();
    ValueTask VibrateEmergencyAsync();
    ValueTask VibrateUpdateAsync();
    ValueTask VibrateAsync(int duration);
    ValueTask VibrateAsync(int[] pattern);
}
