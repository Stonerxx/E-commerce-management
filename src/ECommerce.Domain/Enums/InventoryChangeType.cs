namespace ECommerce.Domain.Enums;

public static class InventoryChangeType
{
    public const string Sale = "SALE";
    public const string Cancel = "CANCEL";
    public const string Restock = "RESTOCK";
    public const string Adjust = "ADJUST";
    public const string OrderLock = "ORDER_LOCK";
    public const string OrderRelease = "ORDER_RELEASE";
    public const string OrderDeduct = "ORDER_DEDUCT";
}
