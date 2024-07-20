namespace Dependify.Core;

using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Extensions.Logging;

public class ProjectLocator(ILogger<ProjectLocator> logger)
{
    private const string SolutionFileExtension = ".sln";
    private const string ProjectFileExtension = ".csproj";

    /// <summary>
    /// Scans the specified path for .csproj and solution files.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IEnumerable<Node> FullScan(string? path)
    {
        return this.Scan(path, SearchOption.AllDirectories);
    }

    /// <summary>
    /// Scans the specified path for .csproj and solution files.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public IEnumerable<Node> FolderScan(string? path)
    {
        return this.Scan(path, SearchOption.TopDirectoryOnly);
    }

    private IEnumerable<Node> Scan(string? path, SearchOption searchOption)
    {
        IEnumerable<Node> result = [];

        if (File.Exists(path) && Path.GetExtension(path) is var extension)
        {
            if (extension == ProjectFileExtension)
            {
                result = result.Append(new ProjectReferenceNode(path));
            }
            else if (extension == SolutionFileExtension)
            {
                result = result.Append(new SolutionReferenceNode(path));
            }
        }
        else if (Directory.Exists(path))
        {
            var projects = Directory
                .GetFiles(path, $"*{ProjectFileExtension}", searchOption)
                .Select<string, Node>(p => new ProjectReferenceNode(p));

            var solutions = Directory
                .GetFiles(path, $"*{SolutionFileExtension}", searchOption)
                .Select<string, Node>(s => new SolutionReferenceNode(s));

            result = projects.Concat(solutions);
        }
        else
        {
            throw new ArgumentException("The specified path does not exist.", nameof(path));
        }

        logger.LogInformation("Located number of items - {Count}", result.Count());

        foreach (var item in result)
        {
            logger.LogDebug("Located item - {Item}", item);
        }

        return result;
    }
}
