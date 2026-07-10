// 映射了云端 Oracle 数据库中的 COUPON_TEMPLATE 表
namespace ECommerce.Domain.Entities;

public class CouponTemplate
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Type { get; set; }
    
    public decimal FaceValue { get; set; }
    
    public decimal MinConsumption { get; set; }
    
    public int TotalIssue { get; set; }
    
    public int IssuedCount { get; set; }
    
    public DateTime ValidStartTime { get; set; }
    
    public DateTime ValidEndTime { get; set; }
    
    public int Status { get; set; }
}
