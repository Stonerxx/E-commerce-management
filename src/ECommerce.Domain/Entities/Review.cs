namespace ECommerce.Domain.Entities;

public sealed class Review
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public long UserId { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }

    public string? Images { get; set; }

    public bool IsAnonymous { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
