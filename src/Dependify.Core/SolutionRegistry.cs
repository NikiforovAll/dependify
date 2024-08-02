namespace Dependify.Core;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Dependify.Core.Graph;

public class SolutionRegistry
{
    private readonly Dictionary<SolutionReferenceNode, DependencyGraph> solutionGraphs = [];
    private static readonly object LockObject = new();

    private readonly FileProviderProjectLocator projectLocator;
    private readonly MsBuildService buildService;

    private readonly Subject<NodeEvent> subject;
    public IObservable<NodeEvent> OnLoadingEvents { get; }

    public IList<SolutionReferenceNode> Solutions { get; private set; } = [];
    public IList<Node> Nodes { get; private set; }
    public bool IsLoaded { get; private set; }

    public SolutionRegistry(FileProviderProjectLocator projectLocator, MsBuildService buildService)
    {
        this.projectLocator = projectLocator;
        this.buildService = buildService;
        this.subject = new Subject<NodeEvent>();
        this.OnLoadingEvents = buildService.OnLoadingEvents.Merge(this.subject);
    }

    public void LoadRegistry()
    {
        var nodes = this.projectLocator.FullScan().ToList();

        this.Solutions = nodes.OfType<SolutionReferenceNode>().ToList();

        if (this.Solutions.Count == 0)
        {
            var solution = new SolutionReferenceNode();

            this.Solutions.Add(solution);
        }

        this.Nodes = nodes;
    }

    public Task LoadSolutionsAsync(MsBuildConfig msBuildConfig, CancellationToken cancellationToken = default)
    {
        lock (LockObject)
        {
            for (var i = 0; i < this.Solutions.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var solution = this.Solutions[i];

                var dependencyGraph = solution.IsEmpty
                    ? this.buildService.AnalyzeReferences(
                        this.Nodes.OfType<ProjectReferenceNode>().ToList(),
                        msBuildConfig
                    )
                    : this.buildService.AnalyzeReferences(solution, msBuildConfig);

                // TODO: add cache lookup for already loaded solutions
                this.solutionGraphs.Add(solution, dependencyGraph);

                if(solution == this.Solutions[^1])
                {
                    this.subject.OnNext(new NodeEvent(NodeEventType.Other, string.Empty)
                    {
                        Message = "All solutions loaded"
                    });
                }
            }

            this.IsLoaded = true;
        }

        return Task.CompletedTask;
    }

    public NodeUsageStatistics GetDependencyCount(SolutionReferenceNode solution, Node node)
    {
        var graph = this.GetGraph(solution);

        return graph is null
            ? new(node, [], [], [])
            : new(
                node,
                graph.FindDescendants(node).OfType<ProjectReferenceNode>().ToList(),
                graph.FindDescendants(node).OfType<PackageReferenceNode>().ToList(),
                graph.FindAscendants(node).OfType<ProjectReferenceNode>().ToList()
            );
    }

    public DependencyGraph? GetGraph(SolutionReferenceNode solution)
    {
        return this.solutionGraphs.TryGetValue(solution, out var graph) ? graph : null;
    }
}

public record NodeUsageStatistics(
    Node Node,
    IList<ProjectReferenceNode> DependsOnProjects,
    IList<PackageReferenceNode> DependsOnPackages,
    IList<ProjectReferenceNode> UsedBy
)
{
    public int DependsOnProjectsCount => this.DependsOnProjects.Count;
    public int DependsOnPackagesCount => this.DependsOnPackages.Count;
    public int UsedByCount => this.UsedBy.Count;
}
