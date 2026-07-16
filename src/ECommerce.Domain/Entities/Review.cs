namespace ECommerce.Domain.Entities;

public class Review
{
    public long Id { get; set; }
    
    public long OrderId { get; set; }
    
    public long ProductId { get; set; }
    
    public long UserId { get; set; }
    
    public int Rating { get; set; }
    
    public string? Content { get; set; }
    
    public string? Images { get; set; }
    
    public int IsAnonymous { get; set; }
    
    public int Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
