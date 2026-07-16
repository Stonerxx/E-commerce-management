using ECommerce.Shared.Errors;

namespace ECommerce.Web.Errors;

public static class BusinessExceptionStatusMapper
{
    public static int GetStatusCode(string code)
    {
        if (code is ErrorCodes.AuthInvalidCredentials)
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (code is "FORBIDDEN" or ErrorCodes.AuthForbidden or ErrorCodes.UserDisabled)
        {
            return StatusCodes.Status403Forbidden;
        }

        if (code is "NOT_FOUND" or ErrorCodes.ResourceNotFound || code.EndsWith("_NOT_FOUND", StringComparison.Ordinal))
        {
            return StatusCodes.Status404NotFound;
        }

        if (code is "ORDER_STATUS_CHANGED" or "ORDER_STATUS_INVALID" or "ORDER_CANNOT_CANCEL" or "ORDER_CANNOT_CONFIRM"
            or ErrorCodes.PaymentAlreadyPaid or "SKU_STOCK_BELOW_LOCKED" or "STOCK_LOCK_FAILED"
            || code.StartsWith("INVENTORY_", StringComparison.Ordinal))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
