namespace ECommerce.Domain.Enums;

public enum UserStatus
{
    Disabled = 0,
    Normal = 1
}

public enum ProductStatus
{
    OffShelf = 0,
    OnShelf = 1,
    Presale = 2
}

public enum SkuStatus
{
    Disabled = 0,
    Enabled = 1
}

public enum CouponType
{
    FullReduction = 1,
    Discount = 2
}

public enum UserCouponStatus
{
    Unused = 0,
    Used = 1,
    Expired = 2
}

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3
}

public enum LogisticsStatus
{
    Collected = 0,
    InTransit = 1,
    Delivering = 2,
    Signed = 3
}

public enum ReviewStatus
{
    Pending = 0,
    Published = 1,
    Blocked = 2
}

public enum OperationResult
{
    Failed = 0,
    Success = 1
}

