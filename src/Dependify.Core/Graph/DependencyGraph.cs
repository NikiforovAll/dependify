namespace Dependify.Core.Graph;

using System.Collections.Immutable;

public sealed partial class DependencyGraph
{
    public Node Root { get; }

    public ImmutableHashSet<Node> Nodes { get; }

    public ImmutableHashSet<Edge> Edges { get; }

    private DependencyGraph(Node root, IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        this.Root = root;
        this.Nodes = nodes.ToImmutableHashSet();
        this.Edges = edges.ToImmutableHashSet();
    }

    public IEnumerable<Node> FindDescendants(Node node)
    {
        return this.Edges.Where(edge => edge.Start == node).Select(edge => edge.End).Distinct();
    }

    public IEnumerable<Node> FindAscendants(Node node)
    {
        return this.Edges.Where(edge => edge.End == node).Select(edge => edge.Start).Distinct();
    }
}
