namespace Dependify.Cli.Formatters;

using Dependify.Core.Graph;
using Dependify.Core.Serializers;

internal sealed class JsonOutputFormatter(TextWriter textWriter) : IOutputFormatter
{
    public void Dispose() => textWriter.Dispose();

    public void Write<T>(T data)
    {
        if (data is not DependencyGraph)
        {
            textWriter.WriteLine(JsonGraphSerializer.Serialize(data));
        }
        else if (data is DependencyGraph graph)
        {
            textWriter.WriteLine(JsonGraphSerializer.ToString(graph));
        }

        textWriter.Flush();
    }
}
