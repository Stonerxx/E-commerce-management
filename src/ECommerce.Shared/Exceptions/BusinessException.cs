namespace ECommerce.Shared.Exceptions;

public sealed class BusinessException : Exception
{
    public BusinessException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
