namespace ECommerce.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TreeLevel { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
