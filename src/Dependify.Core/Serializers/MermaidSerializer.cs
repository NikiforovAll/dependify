namespace Dependify.Core.Serializers;

using System.CodeDom.Compiler;
using Dependify.Core.Graph;

public record MermaidSerializerOptions(bool ShowPackages = true, string Orientation = "LR", bool NoStyle = false)
{
    public static MermaidSerializerOptions Empty => new();
}

public static class MermaidSerializer
{
    private const string ProjectBackgroundColor = "#74200154";
    private const string PackageBackgroundColor = "#22aaee";

    public static string ToString(DependencyGraph graph, MermaidSerializerOptions? options = default)
    {
        ArgumentNullException.ThrowIfNull(graph);

        options ??= MermaidSerializerOptions.Empty;

        using var stringWriter = new StringWriter();
        using var writer = new IndentedTextWriter(stringWriter);

        writer.WriteLine($"graph {options.Orientation}");

        writer.Indent++;

        foreach (var node in graph.Nodes.OfType<SolutionReferenceNode>())
        {
            writer.WriteLine($"{node.Id}");
        }

        foreach (var node in graph.Nodes.OfType<ProjectReferenceNode>())
        {
            writer.WriteLine($"{node.Id}:::project");
        }

        var packages = graph.Nodes.OfType<PackageReferenceNode>().ToArray();

        if (packages.Length > 0 && options.ShowPackages)
        {
            writer.WriteLine($"subgraph Packages");
            foreach (var node in graph.Nodes.OfType<PackageReferenceNode>())
            {
                writer.WriteLine($"{node.Id}[{node.Id}:{node.Version}]:::package");
            }
            writer.WriteLine("end");
        }

        var edgeIndex = 0;
        foreach (var reference in graph.Edges)
        {
            writer.WriteLine($"{reference.Start.Id} --> {reference.End.Id}");

            if (!options.NoStyle && reference.End.Type == NodeConstants.Package)
            {
                writer.WriteLine($"linkStyle {edgeIndex} stroke:#1976D2,stroke-width:1px;");
            }
            edgeIndex++;
        }

        if (!options.NoStyle)
        {
            writer.WriteLine($"classDef project fill:{ProjectBackgroundColor};");
            writer.WriteLine($"classDef package fill:{PackageBackgroundColor};");
        }

        writer.Indent--;

        writer.Flush();

        return stringWriter.ToString();
    }
}
