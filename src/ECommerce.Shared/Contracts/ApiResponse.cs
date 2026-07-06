namespace ECommerce.Shared.Contracts;

public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T? Data,
    string? TraceId)
{
    public static ApiResponse<T> Ok(T? data, string? traceId = null, string message = "success")
    {
        return new ApiResponse<T>(true, "OK", message, data, traceId);
    }

    public static ApiResponse<T> Fail(
        string code,
        string message,
        string? traceId = null,
        T? data = default)
    {
        return new ApiResponse<T>(false, code, message, data, traceId);
    }
}
