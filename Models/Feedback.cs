namespace WhereAreThey.Models;

public class Feedback
{
    public int Id { get; set; }
    public string Type { get; set; } = "Bug"; // "Bug" or "Feature"
    public string Message { get; set; } = string.Empty;
    public string? UserIdentifier { get; set; }
    public DateTime Timestamp { get; set; }
}
