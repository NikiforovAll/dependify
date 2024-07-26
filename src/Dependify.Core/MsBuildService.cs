namespace Dependify.Core;

using Buildalyzer;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

public record MsBuildConfig
{
    public MsBuildConfig() { }

    public MsBuildConfig(bool includePackages, bool fullScan, string? framework)
    {
        this.IncludePackages = includePackages;
        this.FullScan = fullScan;
        this.Framework = framework;
    }

    public bool IncludePackages { get; set; }
    public bool FullScan { get; set; }
    public string? Framework { get; set; }

    public void Deconstruct(out bool includePackages, out bool fullScan, out string? framework)
    {
        includePackages = this.IncludePackages;
        fullScan = this.FullScan;
        framework = this.Framework;
    }
}

public class MsBuildService(ILogger<MsBuildService> logger, ILoggerFactory loggerFactory)
{
    private MsBuildServiceListener? listener;

    public void SetDiagnosticSource(MsBuildServiceListener? listener)
    {
        this.listener = listener;
    }

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

    public DependencyGraph AnalyzeReferences(ProjectReferenceNode node, MsBuildConfig config)
    {
        var builder = new DependencyGraph.Builder(node);

        this.AnalyzeReferencesCore(builder, [node], config);

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
        if (!nodes.Any())
        {
            return;
        }

        var analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions { LoggerFactory = loggerFactory, });

        foreach (var path in nodes.Select(n => n.Path))
        {
            var projectNode = new ProjectReferenceNode(path);
            var project = analyzerManager.GetProject(path);

            this.AddDependenciesToGraph(builder, project, projectNode, config);
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

        this.listener?.OnProjectLoading?.Invoke(projectNode);

        var analyzeResults = string.IsNullOrEmpty(framework)
            ? projectAnalyzer.Build()
            : projectAnalyzer.Build(framework);

        var analyzerResult = string.IsNullOrEmpty(framework)
            ? analyzeResults.FirstOrDefault()
            : analyzeResults[framework];

        _ = analyzerResult ?? throw new InvalidOperationException("Unable to load project.");

        this.listener?.OnProjectLoaded?.Invoke(analyzerResult);
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

public class MsBuildServiceListener(
    Action<ProjectReferenceNode>? projectLoading,
    Action<IAnalyzerResult>? projectLoaded
)
{
    public Action<ProjectReferenceNode>? OnProjectLoading { get; init; } = projectLoading;
    public Action<IAnalyzerResult>? OnProjectLoaded { get; init; } = projectLoaded;
}
