namespace Dependify.Core;

using Buildalyzer;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

public record MsBuildConfig(bool IncludePackages = false, bool FullScan = false, string? Framework = default);

public class MsBuildService(ILogger<MsBuildService> logger, ILoggerFactory loggerFactory)
{
    public DependencyGraph AnalyzeReferences(SolutionReferenceNode solution, MsBuildConfig config)
    {
        var analyzerManager = new AnalyzerManager(
            solution.Path,
            new AnalyzerManagerOptions { LoggerFactory = loggerFactory, }
        );

        logger.LogInformation("Analyzing solution {Solution}", solution.Path);

        var builder = new DependencyGraph.Builder(solution);

        var projects = analyzerManager.Projects.Where(p =>
            p.Value.ProjectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat
        );

        foreach (var project in projects)
        {
            var projectNode = new ProjectReferenceNode(project.Key);
            builder.WithEdge(new Edge(builder.Root, projectNode));

            this.AddDependenciesToGraph(builder, project.Value, projectNode, config);
        }

        if (config.FullScan)
        {
            List<ProjectReferenceNode> nodesToScan;
            do
            {
                nodesToScan = builder.GetNotScannedNodes().OfType<ProjectReferenceNode>().ToList();

                this.AnalyzeReferencesCore(builder, nodesToScan, config);
            } while (nodesToScan.Count > 0);
        }

        return builder.Build();
    }

    public DependencyGraph AnalyzeReferences(IEnumerable<ProjectReferenceNode> nodes, MsBuildConfig config)
    {
        var builder = new DependencyGraph.Builder(new SolutionReferenceNode());

        this.AnalyzeReferencesCore(builder, nodes, config);

        return builder.Build();
    }

    private void AnalyzeReferencesCore(
        DependencyGraph.Builder builder,
        IEnumerable<ProjectReferenceNode> nodes,
        MsBuildConfig config
    )
    {
        var analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions { LoggerFactory = loggerFactory, });

        foreach (var path in nodes.Select(n => n.Path))
        {
            var projectNode = new ProjectReferenceNode(path);
            var project = analyzerManager.GetProject(path);

            this.AddDependenciesToGraph(builder, project, projectNode, config);
        }
    }

    private void AddDependenciesToGraph(
        DependencyGraph.Builder builder,
        IProjectAnalyzer projectAnalyzer,
        ProjectReferenceNode projectNode,
        MsBuildConfig config
    )
    {
        var (includePackages, _, framework) = config;

        logger.LogInformation("Analyzing project {Project}", projectNode.Path);

        var analyzeResults = string.IsNullOrEmpty(framework)
            ? projectAnalyzer.Build()
            : projectAnalyzer.Build(framework);

        var analyzerResult = string.IsNullOrEmpty(framework)
            ? analyzeResults.FirstOrDefault()
            : analyzeResults[framework];

        _ = analyzerResult ?? throw new InvalidOperationException("Unable to load project.");

        builder.WithNode(projectNode, true);

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
