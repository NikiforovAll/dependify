namespace Dependify.Core;

using Buildalyzer;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

public class MsBuildService(ILoggerFactory loggerFactory)
{
    public DependencyGraph AnalyzeReferences(
        SolutionReferenceNode solution,
        bool includePackages = false,
        string? framework = default
    )
    {
        var analyzerManager = new AnalyzerManager(
            solution.Path,
            new AnalyzerManagerOptions { LoggerFactory = loggerFactory, }
        );

        var builder = new DependencyGraph.Builder(solution);

        var projects = analyzerManager.Projects.Where(p =>
            p.Value.ProjectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat
        );

        foreach (var project in projects)
        {
            AddDependenciesToGraph(builder, project.Value, project.Key, includePackages, framework);
        }

        return builder.Build();
    }

    public DependencyGraph AnalyzeReferences(
        IEnumerable<ProjectReferenceNode> nodes,
        bool includePackages = false,
        string? framework = default
    )
    {
        var analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions { LoggerFactory = loggerFactory, });

        var builder = new DependencyGraph.Builder(new SolutionReferenceNode());

        foreach (var path in nodes.Select(n => n.Path))
        {
            var project = analyzerManager.GetProject(path);

            AddDependenciesToGraph(builder, project, path, includePackages, framework);
        }

        return builder.Build();
    }

    private static void AddDependenciesToGraph(
        DependencyGraph.Builder builder,
        IProjectAnalyzer projectAnalyzer,
        string projectPath,
        bool includePackages = false,
        string? framework = default
    )
    {
        var analyzeResults = string.IsNullOrEmpty(framework)
            ? projectAnalyzer.Build()
            : projectAnalyzer.Build(framework);

        var analyzerResult = string.IsNullOrEmpty(framework)
            ? analyzeResults.FirstOrDefault()
            : analyzeResults[framework];

        _ = analyzerResult ?? throw new InvalidOperationException("Unable to load project.");

        var projectNode = new ProjectReferenceNode(projectPath);

        builder.WithNode(projectNode);
        builder.WithEdge(new Edge(builder.Root, projectNode));

        foreach (var reference in analyzerResult.ProjectReferences)
        {
            var referenceNode = new ProjectReferenceNode(reference);
            builder.WithNode(referenceNode);
            builder.WithEdge(new Edge(projectNode, referenceNode));
        }

        if (includePackages)
        {
            foreach (var reference in analyzerResult.PackageReferences)
            {
                var referenceNode = new PackageReferenceNode(
                    reference.Key,
                    reference.Value.FirstOrDefault(a => a.Key is "Version").Value
                );
                builder.WithNode(referenceNode);
                builder.WithEdge(new Edge(projectNode, referenceNode));
            }
        }
    }
}
