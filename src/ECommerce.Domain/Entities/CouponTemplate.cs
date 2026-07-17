// 映射了云端 Oracle 数据库中的 COUPON_TEMPLATE 表
namespace ECommerce.Domain.Entities;

public class CouponTemplate
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Type { get; set; }
    
    public decimal Amount { get; set; }
    
    public decimal MinAmount { get; set; }
    
    public int TotalCount { get; set; }
    
    public int ReceivedCount { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public int Status { get; set; }
}
