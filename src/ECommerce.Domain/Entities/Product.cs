namespace ECommerce.Domain.Entities;

public class Product
{
    public long Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MainImage { get; set; } = string.Empty;
    public int Status { get; set; }
    public decimal PriceMin { get; set; }
    public int SalesCount { get; set; }
    public int ViewCount { get; set; }
    public decimal AvgRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}