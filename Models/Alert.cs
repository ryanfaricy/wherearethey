namespace WhereAreThey.Models;

public class Alert : IAuditable
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; } = Guid.NewGuid();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; }
    public string? Message { get; set; }
    public string? EncryptedEmail { get; set; }
    public string? EmailHash { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? UserIdentifier { get; set; }
    
    public string LocationDisplay(int digits = 2) => $"{Latitude.ToString($"F{digits}")}, {Longitude.ToString($"F{digits}")}";
}
