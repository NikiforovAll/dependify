namespace Dependify.Core.Serializers;

using System.CodeDom.Compiler;
using Dependify.Core.Graph;
using Depends.Core.Graph;

public static class GraphvizSerializer
{
    private const string ProjectBackgroundColor = "#74200154";
    private const string PackageBackgroundColor = "#22aaee";

    public static string ToString(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        using var stringWriter = new StringWriter();
        using var writer = new IndentedTextWriter(stringWriter);

        writer.WriteLine("digraph dependencies {");

        writer.Indent++;

        foreach (var node in graph.Nodes)
        {
            if (node is ProjectReferenceNode)
            {
                writer.WriteLine(
                    $"\"{node.Id}\" [label=\"{node.Id}\", fillcolor=\"{ProjectBackgroundColor}\", style=filled];"
                );
            }
            else if (node is PackageReferenceNode)
            {
                writer.WriteLine(
                    $"\"{node.Id}\" [label=\"{node.Id}\", fillcolor=\"{PackageBackgroundColor}\", style=filled];"
                );
            }
        }

        foreach (var reference in graph.Edges)
        {
            writer.WriteLine($"\"{reference.Start.Id}\" -> \"{reference.End.Id}\";");
        }

        writer.Indent--;

        writer.WriteLine("}");

        writer.Flush();

        return stringWriter.ToString();
    }
}
