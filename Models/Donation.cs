namespace WhereAreThey.Models;

public class Donation : IAuditable
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? DonorEmail { get; set; }
    public string? DonorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string Status { get; set; } = "pending"; // pending, completed, failed
    public DateTime? DeletedAt { get; set; }
    
    public bool IsSuccess() => Status is "succeeded" or "completed";
}
