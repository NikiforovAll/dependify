namespace Dependify.Core;

using Dependify.Core.Graph;
using Depends.Core.Graph;

public class SolutionRegistry(FileProviderProjectLocator projectLocator, MsBuildService buildService)
{
    private readonly Dictionary<SolutionReferenceNode, DependencyGraph> solutionGraphs = [];
    private static readonly object LockObject = new();

    private readonly FileProviderProjectLocator projectLocator = projectLocator;
    private readonly MsBuildService buildService = buildService;

    public void LoadRegistry()
    {
        var nodes = this.projectLocator.FullScan().ToList();

        this.Solutions = nodes.OfType<SolutionReferenceNode>().ToList();
        this.Nodes = nodes;
    }

    public Task LoadSolutionsAsync(MsBuildConfig msBuildConfig, CancellationToken cancellationToken = default)
    {
        this.buildService.SetDiagnosticSource(this.listener);

        lock (LockObject)
        {
            for (var i = 0; i < this.Solutions.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var solution = this.Solutions[i];

                var dependencyGraph = this.buildService.AnalyzeReferences(solution, msBuildConfig);

                // TODO: add cache lookup for already loaded solutions
                this.solutionGraphs.Add(solution, dependencyGraph);

                this.solutionRegistryListener?.SolutionLoaded?.Invoke(solution, solution == this.Solutions[^1]);
            }

            this.IsLoaded = true;
        }

        return Task.CompletedTask;
    }

    public NodeUsageStatistics GetDependencyCount(SolutionReferenceNode solution, Node node)
    {
        var graph = this.GetGraph(solution);

        return graph is null
            ? new(node, 0, 0, 0)
            : new(
                node,
                graph.FindDescendants(node).OfType<ProjectReferenceNode>().Count(),
                graph.FindDescendants(node).OfType<PackageReferenceNode>().Count(),
                graph.FindAscendants(node).OfType<ProjectReferenceNode>().Count()
            );
    }

    public record NodeUsageStatistics(Node Node, int DependsOnProjects, int DependsOnPackages, int UsedBy);
    public IList<SolutionReferenceNode> Solutions { get; private set; } = [];
    public IList<Node> Nodes { get; private set; }
    public bool IsLoaded { get; private set; }

    public DependencyGraph? GetGraph(SolutionReferenceNode solution)
    {
        return this.solutionGraphs.TryGetValue(solution, out var graph) ? graph : null;
    }

    private MsBuildServiceListener? listener;
    private SolutionRegistryListener? solutionRegistryListener;

    public void SetBuildServiceDiagnosticSource(MsBuildServiceListener? listener = default)
    {
        this.listener = listener;
    }

    public void SetSolutionRegistryListener(SolutionRegistryListener? listener = default)
    {
        this.solutionRegistryListener = listener;
    }
}

public record SolutionRegistryListener(Action<SolutionReferenceNode, bool>? SolutionLoaded);
