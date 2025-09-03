namespace Dependify.Core;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Dependify.Core.Graph;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public class SolutionRegistry
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly Dictionary<SolutionReferenceNode, DependencyGraph> solutionGraphs = [];
    private static readonly Lock LockObject = new();

    private readonly FileProviderProjectLocator projectLocator;
    private readonly MsBuildService buildService;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly Subject<NodeEvent> subject;
#pragma warning restore CA2213 // Disposable fields should be disposed
    public IObservable<NodeEvent> OnLoadingEvents { get; }
    public IObservable<double> OnProgress { get; }

    public IList<SolutionReferenceNode> Solutions { get; private set; } = [];
    public IList<Node> Nodes { get; private set; }

    public IReadOnlyCollection<Node> ProjectsAndSolutions =>
        [
            .. this.GetFullGraph()
                .Nodes.Where(n =>
                    (n.Type == NodeConstants.Solution || n.Type == NodeConstants.Project)
                    && n is not SolutionReferenceNode { IsEmpty: true }
                ),
        ];
    public bool IsLoaded { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SolutionRegistry(FileProviderProjectLocator projectLocator, MsBuildService buildService)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        this.projectLocator = projectLocator;
        this.buildService = buildService;
        this.subject = new Subject<NodeEvent>();
        this.OnLoadingEvents = buildService.OnLoadingEvents.Merge(this.subject);

        this.LoadRegistry();
    }

    public void LoadRegistry()
    {
        var nodes = this.projectLocator.FullScan().ToList();

        this.Solutions = [.. nodes.OfType<SolutionReferenceNode>()];

        if (this.Solutions.Count == 0)
        {
            var solution = new SolutionReferenceNode();

            this.Solutions.Add(solution);
        }

        this.Nodes = nodes;
    }

    public Task LoadSolutionsAsync(MsBuildConfig msBuildConfig, CancellationToken cancellationToken = default)
    {
        this.IsLoaded = false;

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

                this.solutionGraphs[solution] = dependencyGraph;

                if (solution == this.Solutions[^1])
                {
                    this.subject.OnNext(
                        new NodeEvent(NodeEventType.RegistryLoaded, string.Empty, string.Empty)
                        {
                            Message = "All solutions loaded",
                        }
                    );
                }
            }

            this.IsLoaded = true;
        }

        return Task.CompletedTask;
    }

    public NodeUsage GetDependencyCount(SolutionReferenceNode solution, Node node)
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

    public DependencyGraph GetFullGraph()
    {
        var builder = new DependencyGraph.Builder(new SolutionReferenceNode());

        foreach (var (solution, graph) in this.solutionGraphs)
        {
            foreach (var node in graph.Nodes)
            {
                builder.WithNode(node);

                foreach (var edgeNode in graph.FindDescendants(node))
                {
                    builder.WithEdge(new Edge(node, edgeNode));
                }
            }
        }

        return builder.Build();
    }
}

public record NodeUsage(
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
