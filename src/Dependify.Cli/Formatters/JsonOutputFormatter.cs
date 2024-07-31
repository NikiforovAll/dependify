namespace Dependify.Cli.Formatters;

using Dependify.Core.Graph;
using Dependify.Core.Serializers;

internal class JsonOutputFormatter(TextWriter textWriter) : IOutputFormatter
{
    public void Dispose() => textWriter.Dispose();

    public void Write<T>(T data)
    {
        var graph = data as DependencyGraph;

        if (graph is null)
        {
            textWriter.WriteLine(JsonGraphSerializer.Serialize(data));
        }
        else
        {
            textWriter.WriteLine(JsonGraphSerializer.ToString(data as DependencyGraph));
        }

        textWriter.Flush();
    }
}
