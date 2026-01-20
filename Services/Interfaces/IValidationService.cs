using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service to centralize the execution of operations that return <see cref="Result"/>,
/// handling notifications, haptic feedback, and logging to reduce boilerplate in components.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Executes an operation that returns a <see cref="Result"/>.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="successMessage">Optional message to show on success.</param>
    /// <param name="errorTitle">Optional title for the error notification.</param>
    /// <param name="onSuccess">Optional callback to execute on success.</param>
    /// <param name="onFailure">Optional callback to execute on failure, receiving the error message.</param>
    /// <param name="showSuccessNotification">Whether to show a success notification.</param>
    /// <param name="showErrorNotification">Whether to show an error notification.</param>
    /// <param name="showHapticFeedback">Whether to trigger haptic feedback.</param>
    /// <param name="logContext">Optional context for error logging.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    Task<bool> ExecuteAsync(
        Func<Task<Result>> operation,
        string? successMessage = null,
        string? errorTitle = null,
        Func<Task>? onSuccess = null,
        Func<string, Task>? onFailure = null,
        bool showSuccessNotification = true,
        bool showErrorNotification = true,
        bool showHapticFeedback = true,
        string? logContext = null);

    /// <summary>
    /// Executes an operation that returns a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="successMessage">Optional message to show on success.</param>
    /// <param name="errorTitle">Optional title for the error notification.</param>
    /// <param name="onSuccess">Optional callback to execute on success, receiving the result value.</param>
    /// <param name="onFailure">Optional callback to execute on failure, receiving the error message.</param>
    /// <param name="showSuccessNotification">Whether to show a success notification.</param>
    /// <param name="showErrorNotification">Whether to show an error notification.</param>
    /// <param name="showHapticFeedback">Whether to trigger haptic feedback.</param>
    /// <param name="logContext">Optional context for error logging.</param>
    /// <returns>The result value if successful; otherwise, default.</returns>
    Task<T?> ExecuteAsync<T>(
        Func<Task<Result<T>>> operation,
        string? successMessage = null,
        string? errorTitle = null,
        Func<T, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null,
        bool showSuccessNotification = true,
        bool showErrorNotification = true,
        bool showHapticFeedback = true,
        string? logContext = null);
}
