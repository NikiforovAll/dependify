namespace Dependify.Core.Graph;

using System.Collections.Generic;

public sealed partial class DependencyGraph
{
    public sealed class Builder
    {
        private readonly HashSet<Node> nodes = [];
        private readonly HashSet<Edge> edges = [];
        private readonly HashSet<Node> scannedForDependencies = [];

        public Node Root { get; }

        public Builder(Node root)
        {
            this.Root = root;
            this.nodes.Add(root);
        }

        public Builder WithEdges(IEnumerable<Edge> edges)
        {
            foreach (var edge in edges)
            {
                this.edges.Add(edge);
            }

            return this;
        }

        public Builder WithEdge(Edge edge)
        {
            this.edges.Add(edge);
            return this;
        }

        public Builder WithNodes(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                this.nodes.Add(node);
            }

            return this;
        }

        public Builder WithNode(Node node, bool scanned = false)
        {
            this.nodes.Add(node);

            if (scanned)
            {
                this.MarkAsScanned(node);
            }

            return this;
        }

        private void MarkAsScanned(Node node)
        {
            this.scannedForDependencies.Add(node);
        }

        public IEnumerable<Node> GetNotScannedNodes() => this.nodes.Except(this.scannedForDependencies);

        public DependencyGraph Build()
        {
            return new DependencyGraph(this.Root, this.nodes, this.edges);
        }
    }
}
