namespace WhereAreThey.Models;

public class Feedback : IAuditable
{
    public int Id { get; set; }
    public string Type { get; set; } = "Bug"; // "Bug" or "Feature"
    public string Message { get; set; } = string.Empty;
    public string? UserIdentifier { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
