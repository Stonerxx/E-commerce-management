namespace ECommerce.Shared.Errors;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthForbidden = "AUTH_FORBIDDEN";
    public const string UserDisabled = "USER_DISABLED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ProductOffShelf = "PRODUCT_OFF_SHELF";
    public const string SkuNotAvailable = "SKU_NOT_AVAILABLE";
    public const string OrderStockNotEnough = "ORDER_STOCK_NOT_ENOUGH";
    public const string OrderStatusInvalid = "ORDER_STATUS_INVALID";
    public const string CouponNotAvailable = "COUPON_NOT_AVAILABLE";
    public const string PaymentAlreadyPaid = "PAYMENT_ALREADY_PAID";
    public const string ExportFailed = "EXPORT_FAILED";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string ConfigurationError = "CONFIGURATION_ERROR";
}
