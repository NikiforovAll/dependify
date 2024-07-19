namespace Dependify.Core;

using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Extensions.Logging;

public class ProjectLocator(ILogger<ProjectLocator> logger)
{
    private const string SolutionFileExtension = "*.sln";
    private const string ProjectFileExtension = "*.csproj";

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
        if (File.Exists(path))
        {
            if (Path.GetExtension(path) == ProjectFileExtension)
            {
                return [new ProjectReferenceNode(path)];
            }

            if (Path.GetExtension(path) == SolutionFileExtension)
            {
                return [new SolutionReferenceNode(path)];
            }
        }

        if (Directory.Exists(path))
        {
            var projects = Directory
                .GetFiles(path, ProjectFileExtension, searchOption)
                .Select<string, Node>(p => new ProjectReferenceNode(p));

            var solutions = Directory
                .GetFiles(path, SolutionFileExtension, searchOption)
                .Select<string, Node>(s => new SolutionReferenceNode(s));

            return projects.Concat(solutions);
        }

        throw new ArgumentException("The specified path does not exist.", nameof(path));
    }
}
