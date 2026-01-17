namespace WhereAreThey.Models;

public class Donation
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? DonorEmail { get; set; }
    public string? DonorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string Status { get; set; } = "pending"; // pending, completed, failed
    
    public bool IsSuccess() => Status is "succeeded" or "completed";
}
