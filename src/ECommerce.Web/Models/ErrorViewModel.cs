namespace ECommerce.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public string Message { get; set; } = "操作未能完成，请稍后重试。";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
