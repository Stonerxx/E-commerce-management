using System;

namespace ECommerce.Infrastructure.Models;

/// <summary>
/// 购物车项联表查询结果（CART + SKU + PRODUCT）
/// </summary>
public class CartItemWithDetails
{
    public long CartItemId { get; set; }
    public long SkuId { get; set; }
    public long ProductId { get; set; }
    public int CategoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SpecDescJson { get; set; } = string.Empty;
    public string MainImage { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool Selected { get; set; }
    public DateTime UpdatedAt { get; set; }
}
