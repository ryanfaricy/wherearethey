using System.Text.Json.Serialization;

namespace WhereAreThey.Models;

/// <summary>
/// Model matching the JavaScript PushSubscription object (stringified).
/// </summary>
public class PushSubscriptionModel
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("keys")]
    public PushSubscriptionKeys Keys { get; set; } = new();
}

public class PushSubscriptionKeys
{
    [JsonPropertyName("p256dh")]
    public string P256DH { get; set; } = string.Empty;

    [JsonPropertyName("auth")]
    public string Auth { get; set; } = string.Empty;
}
