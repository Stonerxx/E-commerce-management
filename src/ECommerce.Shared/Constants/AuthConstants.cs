namespace ECommerce.Shared.Constants;

public static class AuthConstants
{
    public const string AuthenticationScheme = "ECommerce.Auth";

    public static class Roles
    {
        public const string User = "USER";
        public const string Service = "SERVICE";
        public const string Admin = "ADMIN";
    }

    public static class Policies
    {
        public const string CustomerOnly = "CustomerOnly";
        public const string ServiceOrAdmin = "ServiceOrAdmin";
        public const string AdminOnly = "AdminOnly";
    }
}
