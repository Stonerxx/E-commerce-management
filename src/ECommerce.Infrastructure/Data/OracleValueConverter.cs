using System.Globalization;
using Oracle.ManagedDataAccess.Types;

namespace ECommerce.Infrastructure.Data;

public static class OracleValueConverter
{
    public static long ToInt64(object? value)
    {
        var normalizedValue = Normalize(value);
        return Convert.ToInt64(normalizedValue, CultureInfo.InvariantCulture);
    }

    public static int ToInt32(object? value)
    {
        var normalizedValue = Normalize(value);
        return Convert.ToInt32(normalizedValue, CultureInfo.InvariantCulture);
    }

    private static object Normalize(object? value)
    {
        if (value is null or DBNull)
        {
            throw new InvalidOperationException("Oracle numeric value is null.");
        }

        if (value is OracleDecimal oracleDecimal)
        {
            if (oracleDecimal.IsNull)
            {
                throw new InvalidOperationException("Oracle numeric value is null.");
            }

            return oracleDecimal.Value;
        }

        return value;
    }
}
