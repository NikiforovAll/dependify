namespace Dependify.Core;

internal static class Utils
{
    public static string NormalizePath(this string path) => path.Replace('\\', '/');
}
