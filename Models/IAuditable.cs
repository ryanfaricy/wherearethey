namespace WhereAreThey.Models;

public interface IAuditable : IEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? DeletedAt { get; set; }
}
