namespace WhereAreThey.Models;

public class WebPushSubscription : IAuditable
{
    public int Id { get; set; }
    public string UserIdentifier { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256DH { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
