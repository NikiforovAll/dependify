namespace Dependify.Core.Graph;

using System.Collections.Immutable;

public sealed partial class DependencyGraph
{
    public Node? Root { get; }

    public ImmutableHashSet<Node> Nodes { get; }

    public ImmutableHashSet<Edge> Edges { get; }

    private DependencyGraph(Node? root, IEnumerable<Node> nodes, IEnumerable<Edge> edges)
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

    public DependencyGraph SubGraph(Node node, Predicate<Node>? filter = default)
    {
        var nodes = this.FindAllDescendants(node, filter).Concat([node]).ToList();

        // TODO: bug - fix, filter?.Invoke(edge.End) == true

        var edges = this.Edges.Where(edge => nodes.Contains(edge.Start) && filter?.Invoke(edge.End) == true).ToList();

        return new DependencyGraph(node, nodes, edges);
    }

    private IEnumerable<Node> FindAllDescendants(Node node, Predicate<Node>? filter = default)
    {
        var nodes = new List<Node>();

        foreach (var child in this.FindDescendants(node))
        {
            if (filter is not null && !filter(child))
            {
                continue;
            }

            nodes.Add(child);
            nodes.AddRange(this.FindAllDescendants(child, filter));
        }

        return nodes;
    }

    public DependencyGraph CopyNoRoot()
    {
        return new DependencyGraph(
            default,
            this.Nodes.Where(n => n != this.Root),
            this.Edges.Where(n => n.Start != this.Root && n.End != this.Root)
        );
    }
}
