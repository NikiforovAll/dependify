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

        AnalyzerManager analyzerManager;
        try
        {
            analyzerManager = new AnalyzerManager(
                solution.Path,
                new AnalyzerManagerOptions { LoggerFactory = this.loggerFactory }
            );
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create AnalyzerManager for solution {Solution}", solution.Path);

            // Try slnx parsing fallback for XML format solutions
            if (solution.IsXmlFormat)
            {
                var fallbackProjects = this.TryParseProjectsFromSlnx(solution.Path);
                if (fallbackProjects.Count > 0)
                {
                    var projectNodes = fallbackProjects.Select(path => new ProjectReferenceNode(path));
                    var fallbackBuilder = new DependencyGraph.Builder(solution);

                    // Analyze each project with the correct solution node as root
                    this.AnalyzeReferencesCore(fallbackBuilder, projectNodes, config);

                    // Connect solution to top-level projects
                    foreach (var projectPath in fallbackProjects)
                    {
                        var projectNode = new ProjectReferenceNode(projectPath);
                        fallbackBuilder.WithEdge(new Edge(solution, projectNode));
                    }

                    return fallbackBuilder.Build();
                }
            }

            // Return empty graph with just the solution node
            var emptyBuilder = new DependencyGraph.Builder(solution);
            return emptyBuilder.Build();
        }

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
                nodesToScan = [.. builder.GetNotScannedNodes().OfType<ProjectReferenceNode>()];

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
#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
        if (!nodes.Any())
        {
            return;
        }
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

        var analyzerManager = new AnalyzerManager(new AnalyzerManagerOptions { LoggerFactory = this.loggerFactory });

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
        foreach (var path in nodes.Select(n => n.Path))
        {
            var projectNode = new ProjectReferenceNode(path);
            var project = analyzerManager.GetProject(path);

            this.AddDependenciesToGraph(builder, project, projectNode, config);
        }
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

        if (config.FullScan)
        {
            List<ProjectReferenceNode> nodesToScan;
            do
            {
                nodesToScan = [.. builder.GetNotScannedNodes().OfType<ProjectReferenceNode>()];

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

        if (analyzerResult is null)
        {
            this.logger.LogWarning(
                "Failed to analyze project {Project} with framework {Framework}",
                projectNode.Path,
                framework ?? "default"
            );
            this.subject.OnNext(
                new NodeEvent(NodeEventType.ProjectFailed, projectNode.Id, projectNode.Path)
                {
                    Message = $"Failed to analyze project: {projectNode.Path}",
                }
            );

            // Try fallback: analyze without framework constraint if we originally tried with one
            if (!string.IsNullOrEmpty(framework))
            {
                this.logger.LogInformation(
                    "Attempting fallback analysis without framework constraint for {Project}",
                    projectNode.Path
                );
                var fallbackResults = projectAnalyzer.Build();
                analyzerResult = fallbackResults.FirstOrDefault();

                if (analyzerResult is not null)
                {
                    this.logger.LogInformation("Fallback analysis succeeded for {Project}", projectNode.Path);
                }
            }

            // If still null after fallback attempts, try XML parsing as last resort
            if (analyzerResult is null)
            {
                this.logger.LogInformation("Attempting XML parsing fallback for {Project}", projectNode.Path);
                var xmlReferences = this.TryParseProjectReferencesFromXml(projectNode.Path);

                if (xmlReferences.Count > 0)
                {
                    this.logger.LogInformation(
                        "XML parsing found {Count} project references for {Project}",
                        xmlReferences.Count,
                        projectNode.Path
                    );

                    this.subject.OnNext(new NodeEvent(NodeEventType.ProjectLoaded, projectNode.Id, projectNode.Path));
                    builder.WithNode(projectNode, true);

                    // Add only project references from XML parsing (no package references available)
                    foreach (var reference in xmlReferences)
                    {
                        var referenceNode = new ProjectReferenceNode(reference);
                        builder.WithNode(referenceNode);
                        builder.WithEdge(new Edge(projectNode, referenceNode));
                    }

                    return;
                }

                this.logger.LogError("All analysis methods failed for project {Project}, skipping", projectNode.Path);
                return;
            }
        }

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
                builder.WithEdge(new Edge(projectNode, referenceNode));
            }
        }
    }

    private List<string> TryParseProjectReferencesFromXml(string projectPath)
    {
        var references = new List<string>();

        try
        {
            if (!File.Exists(projectPath))
            {
                return references;
            }

            var projectDir = Path.GetDirectoryName(projectPath) ?? string.Empty;
            var doc = System.Xml.Linq.XDocument.Load(projectPath);

            var projectReferences = doc.Descendants("ProjectReference")
                .Where(pr => pr.Attribute("Include") is not null)
                .Select(pr => pr.Attribute("Include")!.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path));

            foreach (var reference in projectReferences)
            {
                // Convert relative paths to absolute paths
                var absolutePath = Path.IsPathRooted(reference)
                    ? reference
                    : Path.GetFullPath(Path.Combine(projectDir, reference));

                if (File.Exists(absolutePath))
                {
                    references.Add(absolutePath);
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to parse project references from XML for {Project}", projectPath);
        }

        return references;
    }

    private List<string> TryParseProjectsFromSlnx(string solutionPath)
    {
        var projects = new List<string>();

        try
        {
            if (!File.Exists(solutionPath))
            {
                return projects;
            }

            var solutionDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;
            var doc = System.Xml.Linq.XDocument.Load(solutionPath);

            var projectPaths = doc.Descendants("Project")
                .Where(p => p.Attribute("Path") is not null)
                .Select(p => p.Attribute("Path")!.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path));

            foreach (var projectPath in projectPaths)
            {
                var absolutePath = Path.IsPathRooted(projectPath)
                    ? projectPath
                    : Path.GetFullPath(Path.Combine(solutionDir, projectPath));

                if (File.Exists(absolutePath))
                {
                    projects.Add(absolutePath);
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return projects;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.subject.OnCompleted();
            this.subject.Dispose();
        }
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
    ProjectFailed,
    SolutionLoading,
    SolutionLoaded,
    RegistryLoaded,
    Other,
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

    public static MsBuildConfig Default => new(includePackages: true, fullScan: true, framework: null);

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
