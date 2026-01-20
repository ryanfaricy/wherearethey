using Microsoft.AspNetCore.Mvc;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PushSubscriptionController(IAlertService alertService, ILogger<PushSubscriptionController> logger) : ControllerBase
{
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        if (string.IsNullOrEmpty(request.Endpoint))
        {
            return BadRequest("Endpoint is required.");
        }

        var subscription = new WebPushSubscription
        {
            UserIdentifier = request.UserIdentifier,
            Endpoint = request.Endpoint,
            P256DH = request.P256dh,
            Auth = request.Auth,
        };

        var result = await alertService.AddPushSubscriptionAsync(subscription);
        if (result.IsSuccess)
        {
            return Ok();
        }

        return StatusCode(500, result.Error);
    }
}

public class PushSubscriptionRequest
{
    public string UserIdentifier { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
