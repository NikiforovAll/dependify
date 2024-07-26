namespace Dependify.Core;

using Dependify.Core.Graph;
using Depends.Core.Graph;

public static class Utils
{
    public static string NormalizePath(this string path) => path.Replace('\\', '/');

    public static string RemovePrefix(this string value, string prefix)
    {
        if (value.StartsWith(prefix, StringComparison.InvariantCulture))
        {
            return value[prefix.Length..].TrimStart('/', '\\');
        }

        return value;
    }

    public static string CalculateCommonPrefix(IEnumerable<Node> nodes)
    {
        var prefix = nodes
            .OfType<ProjectReferenceNode>()
            .Select(n => n.Path)
            .Aggregate(
                (a, b) =>
                    a.Zip(b)
                        .TakeWhile(p => p.First == p.Second)
                        .Select(p => p.First)
                        .Aggregate(string.Empty, (a, b) => a + b)
            );

        prefix = prefix[..prefix.LastIndexOfAny(['/', '\\'])];

        return prefix;
    }

    public static string GetFullPath(string pathArg)
    {
        FileSystemInfo fileSystemInfo;

        if (File.Exists(pathArg))
        {
            fileSystemInfo = new FileInfo(pathArg);
        }
        else if (Directory.Exists(pathArg))
        {
            fileSystemInfo = new DirectoryInfo(pathArg);
        }
        else
        {
            throw new ArgumentException("The specified path does not exist.", nameof(pathArg));
        }

        var path = fileSystemInfo.FullName;
        return path;
    }
}
