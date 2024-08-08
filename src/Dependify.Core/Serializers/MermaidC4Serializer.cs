namespace Dependify.Core.Serializers;

using System.CodeDom.Compiler;
using Dependify.Core.Graph;

public static class MermaidC4Serializer
{
    public static string ToString(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        using var stringWriter = new StringWriter();
        using var writer = new IndentedTextWriter(stringWriter);

        writer.WriteLine("C4Component");
        writer.WriteLine($"title {graph.Root.Id}");

        var projects = graph.Nodes.Where(n => n.Type == NodeConstants.Project);

        foreach (var project in projects)
        {
            writer.WriteLine($"Container_Boundary({project.Id}, \"{project.Id}\", \"\", \"\") {{");
            writer.Indent++;

            writer.WriteLine($"Component({project.Id}, \"{project.Id}\", \"Project\", \"\")");

            var packages = graph.FindDescendants(project).OfType<PackageReferenceNode>();

            if (packages.Any())
            {
                writer.WriteLine($"Container_Boundary(Packages.{project.Id}, \"Packages\", \"\", \"\") {{");
                writer.Indent++;

                foreach (var component in packages)
                {
                    writer.WriteLine($"Component({component.Id}, \"{component.Id}:{component.Version}\", \"Package\", \"\")");
                    writer.WriteLine(
                        $"UpdateElementStyle({component.Id}, $fontColor=\"white\", $bgColor=\"grey\", $borderColor=\"#99CB0E\")"
                    );
                }
                writer.Indent--;
                writer.WriteLine("}");
            }

            writer.Indent--;
            writer.WriteLine("}");
        }

        foreach (var project in projects)
        {
            foreach (var child in graph.FindDescendants(project).OfType<ProjectReferenceNode>())
            {
                writer.WriteLine($"Rel({project.Id}, {child.Id}, \"\")");
            }
        }

        writer.Flush();
        return stringWriter.ToString();
    }
}
