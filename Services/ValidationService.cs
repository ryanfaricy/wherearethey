using Radzen;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class ValidationService(
    NotificationService notificationService,
    IHapticFeedbackService hapticFeedbackService,
    ILogger<ValidationService> logger) : IValidationService
{
    public async Task<bool> ExecuteAsync(
        Func<Task<Result>> operation,
        string? successMessage = null,
        string? errorTitle = null,
        Func<Task>? onSuccess = null,
        Func<string, Task>? onFailure = null,
        bool showSuccessNotification = true,
        bool showErrorNotification = true,
        bool showHapticFeedback = true,
        string? logContext = null)
    {
        try
        {
            var result = await operation();
            if (result.IsSuccess)
            {
                if (showHapticFeedback)
                {
                    await hapticFeedbackService.VibrateSuccessAsync();
                }
                
                if (showSuccessNotification && !string.IsNullOrEmpty(successMessage))
                {
                    notificationService.Notify(NotificationSeverity.Success, successMessage);
                }

                if (onSuccess != null)
                {
                    await onSuccess();
                }

                return true;
            }

            if (showHapticFeedback)
            {
                await hapticFeedbackService.VibrateErrorAsync();
            }

            var errorMessage = result.Error ?? "Unknown error";
            
            if (showErrorNotification)
            {
                notificationService.Notify(NotificationSeverity.Error, errorTitle ?? "Error", errorMessage);
            }

            if (onFailure != null)
            {
                await onFailure(errorMessage);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing operation: {Context}", logContext ?? "N/A");

            if (showHapticFeedback)
            {
                await hapticFeedbackService.VibrateErrorAsync();
            }
            
            if (showErrorNotification)
            {
                notificationService.Notify(NotificationSeverity.Error, errorTitle ?? "Error", ex.Message);
            }

            if (onFailure != null)
            {
                await onFailure(ex.Message);
            }

            return false;
        }
    }

    public async Task<T?> ExecuteAsync<T>(
        Func<Task<Result<T>>> operation,
        string? successMessage = null,
        string? errorTitle = null,
        Func<T, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null,
        bool showSuccessNotification = true,
        bool showErrorNotification = true,
        bool showHapticFeedback = true,
        string? logContext = null)
    {
        T? value = default;
        
        await ExecuteAsync(
            async () =>
            {
                var result = await operation();
                if (result.IsSuccess)
                {
                    value = result.Value;
                }
                return result;
            },
            successMessage,
            errorTitle,
            onSuccess: async () =>
            {
                if (onSuccess != null && value != null)
                {
                    await onSuccess(value);
                }
            },
            onFailure,
            showSuccessNotification,
            showErrorNotification,
            showHapticFeedback,
            logContext);

        return value;
    }
}
