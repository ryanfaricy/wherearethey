namespace WhereAreThey.Models;

public class AdminLoginAttempt
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public bool IsSuccessful { get; set; }
}
