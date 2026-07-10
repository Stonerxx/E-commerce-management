namespace ECommerce.Domain.Entities;

/// <summary>
/// ADDRESS 表实体：保存用户收货地址，一个用户最多一条默认地址。
/// </summary>
public sealed class Address
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string DetailAddress { get; set; } = string.Empty;

    public int IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
}
