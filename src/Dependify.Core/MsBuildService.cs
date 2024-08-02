namespace Dependify.Core;

using System.Reactive.Subjects;
using Buildalyzer;
using Dependify.Core.Graph;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

public class MsBuildService : IDisposable
{
    private readonly ILogger<MsBuildService> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly Subject<NodeEvent> subject;

    public IObservable<NodeEvent> OnLoadingEvents { get; }

    public MsBuildService(ILogger<MsBuildService> logger, ILoggerFactory loggerFactory)
    {
        this.logger = logger;
        this.loggerFactory = loggerFactory;
        this.subject = new Subject<NodeEvent>();
        this.OnLoadingEvents = this.subject;
    }

    public DependencyGraph AnalyzeReferences(SolutionReferenceNode solution, MsBuildConfig config)
    {
        this.logger.LogInformation("Analyzing solution {Solution}", solution.Path);
        this.subject.OnNext(new NodeEvent(NodeEventType.SolutionLoading, solution.Id, solution.Path));

        var analyzerManager = new AnalyzerManager(
            solution.Path,
            new AnalyzerManagerOptions { LoggerFactory = this.loggerFactory, }
        );

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

        this.logger.LogInformation("Analyzed solution {Solution}", solution.Path);
        this.subject.OnNext(new NodeEvent(NodeEventType.SolutionLoaded, solution.Id, solution.Path));

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

        var analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions { LoggerFactory = this.loggerFactory, });

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

        this.logger.LogInformation("Analyzing project {Project}", projectNode.Path);

        this.subject.OnNext(new NodeEvent(NodeEventType.ProjectLoading, projectNode.Id, projectNode.Path));

        var analyzeResults = string.IsNullOrEmpty(framework)
            ? projectAnalyzer.Build()
            : projectAnalyzer.Build(framework);

        var analyzerResult = string.IsNullOrEmpty(framework)
            ? analyzeResults.FirstOrDefault()
            : analyzeResults[framework];

        _ = analyzerResult ?? throw new InvalidOperationException("Unable to load project.");

        this.subject.OnNext(new NodeEvent(NodeEventType.ProjectLoaded, projectNode.Id, projectNode.Path));

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

    public void Dispose()
    {
        this.subject.OnCompleted();
    }
}

public class NodeEvent(NodeEventType eventType, string id, string path)
{
    public NodeEventType EventType { get; } = eventType;

    public string Id { get; } = id;
    public string Path { get; } = path;
    public string? Message { get; set; }
}

public enum NodeEventType
{
    ProjectLoading,
    ProjectLoaded,
    SolutionLoading,
    SolutionLoaded,
    Other
}

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
