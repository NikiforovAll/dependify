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
    public IObservable<double> OnProgress { get; }

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
                            Message = "All solutions loaded"
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
            var solutionNode = new SolutionReferenceNode(solution.Path);

            builder.WithNode(solutionNode);

            foreach (var node in graph.Nodes)
            {
                if (node.Type == NodeConstants.Solution)
                {
                    continue;
                }

                builder.WithNode(node);

                builder.WithEdge(new Edge(solutionNode, node));

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
