namespace WhereAreThey.Models;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
}
