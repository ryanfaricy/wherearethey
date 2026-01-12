namespace WhereAreThey.Models;

public class EmailVerification
{
    public int Id { get; set; }
    public required string EmailHash { get; set; }
    public required string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
