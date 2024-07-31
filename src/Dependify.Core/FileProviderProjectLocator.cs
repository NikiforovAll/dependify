namespace Dependify.Core;

using Dependify.Core.Graph;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

public class FileProviderProjectLocator(IFileProvider fileProvider, ILogger<ProjectLocator> logger)
{
    private const string SolutionFileExtension = ".sln";
    private const string ProjectFileExtension = ".csproj";

    public string Root =>
        (fileProvider as PhysicalFileProvider)?.Root
        ?? throw new InvalidOperationException("File provider is not a physical file provider.");

    /// <summary>
    /// Scans the specified path for .csproj and solution files.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Node> FullScan()
    {
        return this.Scan();
    }

    /// <summary>
    /// Scans the specified path for .csproj and solution files.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Node> FolderScan()
    {
        return this.Scan(1);
    }

    private IEnumerable<Node> Scan(int maxDepth = -1)
    {
        IEnumerable<Node> result = [];

        var files = fileProvider.FindFiles(
            string.Empty,
            f =>
                f.Name.EndsWith(ProjectFileExtension, StringComparison.OrdinalIgnoreCase)
                || f.Name.EndsWith(SolutionFileExtension, StringComparison.OrdinalIgnoreCase),
            maxDepth
        );

        foreach (var file in files)
        {
            if (file.Name.EndsWith(ProjectFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Append(new ProjectReferenceNode(file.PhysicalPath!));
            }
            else if (file.Name.EndsWith(SolutionFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Append(new SolutionReferenceNode(file.PhysicalPath));
            }
        }

        logger.LogInformation("Located number of items - {Count}", result.Count());

        foreach (var item in result)
        {
            logger.LogDebug("Located item - {Item}", item);
        }

        return result;
    }
}

public static class FileProviderExtensions
{
    public static IEnumerable<IFileInfo> FindFiles(
        this IFileProvider provider,
        string directory,
        Predicate<IFileInfo> match,
        int maxDepth = -1
    )
    {
        var dirsToSearch = new Stack<string>();
        dirsToSearch.Push(directory);

        var depth = 0;

        while (dirsToSearch.Count > 0)
        {
            var dir = dirsToSearch.Pop();
            foreach (var file in provider.GetDirectoryContents(dir))
            {
                if (file.IsDirectory)
                {
                    if (maxDepth != -1 && depth >= maxDepth)
                    {
                        continue;
                    }

                    var relPath = Path.Join(dir, file.Name);
                    dirsToSearch.Push(relPath);
                }
                else
                {
                    if (!match(file))
                    {
                        continue;
                    }

                    yield return file;
                }
            }

            depth++;
        }
    }
}
