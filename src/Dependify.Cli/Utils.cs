namespace Dependify.Cli;

internal static class Utils
{
    public static string RemovePrefix(this string value, string prefix)
    {
        if (value.StartsWith(prefix, StringComparison.InvariantCulture))
        {
            return value[prefix.Length..];
        }

        return value;
    }
}
