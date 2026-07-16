namespace ECommerce.Infrastructure.Services;

public static class PermissionPathMatcher
{
    public static bool IsMatch(string? pattern, string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var patternSegments = SplitSegments(NormalizePath(pattern));
        var pathSegments = SplitSegments(NormalizePath(requestPath));
        return IsMatch(patternSegments, 0, pathSegments, 0);
    }

    public static string NormalizePath(string? path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    private static string[] SplitSegments(string path) => path
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsMatch(string[] pattern, int patternIndex, string[] path, int pathIndex)
    {
        if (patternIndex == pattern.Length)
        {
            return pathIndex == path.Length;
        }

        var segment = pattern[patternIndex];
        if (segment == "**")
        {
            return IsMatch(pattern, patternIndex + 1, path, pathIndex)
                || (pathIndex < path.Length && IsMatch(pattern, patternIndex, path, pathIndex + 1));
        }

        if (pathIndex == path.Length)
        {
            return false;
        }

        return (segment == "*" || string.Equals(segment, path[pathIndex], StringComparison.OrdinalIgnoreCase))
            && IsMatch(pattern, patternIndex + 1, path, pathIndex + 1);
    }
}
