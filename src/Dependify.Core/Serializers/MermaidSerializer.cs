namespace Dependify.Core.Serializers;

using System.CodeDom.Compiler;
using Dependify.Core.Graph;
using Depends.Core.Graph;

public static class MermaidSerializer
{
    private const string ProjectBackgroundColor = "#74200154";
    private const string PackageBackgroundColor = "#22aaee";

    public static string ToString(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        using var stringWriter = new StringWriter();
        using var writer = new IndentedTextWriter(stringWriter);

        writer.WriteLine("graph LR");

        writer.Indent++;

        foreach (var node in graph.Nodes.OfType<ProjectReferenceNode>())
        {
            writer.WriteLine($"{node.Id}:::project");
        }

        var packages = graph.Nodes.OfType<PackageReferenceNode>().ToArray();

        if (packages.Length > 0)
        {
            writer.WriteLine($"subgraph Packages");
            foreach (var node in graph.Nodes.OfType<PackageReferenceNode>())
            {
                writer.WriteLine($"{node.Id}:::package");
            }
            writer.WriteLine("end");
        }

        foreach (var reference in graph.Edges)
        {
            writer.WriteLine($"{reference.Start.Id} --> {reference.End.Id}");
        }

        writer.WriteLine($"classDef project fill:{ProjectBackgroundColor};");
        writer.WriteLine($"classDef package fill:{PackageBackgroundColor};");

        writer.Indent--;

        writer.Flush();

        return stringWriter.ToString();
    }
}
