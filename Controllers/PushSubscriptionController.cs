using Microsoft.AspNetCore.Mvc;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Controllers;

/// <summary>
/// Controller for managing web push subscriptions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PushSubscriptionController(IAlertService alertService, ILogger<PushSubscriptionController> logger) : ControllerBase
{
    /// <summary>
    /// Subscribes a user to push notifications.
    /// </summary>
    /// <param name="request">The push subscription request details.</param>
    /// <returns>An IActionResult indicating the result of the operation.</returns>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        if (string.IsNullOrEmpty(request.Endpoint))
        {
            logger.LogWarning("Push subscription attempt failed: Endpoint is missing for user {UserIdentifier}", request.UserIdentifier);
            return BadRequest("Endpoint is required.");
        }

        var subscription = new WebPushSubscription
        {
            UserIdentifier = request.UserIdentifier,
            Endpoint = request.Endpoint,
            P256DH = request.P256dh,
            Auth = request.Auth,
        };

        logger.LogInformation("Adding push subscription for user {UserIdentifier} with endpoint {Endpoint}", request.UserIdentifier, request.Endpoint);
        var result = await alertService.AddPushSubscriptionAsync(subscription);
        if (result.IsSuccess)
        {
            logger.LogInformation("Successfully added push subscription for user {UserIdentifier}", request.UserIdentifier);
            return Ok();
        }

        logger.LogError("Failed to add push subscription for user {UserIdentifier}: {Error}", request.UserIdentifier, result.Error);
        return StatusCode(500, result.Error);
    }
}

/// <summary>
/// Request model for push subscription.
/// </summary>
public class PushSubscriptionRequest
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The push subscription endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The P256DH public key.
    /// </summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// The Auth secret.
    /// </summary>
    public string Auth { get; set; } = string.Empty;
}
